using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;



/// <summary>
/// Single-round (tube-style) reload for the Anti-Material Rifle.
///
/// Catalog sets refillAmmoOnReload = false so vanilla OnAmmoLoaded only unsubscribes
/// its animation callback and does not fill the mag. This postfix then loads one shell
/// and restarts reload until full, out of reserve, or interrupted.
///
/// Per-shell duration = GetReloadDuration() / magazineSize so a full empty reload
/// still lands near the designed total (≈3.1s for mag 5).
/// </summary>
[HarmonyPatch(typeof(Gun), "OnAmmoLoaded")]
internal static class BallisticSniperReloadHook
{
    [HarmonyPostfix]
    private static void Postfix(Gun __instance)
    {
        try
        {
            if (__instance == null || !__instance.IsOwner)
                return;

            if (!BallisticSniperBehaviour.TryGet(__instance, out BallisticSniperBehaviour behaviour))
                return;

            // Clipped / full-mag path: arm One in the Chamber when mag is full.
            if (!behaviour.SingleRoundReload)
            {
                if (__instance.RemainingAmmoCount >= __instance.GunData.magazineSize)
                    behaviour.OnFullReloadCompleted(__instance);
                return;
            }

            LoadOneRound(__instance, behaviour);

        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogError($"[BallisticSniper] OnAmmoLoaded hook failed: {ex}");
        }
    }

    private static void LoadOneRound(Gun gun, BallisticSniperBehaviour behaviour)
    {
        ref GunData data = ref gun.GunData;
        int magSize = Mathf.Max(data.magazineSize, 1);
        int current = gun.RemainingAmmoCount;

        if (current >= magSize)
        {
            behaviour.IsTubeReloading = false;
            // Full top-off completed — arm One in the Chamber.
            behaviour.OnFullReloadCompleted(gun);
            return;
        }


        bool loaded = false;

        if (data.hasLimitedAmmo)
        {
            if (gun.StoredAmmo >= 1f)
            {
                gun.StoredAmmo -= 1f;
                gun.RemainingAmmo = current + 1;
                loaded = true;
            }
        }
        else
        {
            gun.RemainingAmmo = current + 1;
            loaded = true;
        }

        if (!loaded)
        {
            behaviour.IsTubeReloading = false;
            return;
        }

        bool canContinue = gun.RemainingAmmoCount < magSize &&
                           (!data.hasLimitedAmmo || gun.StoredAmmo >= 1f);

        if (canContinue)
        {
            behaviour.IsTubeReloading = true;
            gun.StartCoroutine(ContinueReloadAfterCurrent(gun));
        }
        else
        {
            behaviour.IsTubeReloading = false;
            if (gun.RemainingAmmoCount >= magSize)
                behaviour.OnFullReloadCompleted(gun);
        }

    }

    /// <summary>
    /// Wait until the current reload animation finishes (Reloading becomes false),
    /// then start the next single-round cycle.
    /// </summary>
    private static IEnumerator ContinueReloadAfterCurrent(Gun gun)
    {
        // Let OnReloadFinished run this frame / next frames.
        float timeout = 5f;
        while (gun != null && gun.Reloading && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        yield return null;

        if (gun == null || !gun.IsOwner || !gun.Active)
            yield break;

        if (!BallisticSniperBehaviour.TryGet(gun, out BallisticSniperBehaviour behaviour))
            yield break;

        // Fire-interrupt or Clipped cleared the tube chain.
        if (!behaviour.IsTubeReloading || !behaviour.SingleRoundReload)
            yield break;

        if (gun.Reloading)
            yield break;

        ref GunData data = ref gun.GunData;
        if (gun.RemainingAmmoCount >= data.magazineSize)
        {
            behaviour.IsTubeReloading = false;
            yield break;
        }

        if (data.hasLimitedAmmo && gun.StoredAmmo < 1f)
        {
            behaviour.IsTubeReloading = false;
            yield break;
        }

        try
        {
            gun.Reload();
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] Continue reload: {ex.Message}");
            behaviour.IsTubeReloading = false;
        }
    }
}

