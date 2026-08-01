using System;
using System.Collections.Generic;
using Pigeon.Math;

/// <summary>
/// Starter sniper upgrade: multiplies GunData.damage.
/// Also ensures GearInfo has a non-skin upgrade so GearDetailsWindow shows
/// the hex grid + inventory (gated on PlayerData.HasUpgrades).
/// </summary>
[Serializable]
public class MatchGradeRoundsProperty : UpgradeProperty
{
    /// <summary>Extra damage fraction. 0.20 → +20%.</summary>
    public global::Range<float> damageBonus = new global::Range<float>(0.12f, 0.22f);

    public override IEnumerator<StatData> GetStatData(
        Pigeon.Math.Random rand,
        IUpgradable gear,
        UpgradeInstance upgrade)
    {
        yield return StatData.Create(
            "Damage:",
            damageBonus,
            upgrade,
            ref rand,
            OverrideType.Multiply,
            StatData.LabelType.BeforeWithColon,
            default(BoostParams));
    }

    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (gear is not IWeapon weapon)
            return;

        float bonus = damageBonus.GetValue(ref rand, upgrade, default(BoostParams));
        weapon.GunData.damage *= (1f + bonus);
    }

    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (gear is IWeapon weapon && prefab is IWeapon prefabWeapon)
            weapon.GunData.damage = prefabWeapon.GunData.damage;
    }
}
