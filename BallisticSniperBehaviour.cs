using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


/// <summary>
/// Custom gameplay host for the Anti-Material Rifle — baseline + upgrade fields/runtime.
/// </summary>
public sealed class BallisticSniperBehaviour : MonoBehaviour
{
    [Serializable]
    public struct Data
    {
        public bool singleRoundReload;

        // Hullbreaker
        public float shellDamageMult;

        // Subsonic
        public float fullHpDamageMult;

        // Ricochet Protocol
        public int bonusBounces;
        public float postBounceHoming;

        // Overpressure
        public bool overpressure;
        public float overpressureChargeDuration;
        public float overpressureDamageMult;
        public float overpressureMoveMult;
        public int overpressureMagPenalty;

        // Twin Link
        public int extraBulletsPerShot;
        public float twinLinkFenceDamage;
        public float twinLinkFenceRadius;

        // Mark of Exhaustion
        public float exhaustionCooldown;
        public float exhaustionSlowDuration;
        public float exhaustionSlowStrength;

        // Longwatch
        public float longwatchAimSeconds;
        public float longwatchRangeBonus;
        public float longwatchGravityMult;
        public float longwatchSpeedMult;

        // Scouter
        public float scouterInterval;
        public float scouterRadius;

        // Perforator
        public int pierceTargets;
        public float pierceFalloff;

        // Spotter
        public float spotterRadius;
        public float spotterDuration;

        // Deadbolt
        public float deadboltDuration;

        // Death Mark
        public bool deathMark;
        public float deathMarkFuse;
        public float deathMarkRadius;
        public float deathMarkDamageScale;
        public float deathMarkHeadshotFuseMult;
        public float deathMarkStackScale;

        // Auto Trigger
        public bool autoTrigger;
        public float autoTriggerFireInterval;
        public float autoTriggerDamageMult;
        public int autoTriggerMagBonus;
        public float autoTriggerAdsSpread;

        // High Explosive C4
        public bool highExplosive;
        public float c4Damage;
        public float c4Radius;
        public float c4ThrowForce;
        public float c4ArmTime;

        // Clipped
        public bool clipped;
        public float clippedReloadMult;

        // Anchor
        public bool anchor;
        public float anchorSpeedThreshold;

        // One in the Chamber
        public bool oneInTheChamber;

        // Disrupt Channel
        public float disruptDuration;

        // Heavy Grain / Reserve / elements / move
        public float heavyGrainDamageMult;
        public float reserveAmmoMult;
        public float rotAmount;
        public float rotSplashRadius;
        public float waterAmount;
        public float repositionMoveBonus;
        public float repositionAdsMoveMult;

        // Powered Echo
        public float echoDelay;
        public float echoDamageScale;
        public float echoShockAmount;

        // Transfer Relay
        public float transferRelayScale;

        // Synchronize
        public float syncWindow;
        public float syncDamageMult;

        // Overkill — leftover shell damage carries to parent layer
        public bool overkill;
    }


    public struct DeathMarkEntry
    {
        public ITarget target;
        public float detonateAt;
        public float stacks;
        public Vector3 lastPos;
        public bool fromHeadshot;
    }

    public struct PendingEcho
    {
        public float fireAt;
        public Vector3 position;
        public Quaternion rotation;
        public float damage;
        public float speed;
        public float gravity;
    }

    [SerializeField]
    private Data data = CreateDefaultData();

    private Data prefabSnapshot = CreateDefaultData();
    private string description = "Anti-Material Rifle";

    // Runtime
    public bool IsTubeReloading { get; set; }
    public float AimHoldTime { get; private set; }
    public float LastKillTime { get; set; } = -999f;
    public float ExhaustionReadyAt { get; set; }
    public bool ExhaustionShotArmed { get; set; }
    public float LastScouterPulse { get; set; } = -999f;
    public bool ChamberBonusReady { get; set; }
    public bool LastReloadWasFullTopOff { get; set; }
    /// <summary>True while the phantom chamber round is held — blocks auto-reload.</summary>
    public bool BlockAutoReloadForChamber { get; set; }
    public bool C4Deployed { get; set; }
    public Vector3 C4Position { get; set; }
    public float C4DeployedAt { get; set; }
    public float ReloadHoldTime { get; set; }
    public bool ReloadHeld { get; set; }

    public readonly List<DeathMarkEntry> DeathMarks = new List<DeathMarkEntry>(8);
    public readonly List<PendingEcho> PendingEchoes = new List<PendingEcho>(4);
    public readonly List<ITarget> TwinShotHits = new List<ITarget>(4);

    private Gun boundGun;
    private bool hooksBound;
    private bool moveHookBound;
    private GameObject c4Visual;
    private readonly List<ISelectable> activeHighlights = new List<ISelectable>(16);
    private float highlightClearAt = -999f;
    private BulletData lastFiredBullet;
    private bool hasLastFiredBullet;


    public ref Data WeaponData => ref data;
    public Data GetPrefabSnapshot() => prefabSnapshot;
    public string Description => description;
    public bool SingleRoundReload => data.singleRoundReload && !data.clipped;

