using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


/// <summary>
/// Ballistic Sniper (Anti-Material Rifle) for Mycopunk.
///
/// Registers a primary weapon by cloning CartridgeSMG at runtime, rewriting GunData
/// into a slow high-damage bolt-action profile, and enabling single-round reload.
/// </summary>
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsSandbox)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.antimaterialrifle";
    public const string PluginName = "AntiMaterialRifle";
    public const string PluginVersion = "1.2.0";



    /// <summary>
    /// Stable numeric GearInfo.ID. High unique range to avoid vanilla / other mods.
    /// </summary>
    public const int GearId = 87421;

    /// <summary>Value of GearInfo.APIName — used by FindGear / AllGear scans.</summary>
    public const string GearApiName = "ballistic_sniper";

    public const string GearDisplayName = "Anti-Material Rifle";
    public const string GearDescription =
        "Heavy kinetic bolt-action rifle. High single-shot damage, low capacity, " +
        "and a deliberate single-round reload. Built for long-range elimination.";

    /// <summary>Vanilla gun type to clone for model / NGO spawn validity.</summary>
    public const string BaseTypeName = "CartridgeSMG";

    internal static new ManualLogSource Logger;
    internal static SparrohPlugin Instance;

    /// <summary>
    /// When true, grants one unlocked inventory instance of each sniper upgrade on load.
    /// Does not auto-equip; only tops up ownership to 1 (idempotent).
    /// </summary>
    internal static ConfigEntry<bool> GrantAllUpgrades;

    /// <summary>Registered prefab / gear instance (null until registration succeeds).</summary>
    public static IUpgradable CustomWeaponPrefab;

    private Harmony _harmony;
    private bool _gearRegistered;


    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        GrantAllUpgrades = Config.Bind(
            "Debug",
            "GrantAllUpgrades",
            true,
            "Grant one unlocked inventory instance of each Anti-Material Rifle upgrade on load. " +
            "Idempotent (tops up to 1). Disable before shipping if players should earn drops normally.");

        _harmony = new Harmony(PluginGUID);


        // Core boot patches first — must not throw.
        _harmony.PatchAll(typeof(GlobalLoadHook));
        _harmony.PatchAll(typeof(PlayerDataPersistenceHooks));
        _harmony.PatchAll(typeof(BallisticSniperReloadHook));
        _harmony.PatchAll(typeof(BallisticSniperReloadInterruptHook));
        _harmony.PatchAll(typeof(BallisticSniperReloadDurationHook));
        _harmony.PatchAll(typeof(BallisticSniperSpreadHook));
        _harmony.PatchAll(typeof(GearSelectionWindowHooks));


        // Register upgrade callback BEFORE optional combat patches.
        // Vanilla fires OnRegisterUpgrades during PlayerData.OnAwake AFTER AddGear.
        PlayerData.AddRegisterUpgradesCallback(RegisterUpgrades);

        // Optional combat patches — each isolated so a missing method can't kill Awake.
        BallisticSniperCombatHooks.Apply(_harmony);
        SpawnGearHooks.Apply(_harmony);


        TryRegisterGear("Awake");
        // Only attempt upgrades if PlayerData is already up (hot reload / late load).
        // Normal boot: AddRegisterUpgradesCallback + OnAwake postfix handle it.
        if (PlayerData.Instance != null)
            RegisterUpgrades();

        Logger.LogInfo($"{PluginName} v{PluginVersion} loaded.");

    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
        Instance = null;
    }

    internal void TryRegisterGear(string reason)
    {
        if (_gearRegistered)
            return;

        if (Global.Instance == null || Global.Instance.AllGear == null || Global.Instance.AllGear.Length == 0)
        {
            Logger.LogDebug($"[BallisticSniper] Global.AllGear not ready yet ({reason}).");
            return;
        }

        try
        {
            if (!WeaponRegistration.TryCreateAndRegister(
                    modGuid: PluginGUID,
                    gearId: GearId,
                    apiName: GearApiName,
                    displayName: GearDisplayName,
                    description: GearDescription,
                    baseTypeName: BaseTypeName,
                    autoUnlock: true,
                    log: Logger,
                    out CustomWeaponPrefab))
            {
                return;
            }

            _gearRegistered = true;
            Logger.LogInfo(
                $"[BallisticSniper] Registered gear '{GearDisplayName}' " +
                $"(api={GearApiName}, id={GearId}) via {reason}.");

            // Only register upgrades once PlayerData can bind GearData.
            if (PlayerData.Instance != null)
                RegisterUpgrades();

        }
        catch (Exception ex)
        {
            Logger.LogError($"[BallisticSniper] Gear registration failed: {ex}");
        }
    }

    /// <summary>
    /// Registers the full sniper upgrade pool (also unlocks hex grid UI via HasUpgrades).
    /// Requires PlayerData.Instance + GearData so CreateUpgrade/RegisterUpgrade does not NRE.
    /// </summary>
    internal void RegisterUpgrades()
    {
        try
        {
            if (PlayerData.Instance == null)
            {
                Logger.LogDebug("[BallisticSniper] Deferring upgrades — PlayerData.Instance null.");
                return;
            }

            IUpgradable gear = ResolveRegisteredGear();
            if (gear == null)
            {
                Logger.LogDebug("[BallisticSniper] Deferring upgrades until gear is registered.");
                return;
            }

            // CreateUpgrade → RegisterUpgrade → GetGearData; must have a bound entry.
            WeaponRegistration.EnsureGearData(gear, autoUnlock: true, Logger);
            if (PlayerData.GetGearData(gear) == null && PlayerData.GetGearData(gear.Info.ID) == null)
            {
                Logger.LogDebug("[BallisticSniper] Deferring upgrades — GearData not bound yet.");
                return;
            }

            CustomWeaponPrefab = gear;
            SniperUpgrades.RegisterAll(Logger);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[BallisticSniper] Upgrade registration failed: {ex}");
        }
    }



    /// <summary>
    /// Resolve our gear without calling vanilla FindGear first (it can NRE early in boot).
    /// </summary>
    internal static IUpgradable ResolveRegisteredGear()
    {
        if (CustomWeaponPrefab != null)
            return CustomWeaponPrefab;

        if (WeaponRegistration.CatalogGear != null)
            return WeaponRegistration.CatalogGear;

        return WeaponRegistration.FindGearSafe(GearApiName, GearId);
    }
}

