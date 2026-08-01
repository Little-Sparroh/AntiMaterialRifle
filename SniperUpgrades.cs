using BepInEx.Logging;

/// <summary>
/// Registers all Anti-Material Rifle upgrades (ids 87422–87449).

/// </summary>
public static class SniperUpgrades
{
    public const int IdHeavyGrain = 87422;
    public const int IdReserveLoad = 87423;
    public const int IdHullbreaker = 87424;
    public const int IdSubsonic = 87425;
    public const int IdRicochetProtocol = 87426;
    public const int IdOverpressure = 87427;
    public const int IdTwinLink = 87428;
    public const int IdMarkOfExhaustion = 87429;
    public const int IdLongwatch = 87430;
    public const int IdScouter = 87431;
    public const int IdPerforator = 87432;
    public const int IdSpotter = 87433;
    public const int IdDeadbolt = 87434;
    public const int IdDeathMark = 87435;
    public const int IdAutoTrigger = 87436;
    public const int IdHighExplosive = 87437;
    public const int IdClipped = 87438;
    public const int IdAnchor = 87439;
    public const int IdOneInTheChamber = 87440;
    public const int IdDisruptChannel = 87441;
    public const int IdMycoSplash = 87442;
    public const int IdWetRounds = 87443;
    public const int IdReposition = 87444;
    public const int IdPoweredEcho = 87445;
    public const int IdTransferRelay = 87446;
    public const int IdBoundaryIncursion = 87447;
    public const int IdSynchronize = 87448;
    public const int IdOverkill = 87449;

    private const Upgrade.UpgradeFlags FlagStack = Upgrade.UpgradeFlags.CanStack;
    private const Upgrade.UpgradeFlags FlagMissionStack = (Upgrade.UpgradeFlags)16384u;
    private const Upgrade.UpgradeFlags FlagNone = Upgrade.UpgradeFlags.None;
    /// <summary>Vanilla GridGrow flags: IsSpatial | CanStackInMission.</summary>
    private const Upgrade.UpgradeFlags FlagSpatialMissionStack =
        Upgrade.UpgradeFlags.IsSpatial | FlagMissionStack;


    private static bool _registered;

