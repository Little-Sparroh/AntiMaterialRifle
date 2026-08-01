using System;
using BepInEx.Logging;
using UnityEngine;

/// <summary>
/// Helpers for creating custom upgrades via PlayerData.CreateUpgrade.
/// </summary>
public static class UpgradeRegistration
{
    public static bool TryCreateGunUpgrade(
        string modGuid,
        IUpgradable gear,
        string gearApiName,
        int upgradeId,
        string name,
        string description,
        Rarity rarity,
        UpgradeProperty[] properties,
        HexMap pattern,
        Upgrade.UpgradeFlags flags,
        Sprite icon,
        ManualLogSource log,
        out Upgrade upgrade,
        int priority = 0,
        Upgrade.CollectionSource collectionSource = Upgrade.CollectionSource.WorldPool)
    {
        upgrade = null;

        if (string.IsNullOrEmpty(modGuid))
        {
            log?.LogError("[UpgradeRegistration] modGuid is null/empty.");
            return false;
        }

        if (properties == null || properties.Length == 0)
        {
            log?.LogError("[UpgradeRegistration] properties array is null/empty.");
            return false;
        }

        if (gear == null)
            gear = WeaponRegistration.FindGearSafe(gearApiName);

        if (gear == null)
        {
            log?.LogError(
                $"[UpgradeRegistration] Gear \"{gearApiName}\" not found. Register gear first.");
            return false;
        }

        PlayerData.CustomUpgradeParams upgradeParams = PlayerData.CustomUpgradeParams.Create(
            gear,
            upgradeId,
            name,
            description,
            rarity,
            icon);

        upgradeParams.flags = flags;
        upgradeParams.priority = priority;
        upgradeParams.collectionSource = collectionSource;
        upgradeParams.upgradeType = Upgrade.Type.Normal;
        upgradeParams.useDefaultUnlockCost = true;

        if (pattern != null)
            upgradeParams.pattern = pattern;

        try
        {
            upgrade = PlayerData.CreateUpgrade(modGuid, upgradeParams, properties);
        }
        catch (Exception ex)
        {
            log?.LogError($"[UpgradeRegistration] CreateUpgrade threw: {ex}");
            upgrade = null;
            return false;
        }

        if (upgrade == null)
        {
            log?.LogError("[UpgradeRegistration] CreateUpgrade returned null.");
            return false;
        }

        if (pattern != null)
            upgrade.SetPattern(pattern);

        return true;
    }

    /// <summary>Small diagonal footprint for stackable damage modules.</summary>
    public static HexMap CreateSamplePattern()
    {
        HexMap map = new HexMap(3, 3);
        map[0, 0].enabled = true;
        map[1, 0].enabled = true;
        map[1, 0].connections = HexMap.Direction.SouthEast | HexMap.Direction.NorthWest;
        map[2, 1].enabled = true;
        return map;
    }

    public static UpgradeInstance GrantTestInstance(
        IUpgradable gear,
        Upgrade upgrade,
        bool unlock = true,
        bool quietUnlock = true,
        ManualLogSource log = null)
    {
        if (gear == null || upgrade == null)
        {
            log?.LogError("[UpgradeRegistration] GrantTestInstance: gear or upgrade is null.");
            return null;
        }

        UpgradeInstance instance = PlayerData.CollectInstance(gear, upgrade, PlayerData.UnlockFlags.Hidden);
        if (instance == null)
        {
            log?.LogError("[UpgradeRegistration] CollectInstance returned null.");
            return null;
        }

        if (unlock)
            instance.Unlock(quietUnlock);

        log?.LogInfo($"[UpgradeRegistration] Granted test instance of '{upgrade.Name}'.");
        return instance;
    }
}
