using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Pigeon.Movement;
using UnityEngine;

/// <summary>
/// Combat / tick / upgrade lifecycle hooks for Anti-Material Rifle upgrades.
/// </summary>
internal static class BallisticSniperCombatHooks
{
    public static void Apply(Harmony harmony)
    {
        TryPatch(harmony, typeof(SniperGunUpdateHook));
        TryPatch(harmony, typeof(SniperModifyBulletHook));
        TryPatch(harmony, typeof(SniperCartridgeModifyBulletHook));
        TryPatch(harmony, typeof(SniperOnFireHook));
        TryPatch(harmony, typeof(SniperCartridgeOnFireHook));
        TryPatch(harmony, typeof(SniperOnFiredBulletHook));
        TryPatch(harmony, typeof(SniperRecoilHook));
        TryPatch(harmony, typeof(SniperUpgradesEnabledHook));
        TryPatch(harmony, typeof(SniperUpgradesDisabledHook));
        TryPatch(harmony, typeof(SniperReloadHoldC4Hook));
        TryPatch(harmony, typeof(SniperCanReloadHook));
        // Projectile-path upgrades (standard bullet, not RailBullet).
        TryPatch(harmony, typeof(SniperProjectileBounceHomingHook));
        TryPatch(harmony, typeof(SniperProjectilePierceHook));
        TryPatch(harmony, typeof(SniperAllyDamageTracker));
    }

