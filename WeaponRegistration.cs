using System;
using System.Reflection;
using BepInEx.Logging;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Creates and registers the Anti-Material Rifle at runtime by cloning CartridgeSMG
/// and rewriting GunData into a ballistic sniper profile.
/// </summary>
public static class WeaponRegistration
{
    /// <summary>Catalog entry injected into AllGear (clone with our GearInfo).</summary>
    public static IUpgradable CatalogGear { get; private set; }

    /// <summary>Vanilla gun used as the NGO spawn source (never a runtime clone).</summary>
    public static Gun BaseGunPrefab { get; private set; }

    /// <summary>GameObject of <see cref="BaseGunPrefab"/>.</summary>
    public static GameObject BaseNetworkPrefab { get; private set; }

    /// <summary>Index of the base gun in <see cref="Global.AllGear"/> at registration time.</summary>
    public static int BaseAllGearIndex { get; private set; } = -1;

    public static void SetBaseAllGearIndex(int index) => BaseAllGearIndex = index;

    public static IUpgradable FindGearSafe(string apiName, int gearId = -1)
    {
        if (CatalogGear != null)
        {
            if (CatalogGear.Info != null &&
                (CatalogGear.Info.APIName == apiName || (gearId >= 0 && CatalogGear.Info.ID == gearId)))
            {
                return CatalogGear;
            }
        }

        if (Global.Instance?.AllGear != null)
        {
            IUpgradable[] all = Global.Instance.AllGear;
            for (int i = 0; i < all.Length; i++)
            {
                IUpgradable g = all[i];
                if (g?.Info == null)
                    continue;
                if (!string.IsNullOrEmpty(apiName) && g.Info.APIName == apiName)
                    return g;
                if (gearId >= 0 && g.Info.ID == gearId)
                    return g;
            }
        }

        try
        {
            if (PlayerData.Instance != null && !string.IsNullOrEmpty(apiName))
            {
                IUpgradable found = PlayerData.FindGear(apiName);
                if (found != null)
                    return found;
            }
        }
        catch
        {
            // collectedGear / Gear / Info null — ignore.
        }

        return null;
    }

    public static bool TryCreateAndRegister(
        string modGuid,
        int gearId,
        string apiName,
        string displayName,
        string description,
        string baseTypeName,
        bool autoUnlock,
        ManualLogSource log,
        out IUpgradable registeredGear)
    {
        registeredGear = null;

        if (string.IsNullOrEmpty(modGuid) || string.IsNullOrEmpty(apiName))
        {
            log?.LogError("[WeaponRegistration] modGuid / apiName required.");
            return false;
        }

        if (Global.Instance == null || Global.Instance.AllGear == null)
        {
            log?.LogError("[WeaponRegistration] Global.Instance.AllGear is null.");
            return false;
        }

        IUpgradable existing = FindGearSafe(apiName, gearId);
        if (existing != null)
        {
            CatalogGear = existing;
            registeredGear = existing;
            TryRefreshBaseIndex(baseTypeName, log);
            // Re-apply display strings in case TextBlocks was rebuilt / hot reload.
            RegisterGearTextBlocks(apiName, displayName, "Primary Weapon", description, log);
            if (existing.Info != null)
                TrySetMember(existing.Info, "_localizedName", displayName);
            // Critical for persistence: re-bind GearData.Gear after save load.
            EnsureGearData(existing, autoUnlock, log);
            log?.LogInfo($"[WeaponRegistration] Gear '{apiName}' already present — reusing.");
            return true;
        }


        if (!TryFindBaseGun(baseTypeName, log, out Gun baseGun, out GameObject baseObject, out int baseIndex))
            return false;

        BaseGunPrefab = baseGun;
        BaseNetworkPrefab = baseObject;
        BaseAllGearIndex = baseIndex;
        log?.LogInfo($"[WeaponRegistration] Base spawn prefab index={baseIndex} type={baseGun.GetType().Name}.");

        GameObject clone = UnityEngine.Object.Instantiate(baseObject);
        clone.name = $"[{modGuid}] {displayName}";
        clone.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(clone);

        if (clone.TryGetComponent<NetworkObject>(out var netObj))
        {
            UnityEngine.Object.DestroyImmediate(netObj);
            log?.LogDebug("[WeaponRegistration] Stripped NetworkObject from catalog clone (spawn uses base prefab).");
        }

        Gun cloneGun = clone.GetComponent<Gun>();
        if (cloneGun == null)
        {
            log?.LogError("[WeaponRegistration] Clone lost Gun component.");
            UnityEngine.Object.Destroy(clone);
            return false;
        }

        GearInfo info = CreateGearInfo(
            gearId,
            apiName,
            displayName,
            description,
            baseGun.Info,
            autoUnlock,
            log);


        if (info == null)
        {
            UnityEngine.Object.Destroy(clone);
            return false;
        }

        if (!TryAssignGearInfo(cloneGun, info, log))
        {
            UnityEngine.Object.Destroy(clone);
            return false;
        }

        if (cloneGun.Info == null || cloneGun.Info.ID != gearId || cloneGun.Info.APIName != apiName)
        {
            log?.LogError(
                $"[WeaponRegistration] GearInfo verification failed " +
                $"(Info={(cloneGun.Info == null ? "null" : cloneGun.Info.APIName + "/" + cloneGun.Info.ID)}).");
            UnityEngine.Object.Destroy(clone);
            return false;
        }

        BallisticSniperBehaviour behaviour = clone.GetComponent<BallisticSniperBehaviour>();
        if (behaviour == null)
            behaviour = clone.AddComponent<BallisticSniperBehaviour>();
        behaviour.InitializeAsPrefab(description);

        ApplyBallisticSniperStats(cloneGun, log);

        if (!InjectIntoAllGear(cloneGun, log))
        {
            UnityEngine.Object.Destroy(clone);
            return false;
        }

        InjectIntoPlayerData(cloneGun, autoUnlock, log);

        ModelImportHooks.ApplyPlaceholderHooks(clone, log);

        CatalogGear = cloneGun;
        registeredGear = cloneGun;
        return true;
    }