    public static void RegisterAll(ManualLogSource log = null)
    {
        if (_registered)
            return;

        if (PlayerData.Instance == null)
        {
            log?.LogDebug("[SniperUpgrades] PlayerData.Instance null — deferring.");
            return;
        }

        IUpgradable gear = SparrohPlugin.ResolveRegisteredGear();
        if (gear == null)
        {
            log?.LogDebug("[SniperUpgrades] Gear not ready — deferring.");
            return;
        }

        // Ensure collectedGear has our entry before CreateUpgrade → RegisterUpgrade → GetGearData.
        WeaponRegistration.EnsureGearData(gear, autoUnlock: true, log);
        if (PlayerData.GetGearData(gear) == null && PlayerData.GetGearData(gear.Info.ID) == null)
        {
            log?.LogDebug("[SniperUpgrades] GearData missing — deferring.");
            return;
        }

        int ok = 0;
        const int total = 28;



        if (Reg(gear, IdHeavyGrain, "Heavy Grain",
                "Dense projectiles. Large damage increase.",
                Rarity.Standard, FlagStack, 0, SniperUpgradePatterns.Small(),
                new UpgradeProperty[] { new SniperProp_HeavyGrain() }, log)) ok++;

        if (Reg(gear, IdReserveLoad, "Reserve Load",
                "Expanded reserve capacity.",
                Rarity.Standard, FlagStack, 0, SniperUpgradePatterns.Small(),
                new UpgradeProperty[] { new SniperProp_ReserveLoad() }, log)) ok++;

        if (Reg(gear, IdHullbreaker, "Hullbreaker",
                "Massively increased damage to enemy shells and armor plating.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Medium(),
                new UpgradeProperty[] { new SniperProp_Hullbreaker() }, log)) ok++;

        if (Reg(gear, IdSubsonic, "Subsonic",
                "Devastating damage against full-health targets.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Medium(),
                new UpgradeProperty[] { new SniperProp_Subsonic() }, log)) ok++;

        if (Reg(gear, IdRicochetProtocol, "Ricochet Protocol",
                "Extra bounces. After bouncing, rounds home aggressively.",
                Rarity.Epic, FlagMissionStack, 0, SniperUpgradePatterns.Large(),
                new UpgradeProperty[] { new SniperProp_RicochetProtocol() }, log)) ok++;

        if (Reg(gear, IdOverpressure, "Overpressure",
                "Shots charge before firing. Huge damage, slower move while charging, smaller mag.",
                Rarity.Epic, FlagNone, 1, SniperUpgradePatterns.Large(),
                new UpgradeProperty[] { new SniperProp_Overpressure() }, log)) ok++;

        if (Reg(gear, IdTwinLink, "Twin Link",
                "Fires an extra projectile. Hitting two different targets fences them with shock damage.",
                Rarity.Epic, FlagMissionStack, 0, SniperUpgradePatterns.Wide(),
                new UpgradeProperty[] { new SniperProp_TwinLink() }, log)) ok++;

        if (Reg(gear, IdMarkOfExhaustion, "Mark of Exhaustion",
                "Periodically, the next shot slows the target and cripples tracking.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Medium(),
                new UpgradeProperty[] { new SniperProp_Exhaustion() }, log)) ok++;

        if (Reg(gear, IdLongwatch, "Longwatch",
                "After 3s continuous ADS: more range, less drop, faster bullets.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Line(),
                new UpgradeProperty[] { new SniperProp_Longwatch() }, log)) ok++;

        if (Reg(gear, IdScouter, "Scouter",
                "While aiming, pulse-highlight threats every few seconds.",
                Rarity.Standard, FlagMissionStack, 0, SniperUpgradePatterns.Small(),
                new UpgradeProperty[] { new SniperProp_Scouter() }, log)) ok++;

        if (Reg(gear, IdPerforator, "Perforator",
                "Pierce up to 5 targets. Damage falls off per pierce.",
                Rarity.Epic, FlagMissionStack, 0, SniperUpgradePatterns.Large(),
                new UpgradeProperty[] { new SniperProp_Perforator() }, log)) ok++;

        if (Reg(gear, IdSpotter, "Spotter",
                "Hits mark the target and nearby enemies for allies.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Medium(),
                new UpgradeProperty[] { new SniperProp_Spotter() }, log)) ok++;

        if (Reg(gear, IdDeadbolt, "Deadbolt",
                "After a kill, perfect accuracy for several seconds (hip and ADS).",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Medium(),
                new UpgradeProperty[] { new SniperProp_Deadbolt() }, log)) ok++;

        if (Reg(gear, IdDeathMark, "Death Mark",
                "Hits apply a delayed explosive mark. Stacks cook harder. Headshots shorten the fuse.",
                Rarity.Epic, FlagMissionStack, 1, SniperUpgradePatterns.Exotic(),
                new UpgradeProperty[] { new SniperProp_DeathMark() }, log)) ok++;

        if (Reg(gear, IdAutoTrigger, "Auto Trigger",
                "Full-auto precision fire. Higher mag, lower damage, worse ADS bloom.",
                Rarity.Epic, FlagNone, 1, SniperUpgradePatterns.Wide(),
                new UpgradeProperty[] { new SniperProp_AutoTrigger() }, log)) ok++;

        if (Reg(gear, IdHighExplosive, "High Explosive",
                "Hold reload to throw C4. Hold reload again to detonate a massive blast.",
                Rarity.Exotic, FlagNone, 2, SniperUpgradePatterns.Exotic(),
                new UpgradeProperty[] { new SniperProp_HighExplosive() }, log)) ok++;

        if (Reg(gear, IdClipped, "Clipped",
                "Dramatic reload speed. Uses full magazine reload instead of single-round.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Medium(),
                new UpgradeProperty[] { new SniperProp_Clipped() }, log)) ok++;

        if (Reg(gear, IdAnchor, "Anchor",
                "While completely stationary, recoil is fully negated.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Small(),
                new UpgradeProperty[] { new SniperProp_Anchor() }, log)) ok++;

        if (Reg(gear, IdOneInTheChamber, "One in the Chamber",
                "After a full top-off reload, fire one extra round from empty before reloading.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Small(),
                new UpgradeProperty[] { new SniperProp_OneInChamber() }, log)) ok++;

        if (Reg(gear, IdDisruptChannel, "Disrupt Channel",
                "Hitting a shield blasts it with massive ignore-immunity damage.",
                Rarity.Epic, FlagMissionStack, 0, SniperUpgradePatterns.Large(),
                new UpgradeProperty[] { new SniperProp_Disrupt() }, log)) ok++;

        if (Reg(gear, IdMycoSplash, "Myco Splash",
                "Applies heavy Rot on hit and splashes Rot to nearby targets.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Medium(),
                new UpgradeProperty[] { new SniperProp_MycoSplash() }, log)) ok++;

        if (Reg(gear, IdWetRounds, "Wet Rounds",
                "Applies heavy Wet (Water) buildup on hit.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Small(),
                new UpgradeProperty[] { new SniperProp_WetRounds() }, log)) ok++;

        if (Reg(gear, IdReposition, "Reposition",
                "Faster movement. Strongly reduced move speed while aiming.",
                Rarity.Standard, FlagMissionStack, 0, SniperUpgradePatterns.Small(),
                new UpgradeProperty[] { new SniperProp_Reposition() }, log)) ok++;

        if (Reg(gear, IdPoweredEcho, "Powered Echo",
                "A second projectile echoes along the same path shortly after, applying Shock.",
                Rarity.Epic, FlagMissionStack, 0, SniperUpgradePatterns.Large(),
                new UpgradeProperty[] { new SniperProp_PoweredEcho() }, log)) ok++;

        if (Reg(gear, IdTransferRelay, "Transfer Relay",
                "When damage is negated (immunity), a large ignore-immunity pulse still lands.",
                Rarity.Exotic, FlagNone, 2, SniperUpgradePatterns.Exotic(),
                new UpgradeProperty[] { new SniperProp_TransferRelay() }, log)) ok++;

        if (Reg(gear, IdBoundaryIncursion, "Boundary Incursion",
                "Adds a row or column to the upgrade grid.",
                Rarity.Oddity, FlagSpatialMissionStack, -100, SniperUpgradePatterns.BoundaryIncursion(),
                new UpgradeProperty[] { new UpgradeProperty_GrowGrid() }, log)) ok++;

        if (Reg(gear, IdSynchronize, "Synchronize",
                "If an ally damaged the same target within 3 seconds, deal massive bonus damage.",
                Rarity.Epic, FlagMissionStack, 0, SniperUpgradePatterns.Large(),
                new UpgradeProperty[] { new SniperProp_Synchronize() }, log)) ok++;

        if (Reg(gear, IdOverkill, "Overkill",
                "When damage destroys a shell, leftover damage carries over to the layer underneath.",
                Rarity.Rare, FlagMissionStack, 0, SniperUpgradePatterns.Medium(),
                new UpgradeProperty[] { new SniperProp_Overkill() }, log)) ok++;

        if (ok == total)

        {
            _registered = true;
            log?.LogInfo($"[SniperUpgrades] Registered {ok}/{total} upgrades.");
        }
        else
        {
            log?.LogWarning($"[SniperUpgrades] Registered {ok}/{total} upgrades (partial).");
            if (ok > 0)
                _registered = true;
        }
    }

    private static bool Reg(
        IUpgradable gear,
        int id,
        string name,
        string description,
        Rarity rarity,
        Upgrade.UpgradeFlags flags,
        int priority,
        HexMap pattern,
        UpgradeProperty[] properties,
        ManualLogSource log)
    {
        if (!UpgradeRegistration.TryCreateGunUpgrade(
                modGuid: SparrohPlugin.PluginGUID,
                gear: gear,
                gearApiName: SparrohPlugin.GearApiName,
                upgradeId: id,
                name: name,
                description: description,
                rarity: rarity,
                properties: properties,
                pattern: pattern,
                flags: flags,
                icon: null,
                log: log,
                out Upgrade upgrade,
                priority: priority))
        {
            log?.LogWarning($"[SniperUpgrades] Failed '{name}' (id={id}).");
            return false;
        }

        log?.LogInfo($"[SniperUpgrades] + {name} (id={id}, {rarity}).");
        return upgrade != null;
    }
}