    private static void TryPatch(Harmony harmony, Type patchClass)
    {
        try { harmony.PatchAll(patchClass); }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogWarning(
                $"[BallisticSniper] Skipped patch {patchClass.Name}: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(Gun), "Update")]
internal static class SniperGunUpdateHook
{
    [HarmonyPostfix]
    private static void Postfix(Gun __instance)
    {
        try
        {
            if (__instance == null || !__instance.IsOwner)
                return;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return;
            sniper.Tick(Time.deltaTime, __instance);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] Update: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(Gun), nameof(Gun.ModifyBulletData))]
internal static class SniperModifyBulletHook
{
    [HarmonyPostfix]
    private static void Postfix(Gun __instance, ref BulletData data)
    {
        try
        {
            if (__instance == null || __instance is CartridgeSMG)
                return;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return;
            sniper.ModifyBulletData(ref data, __instance);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] ModifyBullet: {ex.Message}");
        }
    }
}

/// <summary>CartridgeSMG overrides ModifyBulletData without calling base.</summary>
[HarmonyPatch(typeof(CartridgeSMG), nameof(CartridgeSMG.ModifyBulletData))]
internal static class SniperCartridgeModifyBulletHook
{
    [HarmonyPostfix]
    private static void Postfix(CartridgeSMG __instance, ref BulletData data, BulletFlags flags)
    {
        try
        {
            if (__instance == null)
                return;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return;
            sniper.ModifyBulletData(ref data, __instance);
            _ = flags;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] SMG ModifyBullet: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(Gun), "OnFire")]
internal static class SniperOnFireHook
{
    [HarmonyPostfix]
    private static void Postfix(Gun __instance)
    {
        try
        {
            if (__instance == null || !__instance.IsOwner)
                return;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return;
            sniper.OnShotFired(__instance);
        }
        catch { /* signature variance */ }
    }
}

[HarmonyPatch(typeof(CartridgeSMG), "OnFire")]
internal static class SniperCartridgeOnFireHook
{
    [HarmonyPostfix]
    private static void Postfix(CartridgeSMG __instance, int numBullets)
    {
        try
        {
            if (__instance == null || !__instance.IsOwner)
                return;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return;
            sniper.OnShotFired(__instance);
            _ = numBullets;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] Cartridge OnFire: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(Gun), "OnFiredBullet")]
internal static class SniperOnFiredBulletHook
{
    [HarmonyPostfix]
    private static void Postfix(
        Gun __instance, IBullet bullet, BulletFlags flags, int shotIndex, ref BulletData bulletData)
    {
        try
        {
            if (__instance == null || bullet == null)
                return;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return;
            sniper.OnBulletFired(bullet, ref bulletData);
            _ = flags;
            _ = shotIndex;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] OnFiredBullet: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(Gun), "AddRecoil")]
internal static class SniperRecoilHook
{
    [HarmonyPrefix]
    private static void Prefix(Gun __instance, ref float multiplier)
    {
        try
        {
            if (__instance == null)
                return;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return;
            if (sniper.IsAnchored(__instance))
                multiplier = 0f;
        }
        catch { /* ignore */ }
    }
}

[HarmonyPatch(typeof(Gun), "OnUpgradesEnabled")]
internal static class SniperUpgradesEnabledHook
{
    [HarmonyPostfix]
    private static void Postfix(Gun __instance)
    {
        try
        {
            if (__instance == null)
                return;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return;
            sniper.OnUpgradesApplied(__instance);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] OnUpgradesEnabled: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(Gun), "OnUpgradesDisabled")]
internal static class SniperUpgradesDisabledHook
{
    [HarmonyPrefix]
    private static void Prefix(Gun __instance)
    {
        try
        {
            if (__instance == null)
                return;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return;
            sniper.OnUpgradesCleared(__instance);
        }
        catch { /* ignore */ }
    }
}

/// <summary>High Explosive: reload with full mag throws/detonates C4.</summary>
[HarmonyPatch(typeof(Gun), "OnReloadPerformed")]
internal static class SniperReloadHoldC4Hook
{
    [HarmonyPrefix]
    private static bool Prefix(Gun __instance)
    {
        try
        {
            if (__instance == null || !__instance.IsOwner)
                return true;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return true;
            if (!sniper.WeaponData.highExplosive)
                return true;

            if (sniper.C4Deployed)
            {
                sniper.TryThrowOrDetonateC4(__instance);
                return false;
            }

            if (__instance.RemainingAmmoCount >= __instance.GunData.magazineSize)
            {
                sniper.TryThrowOrDetonateC4(__instance);
                return false;
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] C4 reload: {ex.Message}");
        }

        return true;
    }
}

/// <summary>
/// One in the Chamber: only block auto-reload while the phantom round is held.
/// </summary>
[HarmonyPatch(typeof(Gun), "CanReload")]
internal static class SniperCanReloadHook
{
    [HarmonyPrefix]
    private static bool Prefix(Gun __instance, ref bool __result)
    {
        try
        {
            if (__instance == null || !__instance.IsOwner)
                return true;
            if (!BallisticSniperBehaviour.TryGet(__instance, out var sniper))
                return true;
            if (!sniper.WeaponData.oneInTheChamber)
                return true;

            if (sniper.BlockAutoReloadForChamber && __instance.RemainingAmmoCount > 0)
            {
                __result = false;
                return false;
            }
        }
        catch { /* ignore */ }
        return true;
    }
}

/// <summary>
/// Ricochet Protocol on SimpleProjectileBullet: after bounce, retarget toward nearest enemy.
/// </summary>
[HarmonyPatch(typeof(SimpleProjectileBullet), "Bounce")]
internal static class SniperProjectileBounceHomingHook
{
    [HarmonyPostfix]
    private static void Postfix(SimpleProjectileBullet __instance, ref RaycastHit hit)
    {
        try
        {
            if (__instance == null)
                return;
            if (__instance.ParentSource is not Gun gun)
                return;
            if (!BallisticSniperBehaviour.TryGet(gun, out var sniper))
                return;
            if (sniper.WeaponData.postBounceHoming <= 0f && sniper.WeaponData.bonusBounces <= 0)
                return;

            var dataField = AccessTools.Field(typeof(SimpleProjectileBullet), "data");
            var velField = AccessTools.Field(typeof(SimpleProjectileBullet), "velocity");
            if (dataField == null)
                return;

            object boxed = dataField.GetValue(__instance);
            if (boxed is not BulletData bd)
                return;

            Vector3 origin = hit.point;
            float searchRadius = Mathf.Max(25f, sniper.WeaponData.postBounceHoming * 8f);
            ITarget best = null;
            float bestDist = float.MaxValue;

            int mask = bd.targetCollisionMask;
            if (mask == 0)
                mask = ~0;
            Collider[] cols = Physics.OverlapSphere(origin, searchRadius, mask, QueryTriggerInteraction.Ignore);

            ITarget hitTarget = IDamageSource.GetTarget(hit.collider);
            for (int i = 0; i < cols.Length; i++)
            {
                ITarget t = IDamageSource.GetTarget(cols[i]);
                if (t == null || !t.IsAlive || t == hitTarget)
                    continue;
                Vector3 tp = t.GetHealthbarPosition();
                float d = (tp - origin).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }

            if (best != null)
            {
                Vector3 to = (best.GetHealthbarPosition() - origin).normalized;
                float strength = Mathf.Clamp01(sniper.WeaponData.postBounceHoming / 8f);
                strength = Mathf.Max(strength, 0.75f);
                bd.direction = Vector3.Slerp(bd.direction, to, strength).normalized;
                dataField.SetValue(__instance, bd);

                if (velField != null)
                {
                    float speed = ((Vector3)velField.GetValue(__instance)).magnitude;
                    if (speed < 1f)
                        speed = bd.speed > 0f ? bd.speed : 220f;
                    velField.SetValue(__instance, bd.direction * speed);
                }
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] Projectile bounce homing: {ex.Message}");
        }
    }
}

/// <summary>
/// Perforator on SimpleProjectileBullet: allow multi-target pierce with damage falloff.
/// Vanilla OnHit kills the projectile after one target (unless bouncing). We keep it alive
/// and skip bounce when pierce remains.
/// </summary>
[HarmonyPatch(typeof(SimpleProjectileBullet), "OnHit")]
internal static class SniperProjectilePierceHook
{
    private static readonly Dictionary<int, int> HitCounts = new Dictionary<int, int>(32);
    private static readonly FieldInfo BouncesField =
        AccessTools.Field(typeof(SimpleProjectileBullet), "bounces");
    private static readonly FieldInfo DataField =
        AccessTools.Field(typeof(SimpleProjectileBullet), "data");
    private static readonly FieldInfo IsAliveField =
        AccessTools.Field(typeof(SimpleProjectileBullet), "isAlive");
    private static readonly FieldInfo VelocityField =
        AccessTools.Field(typeof(SimpleProjectileBullet), "velocity");
    private static readonly FieldInfo PositionNextField =
        AccessTools.Field(typeof(SimpleProjectileBullet), "positionNext");

    public static void ResetBullet(IBullet bullet)
    {
        if (bullet == null) return;
        HitCounts[bullet.GetHashCode()] = 0;
    }

    [HarmonyPrefix]
    private static bool Prefix(SimpleProjectileBullet __instance, ref RaycastHit hit, ref Vector3 direction, ITarget target)
    {
        try
        {
            if (__instance == null)
                return true;
            if (__instance.ParentSource is not Gun gun)
                return true;
            if (!BallisticSniperBehaviour.TryGet(gun, out var sniper))
                return true;
            if (sniper.WeaponData.pierceTargets <= 0)
                return true;

            // Surface hit with no target — let vanilla handle (stop / bounce).
            if (target == null || !IBullet.CanDamageTarget(target, __instance))
                return true;

            int id = __instance.GetHashCode();
            HitCounts.TryGetValue(id, out int count);
            int max = sniper.WeaponData.pierceTargets;

            if (count >= max)
                return true; // final hit — vanilla kills projectile

            // Apply scaled damage ourselves, then continue flight.
            float falloff = sniper.WeaponData.pierceFalloff > 0f
                ? sniper.WeaponData.pierceFalloff
                : 0.25f;
            float mult = count == 0
                ? 1f
                : Mathf.Pow(Mathf.Clamp01(1f - falloff), count);

            if (__instance.Flags.IsOwner())
            {
                BulletData bd = DataField != null && DataField.GetValue(__instance) is BulletData d
                    ? d
                    : default;
                float rangeMult = 1f;
                try
                {
                    rangeMult = bd.range.GetDamageMultiplier((hit.point - bd.position).magnitude);
                }
                catch { /* ignore */ }

                float dmg = bd.damage * rangeMult * mult;
                float fx = bd.damageEffectAmount * rangeMult * mult;
                IDamageSource.DamageTarget(__instance, target,
                    new DamageData(dmg, bd.damageEffect, fx, bd.damageFlags),
                    hit.point, hit.collider);
            }

            HitCounts[id] = count + 1;

            // Nudge past the hit so we don't re-collide the same collider immediately.
            Vector3 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : __instance.transform.forward;
            Vector3 next = hit.point + dir * 0.35f;
            if (PositionNextField != null)
                PositionNextField.SetValue(__instance, next);
            if (DataField != null && DataField.GetValue(__instance) is BulletData data)
            {
                data.position = next;
                DataField.SetValue(__instance, data);
            }
            if (IsAliveField != null)
                IsAliveField.SetValue(__instance, true);

            // Skip vanilla OnHit (would bounce or kill).
            return false;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] Projectile pierce: {ex.Message}");
            return true;
        }
    }
}

/// <summary>
/// Tracks recent damage sources per target so Synchronize can detect ally hits.
/// </summary>
[HarmonyPatch(typeof(EnemyPart), nameof(EnemyPart.Damage))]
internal static class SniperAllyDamageTracker
{
    private struct HitRecord
    {
        public ulong ownerId;
        public float time;
    }