/// <summary>
/// Fire during single-round reload cancels the remaining shells and lets the shot go.
/// Vanilla UpdateWantsToFire hard-blocks while Reloading; CancelReload clears that flag.
/// </summary>
[HarmonyPatch(typeof(Gun), "Update")]
internal static class BallisticSniperReloadInterruptHook
{
    [HarmonyPrefix]
    private static void Prefix(Gun __instance)
    {
        try
        {
            if (__instance == null || !__instance.IsOwner || !__instance.Active)
                return;

            if (!BallisticSniperBehaviour.TryGet(__instance, out BallisticSniperBehaviour sniper))
                return;

            if (!sniper.SingleRoundReload)
                return;

            if (!__instance.Reloading && !sniper.IsTubeReloading)
                return;

            // Need at least one chambered round to interrupt into a shot.
            if (__instance.RemainingAmmoCount < 1)
                return;

            if (!IsFirePressedOrHeld())
                return;

            sniper.InterruptTubeReload(__instance);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] Reload interrupt: {ex.Message}");
        }
    }

    private static bool IsFirePressedOrHeld()
    {
        try
        {
            // PlayerActions is a struct — cannot use ?. on it.
            if (PlayerInput.Controls == null)
                return false;
            var fire = PlayerInput.Controls.Player.Fire;
            return fire.IsPressed() || fire.WasPressedThisFrame();
        }
        catch
        {
            return false;
        }
    }

}

/// <summary>
/// Scales reload animation speed so each single shell takes (totalReload / magSize).
/// </summary>
[HarmonyPatch(typeof(Gun), "GetReloadDuration")]
internal static class BallisticSniperReloadDurationHook
{
    [HarmonyPostfix]
    private static void Postfix(Gun __instance, ref float __result)
    {
        try
        {
            if (__instance == null)
                return;

            if (!BallisticSniperBehaviour.TryGet(__instance, out BallisticSniperBehaviour behaviour))
                return;

            if (!behaviour.SingleRoundReload)
                return;

            int mag = Mathf.Max(__instance.GunData.magazineSize, 1);
            __result = Mathf.Max(__result / mag, 0.12f);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] GetReloadDuration: {ex.Message}");
        }
    }
}

/// <summary>
/// Vanilla GetSpread never reduces cone while ADS (only recoil has aim multipliers).
/// Sniper hip-fire stays wide; standing ADS is near-perfect; moving ADS is penalized.
/// </summary>
[HarmonyPatch(typeof(Gun), nameof(Gun.GetSpread))]
internal static class BallisticSniperSpreadHook
{
    /// <summary>Standing ADS cone (degrees-ish, same units as spreadSize).</summary>
    private const float AdsSpread = 0.04f;

    /// <summary>ADS while moving — still usable but clearly worse than planted.</summary>
    private const float AdsMovingSpread = 1.15f;

    [HarmonyPostfix]
    private static void Postfix(Gun __instance, ref Vector2 __result)
    {
        try
        {
            if (__instance == null)
                return;

            if (!BallisticSniperBehaviour.TryGet(__instance, out BallisticSniperBehaviour sniper))
                return;

            // Deadbolt: perfect accuracy after kill
            if (sniper.IsDeadboltActive())
            {
                __result = Vector2.zero;
                return;
            }

            bool aiming = __instance.IsAiming;

            // Auto Trigger: worse ADS bloom
            if (sniper.WeaponData.autoTrigger && aiming)
            {
                float autoSize = sniper.WeaponData.autoTriggerAdsSpread;
                float angleA = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float radiusA = UnityEngine.Random.Range(0f, autoSize);
                __result = new Vector2(Mathf.Cos(angleA) * radiusA, Mathf.Sin(angleA) * radiusA);
                return;
            }

            // Hip-fire: keep catalog spreadSize (intentionally terrible).
            if (!aiming)
                return;

            float size = IsMovingWhileScoped(__instance) ? AdsMovingSpread : AdsSpread;

            if (size <= 0.001f)
            {
                __result = Vector2.zero;
                return;
            }

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float radius = UnityEngine.Random.Range(0f, size);
            __result = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogDebug($"[BallisticSniper] GetSpread: {ex.Message}");
        }
    }


    private static bool IsMovingWhileScoped(Gun gun)
    {
        try
        {
            var player = gun.Player;
            if (player == null)
                return false;

            if (player.IsSprinting || player.Sliding)
                return true;

            // Horizontal speed when Velocity is available on this build.
            try
            {
                Vector3 v = player.Velocity;
                v.y = 0f;
                if (v.sqrMagnitude > 0.35f * 0.35f)
                    return true;
            }
            catch
            {
                // no Velocity — still allow tight ADS when not sprinting/sliding
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }
}