    public static Data CreateDefaultData()
    {
        return new Data
        {
            singleRoundReload = true,
            shellDamageMult = 1f,
            fullHpDamageMult = 1f,
            bonusBounces = 0,
            postBounceHoming = 0f,
            overpressure = false,
            overpressureChargeDuration = 0f,
            overpressureDamageMult = 1f,
            overpressureMoveMult = 1f,
            overpressureMagPenalty = 0,
            extraBulletsPerShot = 0,
            twinLinkFenceDamage = 0f,
            twinLinkFenceRadius = 0f,
            exhaustionCooldown = 0f,
            exhaustionSlowDuration = 0f,
            exhaustionSlowStrength = 0f,
            longwatchAimSeconds = 0f,
            longwatchRangeBonus = 0f,
            longwatchGravityMult = 1f,
            longwatchSpeedMult = 1f,
            scouterInterval = 0f,
            scouterRadius = 0f,
            pierceTargets = 0,
            pierceFalloff = 0f,
            spotterRadius = 0f,
            spotterDuration = 0f,
            deadboltDuration = 0f,
            deathMark = false,
            deathMarkFuse = 2f,
            deathMarkRadius = 4f,
            deathMarkDamageScale = 1.2f,
            deathMarkHeadshotFuseMult = 0.6f,
            deathMarkStackScale = 0.35f,
            autoTrigger = false,
            autoTriggerFireInterval = 0.35f,
            autoTriggerDamageMult = 0.5f,
            autoTriggerMagBonus = 5,
            autoTriggerAdsSpread = 0.8f,
            highExplosive = false,
            c4Damage = 220f,
            c4Radius = 8f,
            c4ThrowForce = 18f,
            c4ArmTime = 0.45f,
            clipped = false,
            clippedReloadMult = 1f,
            anchor = false,
            anchorSpeedThreshold = 0.15f,
            oneInTheChamber = false,
            disruptDuration = 0f,
            heavyGrainDamageMult = 1f,
            reserveAmmoMult = 1f,
            rotAmount = 0f,
            rotSplashRadius = 0f,
            waterAmount = 0f,
            repositionMoveBonus = 0f,
            repositionAdsMoveMult = 1f,
            echoDelay = 0f,
            echoDamageScale = 0f,
            echoShockAmount = 0f,
            transferRelayScale = 0f,
            syncWindow = 0f,
            syncDamageMult = 1f,
            overkill = false
        };
    }


    public void InitializeAsPrefab(string desc)
    {
        description = desc ?? "Anti-Material Rifle";
        data = CreateDefaultData();
        prefabSnapshot = data;
        ResetRuntime();
    }

    public void RestoreFromPrefab()
    {
        data = prefabSnapshot;
    }

    public void CapturePrefabSnapshot() => prefabSnapshot = data;

    public void CopyFrom(BallisticSniperBehaviour template)
    {
        if (template == null)
            return;
        description = template.description;
        prefabSnapshot = template.prefabSnapshot;
        data = prefabSnapshot;
        ResetRuntime();
    }

    public void ResetRuntime()
    {
        IsTubeReloading = false;
        AimHoldTime = 0f;
        LastKillTime = -999f;
        ExhaustionReadyAt = 0f;
        ExhaustionShotArmed = false;
        LastScouterPulse = -999f;
        ChamberBonusReady = false;
        LastReloadWasFullTopOff = false;
        BlockAutoReloadForChamber = false;
        C4Deployed = false;
        ReloadHoldTime = 0f;
        ReloadHeld = false;
        DeathMarks.Clear();
        PendingEchoes.Clear();
        TwinShotHits.Clear();
        hasLastFiredBullet = false;
        hasPendingOverkill = false;
        pendingOverkill = 0f;
        pendingOverkillParent = null;
        pendingOverkillGun = null;
        ClearHighlights();
        DestroyC4Visual();
    }