    // target instance id → last few hits
    private static readonly Dictionary<int, List<HitRecord>> Recent =
        new Dictionary<int, List<HitRecord>>(64);

    [HarmonyPostfix]
    private static void Postfix(EnemyPart __instance, DamageData data, IDamageSource source, Vector3 position)
    {
        try
        {
            if (__instance == null || source == null)
                return;

            ulong owner = ResolveOwnerId(source);
            if (owner == ulong.MaxValue)
                return;

            int key = __instance.GetInstanceID();
            if (!Recent.TryGetValue(key, out var list))
            {
                list = new List<HitRecord>(4);
                Recent[key] = list;
            }

            // Prune old
            float now = Time.time;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (now - list[i].time > 6f)
                    list.RemoveAt(i);
            }

            list.Add(new HitRecord { ownerId = owner, time = now });
            if (list.Count > 8)
                list.RemoveAt(0);
        }
        catch { /* ignore */ }
        _ = data;
        _ = position;
    }

    public static bool WasDamagedByAlly(ITarget target, Gun selfGun, float windowSeconds)
    {
        if (target == null || selfGun == null || windowSeconds <= 0f)
            return false;

        try
        {
            int key;
            if (target is EnemyPart ep)
                key = ep.GetInstanceID();
            else if (target is Component c)
                key = c.GetInstanceID();
            else
                return false;

            if (!Recent.TryGetValue(key, out var list) || list.Count == 0)
                return false;

            ulong selfId = selfGun.OwnerClientId;
            float now = Time.time;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (now - list[i].time > windowSeconds)
                    continue;
                if (list[i].ownerId != selfId && list[i].ownerId != ulong.MaxValue)
                    return true;
            }
        }
        catch { /* ignore */ }

        return false;
    }

    private static ulong ResolveOwnerId(IDamageSource source)
    {
        try
        {
            IDamageSource bas = source.GetBase();
            if (bas is Gun g)
                return g.OwnerClientId;
            if (bas is Player p)
                return p.OwnerClientId;
            if (source is Gun sg)
                return sg.OwnerClientId;
        }
        catch { /* ignore */ }
        return ulong.MaxValue;
    }
}