    /// <summary>
    /// Rewrites GunData / aim fields to match design + base stat notes.
    /// Values are tuned relative to typical primary feel (travel time, drop, slow bolt).
    /// </summary>
    private static IBullet _cachedProjectilePrefab;
    private static bool _projectileSearchDone;

    /// <summary>
    /// CartridgeSMG ships a RailBullet (hitscan). Swap to a vanilla SimpleProjectileBullet
    /// so bulletSpeed / bulletGravity from GunData actually apply.
    /// </summary>
    public static void EnsureProjectileBullet(Gun gun, ManualLogSource log = null)
    {
        if (gun == null)
            return;

        try
        {
            // Already a *safe* ballistic projectile? (reject PlateBullet etc.)
            var currentField = typeof(Gun).GetField("bulletPrefab",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (currentField?.GetValue(gun) is IBullet current && IsSafeBallisticProjectile(current))
                return;

            // Re-resolve if cache held a bad type from an older build.
            if (_projectileSearchDone && !IsSafeBallisticProjectile(_cachedProjectilePrefab))
            {
                _projectileSearchDone = false;
                _cachedProjectilePrefab = null;
            }

            if (!_projectileSearchDone)
            {
                _projectileSearchDone = true;
                _cachedProjectilePrefab = FindSimpleProjectilePrefab(log);
            }

            if (_cachedProjectilePrefab == null)
            {
                log?.LogWarning("[WeaponRegistration] No safe SimpleProjectileBullet prefab found — keeping rail bullet.");
                SparrohPlugin.Logger?.LogWarning("[BallisticSniper] No safe SimpleProjectileBullet prefab found — keeping rail bullet.");
                return;
            }


            gun.SetBullet(_cachedProjectilePrefab, gun.CreateBulletPool());
            try { gun.SetBulletPrefabOnObservers_Owner(); } catch { /* offline / not spawned */ }

            log?.LogInfo($"[WeaponRegistration] Swapped bullet → {_cachedProjectilePrefab.GetType().Name} ({_cachedProjectilePrefab.gameObject.name}).");
            SparrohPlugin.Logger?.LogInfo(
                $"[BallisticSniper] Projectile bullet set: {_cachedProjectilePrefab.GetType().Name}.");
        }

        catch (Exception ex)
        {
            log?.LogWarning($"[WeaponRegistration] EnsureProjectileBullet failed: {ex.Message}");
            SparrohPlugin.Logger?.LogWarning($"[BallisticSniper] EnsureProjectileBullet: {ex.Message}");
        }
    }

    private static IBullet FindSimpleProjectilePrefab(ManualLogSource log)
    {
        // 1) Preferred guns in AllGear
        IBullet fromGear = FindProjectileFromAllGear(log);
        if (fromGear != null)
            return fromGear;

        // 2) Global.syncedObjects — networked bullet prefabs live here
        IBullet fromSynced = FindProjectileFromSyncedObjects(log);
        if (fromSynced != null)
            return fromSynced;

        // 3) All loaded SimpleProjectileBullet assets (prefabs + instances)
        IBullet fromAll = FindProjectileFromResources(log);
        if (fromAll != null)
            return fromAll;

        log?.LogWarning(
            "[WeaponRegistration] No safe SimpleProjectileBullet found (AllGear + syncedObjects + Resources).");
        SparrohPlugin.Logger?.LogWarning(
            "[BallisticSniper] No safe SimpleProjectileBullet found after full scan.");
        return null;
    }

    private static IBullet FindProjectileFromAllGear(ManualLogSource log)
    {
        if (Global.Instance?.AllGear == null)
            return null;

        string[] preferredGuns = { "BounceShotgun", "BurrowerGun", "Bruiser", "Scattergun" };

        IBullet exactFallback = null;
        IBullet allowlistedFallback = null;

        IUpgradable[] all = Global.Instance.AllGear;
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is not Gun g || g == null)
                    continue;

                if (IsOurCatalogOrSmG(g))
                    continue;

                if (pass == 0)
                {
                    string tn = g.GetType().Name;
                    bool match = false;
                    for (int p = 0; p < preferredGuns.Length; p++)
                    {
                        if (string.Equals(tn, preferredGuns[p], StringComparison.Ordinal))
                        {
                            match = true;
                            break;
                        }
                    }
                    if (!match)
                        continue;
                }

                IBullet bullet = ReadBulletPrefab(g);
                if (!IsSafeBallisticProjectile(bullet))
                    continue;

                Type bt = bullet.GetType();
                log?.LogInfo(
                    $"[WeaponRegistration] AllGear candidate: gun={g.GetType().Name} bullet={bt.Name}.");

                if (bt == typeof(SimpleProjectileBullet))
                {
                    if (pass == 0)
                        return bullet;
                    exactFallback ??= bullet;
                    continue;
                }

                if (pass == 0)
                    return bullet;
                allowlistedFallback ??= bullet;
            }
        }

        return exactFallback ?? allowlistedFallback;
    }

    private static IBullet FindProjectileFromSyncedObjects(ManualLogSource log)
    {
        try
        {
            if (Global.Instance == null)
                return null;

            FieldInfo field = typeof(Global).GetField("syncedObjects",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field?.GetValue(Global.Instance) is not GameObject[] synced || synced.Length == 0)
                return null;

            IBullet exact = null;
            IBullet allowlisted = null;

            for (int i = 0; i < synced.Length; i++)
            {
                GameObject go = synced[i];
                if (go == null)
                    continue;

                IBullet bullet = go.GetComponent<IBullet>() ?? go.GetComponentInChildren<IBullet>(true);
                if (!IsSafeBallisticProjectile(bullet))
                    continue;

                Type bt = bullet.GetType();
                log?.LogInfo(
                    $"[WeaponRegistration] syncedObjects[{i}] candidate: {go.name} → {bt.Name}.");

                if (bt == typeof(SimpleProjectileBullet))
                    return bullet; // best match

                if (exact == null && bt == typeof(SimpleProjectileBullet))
                    exact = bullet;
                else if (allowlisted == null)
                    allowlisted = bullet;
            }

            return exact ?? allowlisted;
        }
        catch (Exception ex)
        {
            log?.LogDebug($"[WeaponRegistration] syncedObjects scan: {ex.Message}");
            return null;
        }
    }

    private static IBullet FindProjectileFromResources(ManualLogSource log)
    {
        try
        {
            // Includes prefab assets loaded with the game; filters out scene instances when possible.
            SimpleProjectileBullet[] all = Resources.FindObjectsOfTypeAll<SimpleProjectileBullet>();
            if (all == null || all.Length == 0)
                return null;

            IBullet exact = null;
            IBullet allowlisted = null;

            for (int i = 0; i < all.Length; i++)
            {
                SimpleProjectileBullet sp = all[i];
                if (sp == null)
                    continue;

                // Prefer prefab assets (hideFlags often DontUnloadUnusedAsset / none with no scene).
                // Skip objects that look like live scene instances with a valid scene root.
                try
                {
                    if (sp.gameObject.scene.IsValid() && sp.gameObject.scene.isLoaded)
                    {
                        // Live instance — only keep if we have nothing else
                        if (exact != null || allowlisted != null)
                            continue;
                    }
                }
                catch { /* ignore scene checks */ }

                if (!IsSafeBallisticProjectile(sp))
                    continue;

                Type bt = sp.GetType();
                if (bt == typeof(SimpleProjectileBullet))
                {
                    log?.LogInfo(
                        $"[WeaponRegistration] Resources exact: {sp.gameObject.name}.");
                    // Prefer non-scene prefab
                    try
                    {
                        if (!sp.gameObject.scene.IsValid() || !sp.gameObject.scene.isLoaded)
                            return sp;
                    }
                    catch { return sp; }
                    exact ??= sp;
                }
                else
                {
                    allowlisted ??= sp;
                }
            }

            if (exact != null)
            {
                log?.LogInfo($"[WeaponRegistration] Resources fallback exact: {exact.gameObject.name}.");
                return exact;
            }

            if (allowlisted != null)
            {
                log?.LogInfo(
                    $"[WeaponRegistration] Resources allowlisted: {allowlisted.GetType().Name} ({allowlisted.gameObject.name}).");
                return allowlisted;
            }
        }
        catch (Exception ex)
        {
            log?.LogDebug($"[WeaponRegistration] Resources scan: {ex.Message}");
        }

        return null;
    }


    /// <summary>
    /// True only for plain ballistic projectiles that do not cast ParentSource to a specific gun.
    /// PlateBullet.OnInitialized casts to PlateLauncher → InvalidCastException on our SMG clone.
    /// </summary>
    private static bool IsSafeBallisticProjectile(IBullet bullet)
    {
        if (bullet == null)
            return false;

        // Must be a SimpleProjectileBullet (or subclass), not RailBullet / ConeBullet / etc.
        if (bullet is not SimpleProjectileBullet)
            return false;

        Type t = bullet.GetType();

        // Ideal: exact vanilla projectile type.
        if (t == typeof(SimpleProjectileBullet))
            return true;

        // Small allowlist of subclasses that don't require a specific gun type in OnInitialized.
        if (t == typeof(BurrowerBullet))
            return true;

        // Everything else is specialized (PlateBullet, BoxBullet, GrenadeBullet, rockets, acid…).
        return false;
    }


    private static bool IsOurCatalogOrSmG(Gun g)
    {
        if (g == null)
            return true;
        if (g is CartridgeSMG)
            return true;
        if (g.Info != null &&
            (g.Info.APIName == SparrohPlugin.GearApiName || g.Info.ID == SparrohPlugin.GearId))
            return true;
        return false;
    }

    private static IBullet ReadBulletPrefab(Gun gun)
    {
        try
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            // Serialized prefab reference (often set; IBullet field may still be null pre-SetupBulletPool).
            var goField = typeof(Gun).GetField("_bulletPrefab", flags);
            if (goField?.GetValue(gun) is GameObject go && go != null)
            {
                IBullet fromGo = go.GetComponent<IBullet>() ?? go.GetComponentInChildren<IBullet>(true);
                if (fromGo != null)
                    return fromGo;
            }

            var f = typeof(Gun).GetField("bulletPrefab", flags);
            if (f?.GetValue(gun) is IBullet live)
                return live;
        }
        catch { /* ignore */ }
        return null;
    }


    public static void ApplyBallisticSniperStats(Gun gun, ManualLogSource log = null)
    {
        if (gun == null)
            return;

        ref GunData g = ref gun.GunData;


        // --- Core combat (base stat notes) ---
        g.damage = 145f;
        g.damageEffect = EffectType.Normal;
        g.damageEffectAmount = 0f;
        g.fireInterval = 1.25f; // 48 RPM
        g.fireAnimationSpeedMultiplier = Mathf.Max(g.fireAnimationSpeedMultiplier, 0.35f);
        g.automatic = 0; // bolt / slow semi
        g.bulletsPerShot = 1;
        g.burstSize = 1;
        g.burstFireInterval = 0f;
        g.useAmmoOnFire = 1;
        g.doesEachBulletInShotRemoveAmmo = 0;
        g.doesEachBulletInShotTriggerEffects = false;

        // --- Magazine / reserves ---
        g.magazineSize = 5;
        g.hasLimitedAmmo = true;
        g.ammoCapacity = 20;
        g.ammoCollectMultiplier = Mathf.Max(g.ammoCollectMultiplier, 0.75f);
        g.storedAmmoCollectMultiplier = Mathf.Max(g.storedAmmoCollectMultiplier, 0.75f);
        g.ammoGenerationEfficiency = 0f;
        g.useAmmoWhileFiringInterval = 0f;
        // Single-round reload owns ammo fill via BallisticSniperReloadHook.
        g.refillAmmoOnReload = false;
        g.reloadDuration = 3.1f;
        g.autoReloadWhenEmpty = true;

        // --- Projectile: clear travel time + drop at range ---
        // SMG defaults are typically much faster / flatter; push toward heavy slug.
        g.bulletSpeed = 220f;
        g.bulletGravity = 9.5f;
        g.maxBounces = 0;
        g.bulletMagnetismSurface = 0f;
        g.bulletMagnetismTarget = 0f;

        // --- Range / falloff: soft falloff only at very long range ---
        g.rangeData.falloffStartDistance = 140f;
        g.rangeData.falloffEndDistance = 200f;
        g.rangeData.maxDamageRange = 250f;
        g.rangeData.maxFalloffDamageMultiplier = 0.65f;

        // High stagger / knockback for anti-materiel feel.
        g.hitForce = Mathf.Max(g.hitForce * 2.5f, 28f);
        g.hitVFXSize = Mathf.Max(g.hitVFXSize, 1.4f);

        // --- Spread: hip is intentionally terrible. ADS is handled by BallisticSniperSpreadHook
        // (vanilla GetSpread does not shrink cone while aiming). ---
        g.spreadData.spreadType = SpreadData.SpreadType.Circle;
        g.spreadData.spreadSize = new Vector2(3.8f, 3.8f);
        g.firstShotSpreadMultiplier = 1f;


        // --- Recoil: high vertical single kick, low horizontal ---
        g.recoilData.recoilX = new Vector2(0.05f, 0.18f);
        g.recoilData.recoilY = new Vector2(2.8f, 3.6f);
        g.recoilData.recoilZ = new Vector2(0.1f, 0.35f);
        g.recoilData.maxRecoilZ = 2f;
        g.recoilData.translateZ = new Vector2(0.04f, 0.08f);
        g.recoilData.maxTranslateZ = 0.12f;
        g.recoilData.aimTranslateMultiplier = 0.85f;
        g.recoilData.recoilSpeed = Mathf.Max(g.recoilData.recoilSpeed, 18f);
        g.recoilData.recoilRecoverySpeed = 6f;
        g.recoilData.translateSpeed = Mathf.Max(g.recoilData.translateSpeed, 14f);
        g.recoilData.translateRecoverySpeed = 5f;
        g.recoilData.recoilTargetDecaySpeed = 4f;
        g.recoilData.aimRecoilMultiplier = new Vector3(0.55f, 0.7f, 0.55f);

        g.bulletShakeTranslation = Mathf.Max(g.bulletShakeTranslation, 0.08f);
        g.bulletShakeRotation = Mathf.Max(g.bulletShakeRotation, 0.6f);

        // No charge weapon behaviour.
        g.chargeData.duration = 0f;
        g.chargeData.coolDownSpeed = 0f;
        g.chargeData.fireWhenFullyCharged = false;
        g.chargeData.fireOnRelease = false;
        g.chargeData.canFireWhileCharging = false;
        g.chargeData.time = 0f;

        // Handling: stop sprint to fire/aim; heavy commitment.
        g.fireConstraints.canFireWhileSprinting = FireConstraints.ActionFireMode.StopActionAndPerform;
        g.fireConstraints.canFireWhileSliding = FireConstraints.ActionFireMode.StopActionAndPerform;
        g.fireConstraints.canAimWhileSliding = FireConstraints.ActionFireMode.CannotPerformDuring;
        g.fireConstraints.canAimWhileReloading = false;
        g.fireConstraints.canReloadWhileSprinting = FireConstraints.ActionFireMode.StopActionAndPerform;

        // ADS: slow transition, strong zoom (design: 0.55s ADS).
        gun.IsAimEnabled = true;
        gun.AimFOV = 28f;
        TrySetAimTransitionDuration(gun, 0.55f);

        log?.LogInfo(
            $"[WeaponRegistration] Applied ballistic sniper stats: dmg={g.damage}, " +
            $"rpm≈{60f / g.fireInterval:0}, mag={g.magazineSize}, reserve={g.ammoCapacity}, " +
            $"reload={g.reloadDuration}s, speed={g.bulletSpeed}, grav={g.bulletGravity}.");
    }

    private static void TrySetAimTransitionDuration(Gun gun, float duration)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        FieldInfo field = typeof(Gun).GetField("aimTransitionDuration", flags);
        if (field != null && field.FieldType == typeof(float))
            field.SetValue(gun, duration);
    }

    private static void TryRefreshBaseIndex(string preferredTypeName, ManualLogSource log)
    {
        if (Global.Instance?.AllGear == null)
            return;

        if (BaseGunPrefab != null)
        {
            int idx = Array.IndexOf(Global.Instance.AllGear, (IUpgradable)BaseGunPrefab);
            if (idx >= 0)
            {
                BaseAllGearIndex = idx;
                return;
            }
        }

        if (TryFindBaseGun(preferredTypeName, log, out Gun g, out GameObject go, out int index))
        {
            BaseGunPrefab = g;
            BaseNetworkPrefab = go;
            BaseAllGearIndex = index;
        }
    }

    private static bool TryFindBaseGun(
        string preferredTypeName,
        ManualLogSource log,
        out Gun gear,
        out GameObject go,
        out int allGearIndex)
    {
        gear = null;
        go = null;
        allGearIndex = -1;

        Gun fallback = null;
        GameObject fallbackGo = null;
        int fallbackIndex = -1;

        IUpgradable[] all = Global.Instance.AllGear;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] is not Gun g)
                continue;

            if (g.GearType != GearType.Primary && g.GearType != GearType.Custom)
            {
                if (fallback == null)
                {
                    fallback = g;
                    fallbackGo = g.gameObject;
                    fallbackIndex = i;
                }
                continue;
            }

            GameObject candidate = g.gameObject;
            string typeName = g.GetType().Name;

            if (!string.IsNullOrEmpty(preferredTypeName) &&
                string.Equals(typeName, preferredTypeName, StringComparison.Ordinal))
            {
                gear = g;
                go = candidate;
                allGearIndex = i;
                log?.LogInfo($"[WeaponRegistration] Base gun: {typeName} ({candidate.name}) index={i}.");
                return true;
            }

            if (fallback == null || fallback.GearType != GearType.Primary)
            {
                fallback = g;
                fallbackGo = candidate;
                fallbackIndex = i;
            }
        }

        if (fallback != null)
        {
            gear = fallback;
            go = fallbackGo;
            allGearIndex = fallbackIndex;
            log?.LogWarning(
                $"[WeaponRegistration] '{preferredTypeName}' not found — " +
                $"falling back to {fallback.GetType().Name} ({fallbackGo.name}) index={fallbackIndex}.");
            return true;
        }

        log?.LogError("[WeaponRegistration] No Gun found in Global.AllGear.");
        return false;
    }

    private static GearInfo CreateGearInfo(
        int gearId,
        string apiName,
        string displayName,
        string description,
        GearInfo template,
        bool autoUnlock,
        ManualLogSource log)
    {
        GearInfo info = ScriptableObject.CreateInstance<GearInfo>();
        info.name = apiName;

        TrySetMember(info, "ID", gearId);
        TrySetMember(info, "<ID>k__BackingField", gearId);
        TrySetMember(info, "_name", apiName);
        TrySetMember(info, "id", gearId);

        // GearInfo.Name/TypeName/Description resolve through TextBlocks.GetString(_name, index).
        RegisterGearTextBlocks(apiName, displayName, "Primary Weapon", description, log);
        TrySetMember(info, "_localizedName", displayName);

        if (template != null)
        {
            object grid = GetMember(template, "grid");
            if (grid != null)
                TrySetMember(info, "grid", grid);

            if (template.Icon != null)
                TrySetMember(info, "<Icon>k__BackingField", template.Icon);

            if (template.UnlockCost != null)
                info.UnlockCost = template.UnlockCost;

            info.CanGainXP = template.CanGainXP;
            info.XPGainMultilier = template.XPGainMultilier;
            info.MaxLevel = template.MaxLevel;
            info.MinUnlockLevel = 0;
            info.HideWhenNotCollected = false;
        }
        else if (Global.Instance != null && Global.Instance.WarningIcon != null)
        {
            TrySetMember(info, "<Icon>k__BackingField", Global.Instance.WarningIcon);
        }

        info.UnlockAutomatically = autoUnlock;
        info.UnlockState = autoUnlock
            ? PlayerData.UnlockState.Unlocked
            : PlayerData.UnlockState.NotCollected;

        // Empty upgrade pool — base weapon only for now (grid still present via template.grid).
        TrySetMember(info, "upgrades", Array.Empty<Upgrade>());
        TrySetMember(info, "skins", Array.Empty<SkinUpgrade>());
        TrySetMember(info, "combinedUpgradeList", new System.Collections.Generic.List<Upgrade>());
        TrySetMember(info, "defaultSkin", null);
        TrySetMember(info, "<DefaultSkin>k__BackingField", null);

        _ = info.Upgrades;

        if (!info.HasUpgradeGrid)
            log?.LogWarning("[WeaponRegistration] GearInfo has no upgrade grid — UI may hide hex inventory.");

        log?.LogDebug($"[WeaponRegistration] GearInfo created id={gearId} api={apiName} name={displayName}");
        return info;
    }

    /// <summary>
    /// Injects TextBlocks entries for a custom gear API name.
    /// Index 0 = display name, 1 = type line, 2 = description (see GearInfo.Name/TypeName/Description).
    /// </summary>
    private static void RegisterGearTextBlocks(
        string apiName,
        string displayName,
        string typeName,
        string description,
        ManualLogSource log)
    {
        if (string.IsNullOrEmpty(apiName))
            return;

        try
        {
            var group = new TextBlocks.TextBlockGroup(0)
            {
                blocks = new[]
                {
                    new TextBlocks.TextBlock(displayName ?? apiName, apiName),
                    new TextBlocks.TextBlock(typeName ?? "Primary Weapon", apiName),
                    new TextBlocks.TextBlock(description ?? string.Empty, apiName)
                }
            };

            TextBlocks.strings[apiName] = group;
            log?.LogDebug($"[WeaponRegistration] Registered TextBlocks for '{apiName}' → '{displayName}'.");
        }
        catch (Exception ex)
        {
            log?.LogWarning($"[WeaponRegistration] TextBlocks registration failed: {ex.Message}");
        }
    }


    private static bool TryAssignGearInfo(Gun gear, GearInfo info, ManualLogSource log)
    {
        if (TrySetMember(gear, "<Info>k__BackingField", info) ||
            TrySetMember(gear, "Info", info))
        {
            return true;
        }

        log?.LogError("[WeaponRegistration] Failed to assign GearInfo onto clone (reflection).");
        return false;
    }

    private static bool InjectIntoAllGear(IUpgradable gear, ManualLogSource log)
    {
        IUpgradable[] all = Global.Instance.AllGear;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].Info != null && all[i].Info.ID == gear.Info.ID)
            {
                log?.LogWarning($"[WeaponRegistration] AllGear already contains id={gear.Info.ID}.");
                return true;
            }
        }

        var expanded = new IUpgradable[all.Length + 1];
        Array.Copy(all, expanded, all.Length);
        expanded[all.Length] = gear;
        Global.Instance.AllGear = expanded;

        if (gear is Component gearComponent)
            TryAppendObjectArray(Global.Instance, "_allGear", gearComponent.gameObject);

        log?.LogInfo($"[WeaponRegistration] Injected into AllGear (count={expanded.Length}).");
        return true;
    }

    private static void InjectIntoPlayerData(IUpgradable gear, bool autoUnlock, ManualLogSource log)
    {
        EnsureGearData(gear, autoUnlock, log);
    }

    /// <summary>
    /// Bind or create PlayerData.GearData for our gear so save entries aren't purged
    /// (OnAwake removes collectedGear keys whose Gear ref is null).
    /// </summary>
    public static void EnsureGearData(IUpgradable gear, bool autoUnlock, ManualLogSource log)
    {
        if (gear?.Info == null)
            return;

        if (PlayerData.Instance == null)
        {
            log?.LogWarning("[WeaponRegistration] PlayerData.Instance null — gear may appear on next InitializeCollectedGear.");
            return;
        }

        PlayerData.GearData existing = PlayerData.GetGearData(gear);
        if (existing != null)
        {
            existing.Gear = gear;
            if (autoUnlock && !existing.IsUnlocked)
                existing.Unlock();
            log?.LogDebug("[WeaponRegistration] Bound existing GearData entry.");
            return;
        }

        // Try by id in case GetGearData(gear) failed due to ref mismatch but save has the id.
        existing = PlayerData.GetGearData(gear.Info.ID);
        if (existing != null)
        {
            existing.Gear = gear;
            if (autoUnlock && !existing.IsUnlocked)
                existing.Unlock();
            log?.LogInfo("[WeaponRegistration] Re-bound GearData by id after load.");
            return;
        }

        FieldInfo field = typeof(PlayerData).GetField("collectedGear", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field?.GetValue(PlayerData.Instance) is System.Collections.IDictionary dict)
        {
            var data = new PlayerData.GearData(gear, autoUnlock ? PlayerData.UnlockState.Unlocked : PlayerData.UnlockState.NotCollected);
            dict[gear.Info.ID] = data;
            if (autoUnlock)
                data.Unlock();
            log?.LogInfo("[WeaponRegistration] Added GearData to collectedGear.");
            return;
        }

        log?.LogWarning("[WeaponRegistration] Could not inject GearData directly.");
    }


    #region Reflection helpers

    private static bool TrySetMember(object target, string name, object value)
    {
        if (target == null)
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        PropertyInfo prop = type.GetProperty(name, flags);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(target, value);
            return true;
        }

        FieldInfo field = type.GetField(name, flags);
        if (field != null)
        {
            field.SetValue(target, value);
            return true;
        }

        for (Type t = type.BaseType; t != null; t = t.BaseType)
        {
            field = t.GetField(name, flags);
            if (field != null)
            {
                field.SetValue(target, value);
                return true;
            }
            prop = t.GetProperty(name, flags);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value);
                return true;
            }
        }

        return false;
    }

    private static object GetMember(object target, string name)
    {
        if (target == null)
            return null;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();
        return type.GetField(name, flags)?.GetValue(target)
            ?? type.GetProperty(name, flags)?.GetValue(target);
    }

    private static void TryAppendObjectArray(object host, string fieldName, UnityEngine.Object item)
    {
        FieldInfo field = host.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null || field.GetValue(host) is not Array arr)
            return;

        Type elemType = arr.GetType().GetElementType();
        if (elemType == null || !elemType.IsInstanceOfType(item))
            return;

        Array expanded = Array.CreateInstance(elemType, arr.Length + 1);
        Array.Copy(arr, expanded, arr.Length);
        expanded.SetValue(item, arr.Length);
        field.SetValue(host, expanded);
    }

    #endregion
}

/// <summary>
/// Extension points for swapping visuals / audio without rewriting gameplay.
/// </summary>
public static class ModelImportHooks
{
    public static void ApplyPlaceholderHooks(GameObject gearRoot, ManualLogSource log)
    {
        log?.LogDebug("[ModelImportHooks] Placeholder only — using vanilla CartridgeSMG visuals.");
    }

    public static bool ApplyMesh(GameObject root, Mesh mesh, Material material = null)
    {
        if (root == null || mesh == null)
            return false;

        MeshFilter filter = root.GetComponentInChildren<MeshFilter>(true);
        if (filter == null)
            return false;

        filter.sharedMesh = mesh;
        if (material != null && filter.TryGetComponent<MeshRenderer>(out var renderer))
            renderer.sharedMaterial = material;
        return true;
    }

    public static bool TrySetBulletPrefab(Gun gun, GameObject bulletPrefab)
    {
        if (gun == null || bulletPrefab == null)
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        FieldInfo field = typeof(Gun).GetField("_bulletPrefab", flags)
            ?? typeof(Gun).GetField("bulletPrefab", flags);
        if (field == null)
            return false;

        field.SetValue(gun, bulletPrefab);
        return true;
    }
}