    /// <summary>
    /// Cancel in-progress single-round reload so the player can fire chambered rounds.
    /// </summary>
    public void InterruptTubeReload(Gun gun)
    {
        IsTubeReloading = false;
        if (gun == null)
            return;
        try
        {
            if (gun.Reloading)
                gun.CancelReload();
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] CancelReload: {ex.Message}");
            try { /* best-effort flag clear if CancelReload signature differs */ }
            catch { /* ignore */ }
        }
    }

    public void OnUpgradesApplied(Gun gun)
    {
        boundGun = gun;
        BindHooks(gun, true);
        ApplyGunDataMutations(gun);
        WeaponRegistration.EnsureProjectileBullet(gun);
    }


    public void OnUpgradesCleared(Gun gun)
    {
        BindHooks(gun, false);
        data = prefabSnapshot;
        ResetRuntime();
    }

    private void BindHooks(Gun gun, bool bind)
    {
        if (gun == null)
            return;
        if (bind && hooksBound)
            return;
        if (!bind && !hooksBound)
            return;

        try
        {
            if (bind)
            {
                gun.OnDamageTarget = (DamageCallback)Delegate.Combine(gun.OnDamageTarget, new DamageCallback(OnDamageTarget));
                gun.OnBeforeDamage = (MutableDamageCallback)Delegate.Combine(gun.OnBeforeDamage, new MutableDamageCallback(OnBeforeDamage));
                gun.OnKillTarget = (KillCallback)Delegate.Combine(gun.OnKillTarget, new KillCallback(OnKillTarget));
                if (gun.Player != null)
                {
                    gun.Player.OnSetMovementSpeed += new RefAction<float>(HandleSetMoveSpeed);
                    moveHookBound = true;
                }
                hooksBound = true;
            }
            else
            {
                gun.OnDamageTarget = (DamageCallback)Delegate.Remove(gun.OnDamageTarget, new DamageCallback(OnDamageTarget));
                gun.OnBeforeDamage = (MutableDamageCallback)Delegate.Remove(gun.OnBeforeDamage, new MutableDamageCallback(OnBeforeDamage));
                gun.OnKillTarget = (KillCallback)Delegate.Remove(gun.OnKillTarget, new KillCallback(OnKillTarget));
                if (gun.Player != null && moveHookBound)
                {
                    gun.Player.OnSetMovementSpeed -= new RefAction<float>(HandleSetMoveSpeed);
                    moveHookBound = false;
                }
                hooksBound = false;
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] BindHooks({bind}): {ex.Message}");
        }
    }

    private void HandleSetMoveSpeed(ref float speed)
    {
        // Reposition: hip bonus
        if (data.repositionMoveBonus > 0f)
            speed *= (1f + data.repositionMoveBonus);

        bool aiming = false;
        try { aiming = boundGun != null && boundGun.IsAiming; } catch { /* ignore */ }

        if (aiming && data.repositionAdsMoveMult < 0.99f)
            speed *= data.repositionAdsMoveMult;

        // Overpressure: slow while charging
        if (data.overpressure && boundGun != null)
        {
            try
            {
                if (boundGun.GunData.chargeData.duration > 0.01f &&
                    boundGun.GunData.chargeData.time > 0.01f)
                {
                    speed *= data.overpressureMoveMult;
                }
            }
            catch { /* ignore */ }
        }
    }


    /// <summary>Apply cumulative GunData mutations from upgrade flags after vanilla ApplyUpgrades.</summary>
    public void ApplyGunDataMutations(Gun gun)
    {
        if (gun == null)
            return;
        ref GunData g = ref gun.GunData;

        if (data.heavyGrainDamageMult > 1f)
            g.damage *= data.heavyGrainDamageMult;

        if (data.reserveAmmoMult > 1f)
            g.ammoCapacity = Mathf.Max(1, Mathf.RoundToInt(g.ammoCapacity * data.reserveAmmoMult));

        if (data.bonusBounces > 0)
            g.maxBounces = Mathf.Max(g.maxBounces, data.bonusBounces);

        if (data.extraBulletsPerShot > 0)
            g.bulletsPerShot = Mathf.Max(1, g.bulletsPerShot + data.extraBulletsPerShot);

        // Perforator uses true pierce via SimpleProjectileBullet.OnHit — do NOT add bounces.

        if (data.clipped)
        {
            g.refillAmmoOnReload = true;
            g.reloadDuration = Mathf.Max(0.25f, g.reloadDuration * data.clippedReloadMult);
            data.singleRoundReload = false;
        }

        if (data.autoTrigger)
        {
            // Keep single-round reload identity — only change fire mode / mag / damage / ADS bloom.
            g.automatic = 1;
            g.fireInterval = data.autoTriggerFireInterval;
            g.damage *= data.autoTriggerDamageMult;
            g.magazineSize += data.autoTriggerMagBonus;
            // Do NOT clear singleRoundReload or force refillAmmoOnReload.
        }


        if (data.overpressure)
        {
            g.chargeData.duration = data.overpressureChargeDuration;
            g.chargeData.fireWhenFullyCharged = true;
            g.chargeData.fireOnRelease = false;
            g.chargeData.canFireWhileCharging = false;
            g.chargeData.coolDownSpeed = 2.5f;
            g.damage *= data.overpressureDamageMult;
            g.magazineSize = Mathf.Max(1, g.magazineSize - data.overpressureMagPenalty);
        }

        if (data.rotAmount > 0f)
        {
            g.damageEffect = EffectType.Rot;
            g.damageEffectAmount = Mathf.Max(g.damageEffectAmount, data.rotAmount);
        }
        else if (data.waterAmount > 0f)
        {
            g.damageEffect = EffectType.Water;
            g.damageEffectAmount = Mathf.Max(g.damageEffectAmount, data.waterAmount);
        }
    }

    public void Tick(float dt, Gun gun)
    {
        if (gun == null || !gun.IsOwner)
            return;

        if (gun.IsAiming)
            AimHoldTime += dt;
        else
            AimHoldTime = 0f;

        if (data.exhaustionCooldown > 0f && Time.time >= ExhaustionReadyAt)
            ExhaustionShotArmed = true;

        // One in the Chamber: when mag empties after a full top-off, grant +1 round.
        if (data.oneInTheChamber && ChamberBonusReady && gun.RemainingAmmoCount <= 0)
            OnMagazineEmptied(gun);

        TickDeathMarks(gun);
        TickEchoes(gun);
        TickScouter(gun);
    }


    private void TickDeathMarks(Gun gun)
    {
        if (!data.deathMark || DeathMarks.Count == 0)
            return;

        for (int i = DeathMarks.Count - 1; i >= 0; i--)
        {
            var e = DeathMarks[i];
            if (e.target != null && e.target.Exists() && e.target.IsAlive)
                e.lastPos = e.target.GetHealthbarPosition();
            DeathMarks[i] = e;

            if (Time.time < e.detonateAt)
                continue;

            float dmg = gun.GunData.damage * data.deathMarkDamageScale * (1f + (e.stacks - 1f) * data.deathMarkStackScale);
            float radius = data.deathMarkRadius * (1f + (e.stacks - 1f) * 0.15f);
            try
            {
                GameManager.Instance.SpawnExplosionObserverSeeThrough(
                    gun,
                    e.lastPos,
                    radius,
                    TargetType.NonPlayer,
                    new DamageData(dmg, EffectType.Normal, 0f, DamageFlags.AOE),
                    gun.OwnerClientId);
            }
            catch
            {
                try
                {
                    GameManager.Instance.SpawnExplosionFirstPerson(
                        gun, e.lastPos, radius, TargetType.NonPlayer,
                        new DamageData(dmg, EffectType.Normal, 0f, DamageFlags.AOE), 2f);
                }
                catch { /* API variance */ }
            }

            DeathMarks.RemoveAt(i);
        }
    }

    private void TickEchoes(Gun gun)
    {
        if (data.echoDelay <= 0f || PendingEchoes.Count == 0)
            return;

        for (int i = PendingEchoes.Count - 1; i >= 0; i--)
        {
            if (Time.time < PendingEchoes[i].fireAt)
                continue;

            var echo = PendingEchoes[i];
            PendingEchoes.RemoveAt(i);
            FireEchoBullet(gun, echo);
        }
    }

    private void FireEchoBullet(Gun gun, PendingEcho echo)
    {
        try
        {
            // Prefer cloning the last real fired bullet trajectory.
            BulletData bd;
            if (hasLastFiredBullet)
            {
                bd = lastFiredBullet;
                bd.position = echo.position;
                bd.rotation = echo.rotation;
                bd.direction = echo.rotation * Vector3.forward;
            }
            else
            {
                Vector3 pos = echo.position;
                Quaternion rot = echo.rotation;
                bd = gun.GunData.GetBulletData(ref pos, ref rot);
            }

            bd.damage = echo.damage;
            bd.speed = echo.speed;
            bd.gravity = echo.gravity;
            bd.damageEffect = EffectType.Shock;
            bd.damageEffectAmount = Mathf.Max(data.echoShockAmount, 10f);

            IBullet bullet = gun.GetBullet();
            if (bullet == null)
                return;

            // RailBullet.Kill() invokes onKill without a null check — must pass pool release.
            Action<IBullet> onKill = ResolveBulletRelease(gun);

            gun.ModifyBulletData(ref bd, BulletFlags.OwnerGunBullet);
            bullet.Initialize(bd, gun, onKill, BulletFlags.OwnerGunBullet);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] Echo fire failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Vanilla bullets require a non-null onKill (pool release). RailBullet.Kill NRE's otherwise.
    /// </summary>
    private static Action<IBullet> ResolveBulletRelease(Gun gun)
    {
        try
        {
            var field = AccessTools.Field(typeof(Gun), "releaseBulletToPool");
            if (field != null)
            {
                var del = field.GetValue(gun) as Action<IBullet>;
                if (del != null)
                    return del;
            }
        }
        catch { /* fall through */ }

        // Safe no-op that still satisfies Kill()
        return static _ => { };
    }


    private void TickScouter(Gun gun)
    {
        if (highlightClearAt > 0f && Time.time >= highlightClearAt)
            ClearHighlights();

        if (data.scouterInterval <= 0f || !gun.IsAiming)
            return;
        if (Time.time - LastScouterPulse < data.scouterInterval)
            return;
        LastScouterPulse = Time.time;

        try
        {
            var player = gun.Player;
            if (player == null)
                return;

            // Full upgrade radius (do not shrink). Only move the sphere center to aim.
            float radius = data.scouterRadius > 0f ? data.scouterRadius : 50f;

            Vector3 eye = player.transform.position + Vector3.up * 1.4f;
            Vector3 fwd = player.transform.forward;
            try
            {
                if (gun.playerLook != null)
                {
                    eye = gun.playerLook.transform.position;
                    fwd = gun.playerLook.transform.forward;
                }
            }
            catch { /* ignore */ }

            // Prefer look-ray hit as center; if nothing solid, use a point along aim at ~half radius
            // so the full sphere still covers the view cone without collapsing to a tiny bubble.
            Vector3 center = eye + fwd * (radius * 0.5f);
            int aimMask = ~0;
            try
            {
                // Prefer surface/world geometry; avoid starting inside the local player collider.
                if (Physics.Raycast(eye + fwd * 0.35f, fwd, out RaycastHit aimHit, radius * 2f,
                        aimMask, QueryTriggerInteraction.Ignore))
                {
                    // Ignore hits that are ourselves
                    bool selfHit = false;
                    try
                    {
                        if (aimHit.collider != null &&
                            aimHit.collider.transform.IsChildOf(player.transform))
                            selfHit = true;
                    }
                    catch { /* ignore */ }

                    if (!selfHit)
                        center = aimHit.point;
                }
            }
            catch { /* keep fallback center */ }

            Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            int marked = 0;
            // Soft forward bias: skip things clearly behind the look direction.
            float minDot = 0.15f; // ~81° half-angle — still very wide
            for (int i = 0; i < hits.Length && marked < 32; i++)
            {
                ITarget t = IDamageSource.GetTarget(hits[i]);
                if (t == null || !t.IsAlive)
                    continue;

                try
                {
                    Vector3 to = t.GetHealthbarPosition() - eye;
                    if (to.sqrMagnitude > 0.01f && Vector3.Dot(to.normalized, fwd) < minDot)
                        continue;
                }
                catch { /* include if we can't test */ }

                if (t is ISelectable sel)
                {
                    HighlightTarget(sel, 2.5f);
                    marked++;
                }
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] Scouter: {ex.Message}");
        }
    }



    private void HighlightTarget(ISelectable selectable, float duration)
    {
        if (selectable == null)
            return;
        try
        {
            Highlighter.RegisterObjectForHighlight(selectable);
            if (!activeHighlights.Contains(selectable))
                activeHighlights.Add(selectable);
            highlightClearAt = Mathf.Max(highlightClearAt, Time.time + duration);
        }
        catch { /* ignore */ }
    }

    private void ClearHighlights()
    {
        for (int i = 0; i < activeHighlights.Count; i++)
        {
            try { Highlighter.RemoveObjectFromHighlighting(activeHighlights[i]); }
            catch { /* ignore */ }
        }
        activeHighlights.Clear();
        highlightClearAt = -999f;
    }

    private void DestroyC4Visual()
    {
        if (c4Visual != null)
        {
            try { UnityEngine.Object.Destroy(c4Visual); } catch { /* ignore */ }
            c4Visual = null;
        }
    }


    public void ModifyBulletData(ref BulletData bullet, Gun gun)
    {
        bool longwatch = data.longwatchAimSeconds > 0f && AimHoldTime >= data.longwatchAimSeconds;
        if (longwatch)
        {
            bullet.speed *= data.longwatchSpeedMult;
            bullet.gravity *= data.longwatchGravityMult;
            bullet.range.falloffStartDistance += data.longwatchRangeBonus;
            bullet.range.falloffEndDistance += data.longwatchRangeBonus;
            bullet.range.maxDamageRange += data.longwatchRangeBonus;
        }

        // Ricochet: baseline magnetism; post-bounce boost applied in Bounce postfix.
        if (data.bonusBounces > 0)
            bullet.maxBounces = Mathf.Max(bullet.maxBounces, data.bonusBounces);

        if (data.rotAmount > 0f)
        {
            bullet.damageEffect = EffectType.Rot;
            bullet.damageEffectAmount = Mathf.Max(bullet.damageEffectAmount, data.rotAmount);
        }
        else if (data.waterAmount > 0f)
        {
            bullet.damageEffect = EffectType.Water;
            bullet.damageEffectAmount = Mathf.Max(bullet.damageEffectAmount, data.waterAmount);
        }

        // Perforator: do NOT set maxBounces — pierce is handled in OnHit prefix.
    }

    public void OnBulletFired(IBullet bullet, ref BulletData bulletData)
    {
        lastFiredBullet = bulletData;
        hasLastFiredBullet = true;

        // Perforator: track pierce count on projectile bullets.
        if (data.pierceTargets > 0 && bullet != null)
            SniperProjectilePierceHook.ResetBullet(bullet);
    }


    public void OnShotFired(Gun gun)
    {
        TwinShotHits.Clear();

        // One in the Chamber: after the bonus round is actually fired, allow reload again.
        if (BlockAutoReloadForChamber)
        {
            BlockAutoReloadForChamber = false;
            try
            {
                // Restore auto-reload from catalog baseline if needed
                if (gun.Prefab is IWeapon prefabW)
                    gun.GunData.autoReloadWhenEmpty = prefabW.GunData.autoReloadWhenEmpty;
                else
                    gun.GunData.autoReloadWhenEmpty = true;
            }
            catch { /* ignore */ }
        }

        if (data.echoDelay > 0f && gun != null)
        {
            try
            {
                Vector3 pos = hasLastFiredBullet
                    ? lastFiredBullet.position
                    : (gun.GunData.firePoint != null ? gun.GunData.firePoint.position : gun.transform.position);
                Quaternion rot = hasLastFiredBullet
                    ? lastFiredBullet.rotation
                    : (gun.Player != null ? gun.Player.playerLook.transform.rotation : gun.transform.rotation);

                PendingEchoes.Add(new PendingEcho
                {
                    fireAt = Time.time + data.echoDelay,
                    position = pos,
                    rotation = rot,
                    damage = gun.GunData.damage * (data.echoDamageScale > 0f ? data.echoDamageScale : 0.85f),
                    speed = hasLastFiredBullet ? lastFiredBullet.speed : gun.GunData.bulletSpeed,
                    gravity = hasLastFiredBullet ? lastFiredBullet.gravity : gun.GunData.bulletGravity
                });
            }
            catch { /* ignore */ }
        }
    }

    public void OnMagazineEmptied(Gun gun)
    {
        if (!data.oneInTheChamber || !ChamberBonusReady || gun == null)
            return;

        gun.RemainingAmmo = 1f;
        ChamberBonusReady = false;
        BlockAutoReloadForChamber = true;
        try { gun.GunData.autoReloadWhenEmpty = false; } catch { /* ignore */ }
    }

    public void OnFullReloadCompleted(Gun gun)
    {
        if (data.oneInTheChamber)
        {
            LastReloadWasFullTopOff = true;
            ChamberBonusReady = true;
            BlockAutoReloadForChamber = false;
        }
    }



    private void OnBeforeDamage(ref DamageCallbackData callback)
    {
        Gun g = callback.source as Gun ?? callback.source?.ParentSource as Gun;
        if (g == null || !IsOurGear(g))
            return;

        ITarget target = callback.target;
        if (target == null)
            return;

        // Hullbreaker — mutate incoming damage vs shells (EnemyComponentType.Shell)
        if (data.shellDamageMult > 1.01f && IsShellTarget(target))
            callback.damageData.damage *= data.shellDamageMult;

        // Subsonic — full HP via NormalizedHealth / Health ratio
        if (data.fullHpDamageMult > 1.01f)
        {
            float frac = GetHealthFraction(target);
            if (frac >= 0.98f)
                callback.damageData.damage *= data.fullHpDamageMult;
        }

        // Synchronize — ally damaged this target recently → massive damage
        if (data.syncWindow > 0f && data.syncDamageMult > 1.01f)
        {
            if (SniperAllyDamageTracker.WasDamagedByAlly(target, g, data.syncWindow))
                callback.damageData.damage *= data.syncDamageMult;
        }

        // Overkill — stash overflow vs shells so kill path can carry to parent layer
        if (data.overkill && IsShellTarget(target) && target is EnemyPart shellPart)
        {
            try
            {
                float hp = shellPart.Health;
                float dmg = callback.damageData.damage;
                if (hp > 0f && dmg > hp + 0.01f)
                {
                    pendingOverkill = dmg - hp;
                    pendingOverkillParent = shellPart.Parent as ITarget;
                    pendingOverkillPos = callback.position;
                    pendingOverkillGun = g;
                    pendingOverkillEffect = callback.damageData.effect;
                    pendingOverkillEffectAmt = callback.damageData.effectAmount;
                    hasPendingOverkill = true;
                }
            }
            catch { /* ignore */ }
        }

        // Transfer Relay: if immunity would negate, still apply portion as direct
        if (data.transferRelayScale > 0f && callback.damageData.damage <= 0.01f)
        {
            try
            {
                float pulse = g.GunData.damage * data.transferRelayScale;
                IDamageSource.DamageTarget(g, target,
                    new DamageData(pulse, EffectType.IgnoreImmunity, 0f, DamageFlags.None),
                    callback.position, null);
            }
            catch { /* ignore */ }
        }
    }

    private bool hasPendingOverkill;
    private float pendingOverkill;
    private ITarget pendingOverkillParent;
    private Vector3 pendingOverkillPos;
    private Gun pendingOverkillGun;
    private EffectType pendingOverkillEffect;
    private float pendingOverkillEffectAmt;

    private void ApplyPendingOverkill()
    {
        if (!hasPendingOverkill)
            return;

        float overflow = pendingOverkill;
        ITarget parent = pendingOverkillParent;
        Vector3 pos = pendingOverkillPos;
        Gun gun = pendingOverkillGun;
        EffectType fx = pendingOverkillEffect;
        float fxAmt = pendingOverkillEffectAmt;

        hasPendingOverkill = false;
        pendingOverkill = 0f;
        pendingOverkillParent = null;
        pendingOverkillGun = null;

        if (overflow <= 0.01f || parent == null || !parent.IsAlive || gun == null)
            return;

        try
        {
            IDamageSource.DamageTarget(gun, parent,
                new DamageData(overflow, fx, fxAmt * 0.5f, DamageFlags.None),
                pos, null);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] Overkill carry: {ex.Message}");
        }
    }


    private static bool IsShellTarget(ITarget target)
    {
        try
        {
            if (target is EnemyPart part)
            {
                // Shell flag on component type
                if ((part.ComponentType & EnemyComponentType.Shell) != 0)
                    return true;
                // Not a core — outer armor/parts count as shell-like for Hullbreaker
                if ((part.ComponentType & EnemyComponentType.Core) == 0 &&
                    part.ComponentType != EnemyComponentType.Behaviour)
                    return true;
            }
        }
        catch { /* ignore */ }

        string typeName = (target as Component)?.GetType().Name ?? target.GetType().Name;
        return typeName.IndexOf("Shell", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static float GetHealthFraction(ITarget target)
    {
        try
        {
            if (target is EnemyPart ep)
                return ep.NormalizedHealth;
        }
        catch { /* ignore */ }

        try
        {
            float max = target.MaxHealth;
            if (max > 0f && target is EnemyPart p)
                return p.Health / max;
        }
        catch { /* ignore */ }

        return 0.5f; // unknown → don't trigger Subsonic
    }

    private void OnDamageTarget(in DamageCallbackData callback)
    {
        if (callback.source == null)
            return;

        Gun gun = callback.source as Gun ?? callback.source.ParentSource as Gun;
        if (gun == null || !IsOurGear(gun))
            return;
        if (callback.damageData.damage <= 0f)
            return;

        ITarget target = callback.target;
        if (target == null)
            return;

        // Clear chamber block after the bonus round actually deals damage
        if (BlockAutoReloadForChamber && gun.RemainingAmmoCount <= 0)
            BlockAutoReloadForChamber = false;

        // Death Mark
        if (data.deathMark)
            ApplyDeathMark(target, callback.position, headshot: false);

        // Spotter — vanilla highlight, not explosion VFX
        if (data.spotterRadius > 0f)
        {
            float dur = data.spotterDuration > 0f ? data.spotterDuration : 6f;
            if (target is ISelectable mainSel)
                HighlightTarget(mainSel, dur);

            try
            {
                Collider[] near = Physics.OverlapSphere(
                    callback.position, data.spotterRadius, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < near.Length && i < 32; i++)
                {
                    ITarget t = IDamageSource.GetTarget(near[i]);
                    if (t == null || !t.IsAlive || t == target)
                        continue;
                    if (t is ISelectable sel)
                        HighlightTarget(sel, dur);
                }
            }
            catch { /* ignore */ }
        }

        // Myco splash rot
        if (data.rotSplashRadius > 0f && data.rotAmount > 0f)
        {
            try
            {
                GameManager.Instance.SpawnExplosionFirstPerson(
                    gun, callback.position, data.rotSplashRadius, TargetType.NonPlayer,
                    new DamageData(0f, EffectType.Rot, data.rotAmount, DamageFlags.AOE), 1.5f);
            }
            catch { /* ignore */ }
        }

        // Twin link tracking
        if (data.twinLinkFenceDamage > 0f && !TwinShotHits.Contains(target))
        {
            TwinShotHits.Add(target);
            if (TwinShotHits.Count >= 2)
                TryTwinFence(gun);
        }

        // Exhaustion
        if (ExhaustionShotArmed && data.exhaustionSlowDuration > 0f)
        {
            ExhaustionShotArmed = false;
            ExhaustionReadyAt = Time.time + data.exhaustionCooldown;
            try
            {
                target.SlowTargetThisTick(Mathf.Clamp01(1f - data.exhaustionSlowStrength * 0.05f));
                IDamageSource.DamageTarget(gun, target,
                    new DamageData(0f, EffectType.Cryo, data.exhaustionSlowStrength, DamageFlags.None),
                    callback.position, null);
            }
            catch { /* slow via cryo buildup */ }
        }

        // Disrupt Channel — heavy hit vs shields
        if (data.disruptDuration > 0f)
        {
            try
            {
                string n = (target as Component)?.GetType().Name ?? "";
                if (n.IndexOf("Shield", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Bubble", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    IDamageSource.DamageTarget(gun, target,
                        new DamageData(gun.GunData.damage * 3.5f, EffectType.IgnoreImmunity, 0f, DamageFlags.None),
                        callback.position, null);
                }
            }
            catch { /* ignore */ }
        }
    }


    private void ApplyDeathMark(ITarget target, Vector3 pos, bool headshot)
    {
        float fuse = data.deathMarkFuse * (headshot ? data.deathMarkHeadshotFuseMult : 1f);
        for (int i = 0; i < DeathMarks.Count; i++)
        {
            if (DeathMarks[i].target == target)
            {
                var e = DeathMarks[i];
                e.stacks += 1f;
                e.detonateAt = Mathf.Min(e.detonateAt, Time.time + fuse);
                e.lastPos = pos;
                DeathMarks[i] = e;
                return;
            }
        }

        DeathMarks.Add(new DeathMarkEntry
        {
            target = target,
            detonateAt = Time.time + fuse,
            stacks = 1f,
            lastPos = pos,
            fromHeadshot = headshot
        });
    }

    private void TryTwinFence(Gun gun)
    {
        if (TwinShotHits.Count < 2)
            return;
        try
        {
            ITarget t0 = TwinShotHits[0];
            ITarget t1 = TwinShotHits[1];
            Vector3 a = t0.GetHealthbarPosition();
            Vector3 b = t1.GetHealthbarPosition();
            float dist = Vector3.Distance(a, b);
            if (dist < 0.05f)
            {
                TwinShotHits.Clear();
                return;
            }

            // Instant high Shock on both linked targets (not low DoT ticks).
            float shockAmt = Mathf.Max(14f, data.twinLinkFenceDamage * 0.15f);
            float dmg = data.twinLinkFenceDamage;

            try
            {
                IDamageSource.DamageTarget(gun, t0,
                    new DamageData(dmg * 0.5f, EffectType.Shock, shockAmt, DamageFlags.None), a, null);
                IDamageSource.DamageTarget(gun, t1,
                    new DamageData(dmg * 0.5f, EffectType.Shock, shockAmt, DamageFlags.None), b, null);
            }
            catch { /* ignore */ }

            // Single mid-line burst for the fence visual / splash (instant, not DoT).
            Vector3 mid = (a + b) * 0.5f;
            try
            {
                GameManager.Instance.SpawnExplosionFirstPerson(
                    gun, mid, Mathf.Max(1.2f, data.twinLinkFenceRadius * 0.5f), TargetType.NonPlayer,
                    new DamageData(dmg * 0.35f, EffectType.Shock, shockAmt, DamageFlags.AOE), 1.2f);
            }
            catch { /* ignore */ }

            // Parent cores also get an instant shock pulse.
            for (int i = 0; i < 2; i++)
            {
                if (TwinShotHits[i] is EnemyPart part && part.Parent is ITarget core && core.IsAlive)
                {
                    try
                    {
                        IDamageSource.DamageTarget(gun, core,
                            new DamageData(dmg * 0.4f, EffectType.Shock, shockAmt, DamageFlags.None),
                            core.GetHealthbarPosition(), null);
                    }
                    catch { /* ignore */ }
                }
            }
        }
        catch { /* ignore */ }
        TwinShotHits.Clear();
    }



    private void OnKillTarget(in KillCallbackData callback)
    {
        if (callback.source == null)
            return;
        Gun gun = callback.source as Gun ?? callback.source.ParentSource as Gun;
        if (gun == null || !IsOurGear(gun))
            return;
        LastKillTime = Time.time;

        // Overkill: shell destroyed — apply leftover to parent layer
        if (data.overkill)
            ApplyPendingOverkill();
    }


    public bool IsDeadboltActive()
    {
        return data.deadboltDuration > 0f && Time.time - LastKillTime <= data.deadboltDuration;
    }

    public bool IsLongwatchActive()
    {
        return data.longwatchAimSeconds > 0f && AimHoldTime >= data.longwatchAimSeconds;
    }

    public bool IsAnchored(Gun gun)
    {
        if (!data.anchor || gun?.Player == null)
            return false;
        try
        {
            Vector3 v = gun.Player.Velocity;
            v.y = 0f;
            return v.sqrMagnitude <= data.anchorSpeedThreshold * data.anchorSpeedThreshold
                   && !gun.Player.IsSprinting && !gun.Player.Sliding;
        }
        catch
        {
            return false;
        }
    }

    public void TryThrowOrDetonateC4(Gun gun)
    {
        if (!data.highExplosive || gun == null || !gun.IsOwner)
            return;

        if (C4Deployed)
        {
            try
            {
                GameManager.Instance.SpawnExplosionFirstPerson(
                    gun, C4Position, data.c4Radius, TargetType.NonPlayer,
                    new DamageData(data.c4Damage, EffectType.Normal, 0f, DamageFlags.AOE), 3f);
            }
            catch
            {
                try
                {
                    GameManager.Instance.SpawnExplosionObserverSeeThrough(
                        gun, C4Position, data.c4Radius, TargetType.NonPlayer,
                        new DamageData(data.c4Damage, EffectType.Normal, 0f, DamageFlags.AOE),
                        gun.OwnerClientId);
                }
                catch { /* ignore */ }
            }
            DestroyC4Visual();
            C4Deployed = false;
            return;
        }

        // Throw / place ahead of player
        try
        {
            var look = gun.Player?.playerLook;
            Vector3 origin = look != null ? look.transform.position : gun.transform.position;
            Vector3 fwd = look != null ? look.transform.forward : gun.transform.forward;
            C4Position = origin + fwd * data.c4ThrowForce * 0.65f + Vector3.up * 0.5f;
            if (Physics.Raycast(C4Position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 40f))
                C4Position = hit.point + Vector3.up * 0.15f;
            C4Deployed = true;
            C4DeployedAt = Time.time;
            SpawnC4Visual(C4Position);
        }
        catch
        {
            C4Position = gun.transform.position + gun.transform.forward * 10f;
            C4Deployed = true;
            C4DeployedAt = Time.time;
            SpawnC4Visual(C4Position);
        }
    }

    private void SpawnC4Visual(Vector3 pos)
    {
        DestroyC4Visual();
        try
        {
            c4Visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c4Visual.name = "BallisticSniper_C4";
            c4Visual.transform.position = pos;
            c4Visual.transform.localScale = Vector3.one * 0.35f;
            // Untextured small cube — strip collider so it doesn't block shots
            var col = c4Visual.GetComponent<Collider>();
            if (col != null)
                UnityEngine.Object.Destroy(col);
            var rend = c4Visual.GetComponent<Renderer>();
            if (rend != null)
            {
                // Default material, slightly dark
                rend.material.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] C4 visual: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        if (boundGun != null)
            BindHooks(boundGun, false);
        ClearHighlights();
        DestroyC4Visual();
    }


    public static bool TryGet(IGear gear, out BallisticSniperBehaviour behaviour)
    {
        behaviour = null;
        if (gear?.gameObject == null)
            return false;

        behaviour = gear.gameObject.GetComponent<BallisticSniperBehaviour>();
        if (behaviour != null)
            return true;

        bool isOurs = gear.Info != null &&
                      (gear.Info.APIName == SparrohPlugin.GearApiName ||
                       gear.Info.ID == SparrohPlugin.GearId);

        BallisticSniperBehaviour prefabBehaviour = null;
        if (gear.Prefab is Component prefabComp)
            prefabBehaviour = prefabComp.GetComponent<BallisticSniperBehaviour>();

        if (!isOurs && prefabBehaviour == null)
            return false;

        string desc = prefabBehaviour != null
            ? prefabBehaviour.Description
            : SparrohPlugin.GearDescription;

        behaviour = gear.gameObject.AddComponent<BallisticSniperBehaviour>();
        behaviour.InitializeAsPrefab(desc);
        if (prefabBehaviour != null)
            behaviour.CopyFrom(prefabBehaviour);
        return true;
    }

    public static bool IsOurGear(IGear gear)
    {
        if (gear?.Info == null)
            return false;
        return gear.Info.APIName == SparrohPlugin.GearApiName ||
               gear.Info.ID == SparrohPlugin.GearId;
    }

    public static bool IsOurGear(IUpgradable gear)
    {
        if (gear?.Info == null)
            return false;
        return gear.Info.APIName == SparrohPlugin.GearApiName ||
               gear.Info.ID == SparrohPlugin.GearId;
    }
}