/// <summary>
/// Registers custom gear immediately after vanilla Global resources initialize.
/// </summary>
[HarmonyPatch(typeof(Global), nameof(Global.LoadInstance))]
internal static class GlobalLoadHook
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        SparrohPlugin.Instance?.TryRegisterGear("Global.LoadInstance");
    }
}

/// <summary>
/// Keep Anti-Material Rifle alive across save load.
///
/// PlayerData.OnAwake order:
///   1. LoadInstance() — deserialize collectedGear / weapon1ID / levels
///   2. AddGear(AllGear…) — bind Gear refs by ID
///   3. OnRegisterUpgrades — CreateUpgrade for mods
///   4. Purge collectedGear entries whose Gear is still null
///
/// Prefix: inject gear into AllGear before AddGear so save entries rebind.
/// Postfix: EnsureGearData re-binds Gear ref and preserves unlock/level.
/// </summary>
[HarmonyPatch(typeof(PlayerData), nameof(PlayerData.OnAwake))]
internal static class PlayerDataPersistenceHooks
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        SparrohPlugin.Instance?.TryRegisterGear("PlayerData.OnAwake.Prefix");
    }

    [HarmonyPostfix]
    private static void Postfix()
    {
        try
        {
            SparrohPlugin.Instance?.TryRegisterGear("PlayerData.OnAwake.Postfix");
            // Callback already ran inside OnAwake; re-run is no-op if already registered.
            SparrohPlugin.Instance?.RegisterUpgrades();

            IUpgradable gear = SparrohPlugin.ResolveRegisteredGear();
            if (gear == null)
            {
                SparrohPlugin.Logger?.LogWarning("[BallisticSniper] Persistence: gear missing after OnAwake.");
                return;
            }

            PlayerData.GearData gd = PlayerData.GetGearData(gear);
            if (gd == null)
            {
                WeaponRegistration.EnsureGearData(gear, autoUnlock: true, SparrohPlugin.Logger);
                gd = PlayerData.GetGearData(gear);
            }
            else
            {
                gd.Gear = gear;
            }

            if (gd != null)
            {
                if (!gd.IsUnlocked)
                    gd.Unlock();
                SparrohPlugin.Logger?.LogInfo(
                    $"[BallisticSniper] Persistence OK: level={gd.Level} unlocked={gd.IsUnlocked} " +
                    $"equipped={gd.EquippedUpgradeCount} xp={gd.LevelXP} " +
                    $"HasUpgrades={PlayerData.HasUpgrades(gear)} HasGrid={gear.Info?.HasUpgradeGrid}.");
            }

            // Top up inventory instances after save rebind (idempotent).
            SniperUpgrades.GrantAllInstances(SparrohPlugin.Logger);

        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger?.LogError($"[BallisticSniper] Persistence postfix failed: {ex}");
        }
    }
}
