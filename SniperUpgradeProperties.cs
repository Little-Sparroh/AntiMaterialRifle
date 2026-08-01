using System;
using System.Collections.Generic;
using Pigeon.Math;
using UnityEngine;

// ---- Simple stat properties that write BallisticSniperBehaviour.Data ----

[Serializable]
public class SniperProp_Hullbreaker : UpgradeProperty
{
    public Range<float> mult = new Range<float>(1.9f, 2.3f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Shell damage:", mult, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.shellDamageMult *= mult.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.shellDamageMult = b.GetPrefabSnapshot().shellDamageMult;
    }
}

[Serializable]
public class SniperProp_Subsonic : UpgradeProperty
{
    public Range<float> mult = new Range<float>(2.1f, 2.6f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Full-HP damage:", mult, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.fullHpDamageMult *= mult.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.fullHpDamageMult = b.GetPrefabSnapshot().fullHpDamageMult;
    }
}

[Serializable]
public class SniperProp_RicochetProtocol : UpgradeProperty
{
    public Range<int> bounces = new Range<int>(2, 3);
    public Range<float> homing = new Range<float>(8f, 12f);


    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Bounces:", bounces, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Post-bounce homing:", homing, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.bonusBounces += bounces.GetValue(ref rand, upgrade, default);
        b.WeaponData.postBounceHoming = Mathf.Max(b.WeaponData.postBounceHoming, homing.GetValue(ref rand, upgrade, default));
        if (gear is IWeapon w) w.GunData.maxBounces = Mathf.Max(w.GunData.maxBounces, b.WeaponData.bonusBounces);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.bonusBounces = s.bonusBounces;
        b.WeaponData.postBounceHoming = s.postBounceHoming;
        if (gear is IWeapon w && prefab is IWeapon p) w.GunData.maxBounces = p.GunData.maxBounces;
    }
}

[Serializable]
public class SniperProp_Overpressure : UpgradeProperty
{
    public Range<float> charge = new Range<float>(0.55f, 0.7f);
    public Range<float> dmgMult = new Range<float>(1.85f, 2.2f);
    public Range<float> moveMult = new Range<float>(0.55f, 0.65f);
    public Range<int> magPenalty = new Range<int>(2, 2);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Charge time:", charge, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Damage:", dmgMult, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Move while charging:", moveMult, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Magazine penalty:", magPenalty, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);

    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.overpressure = true;
        b.WeaponData.overpressureChargeDuration = charge.GetValue(ref rand, upgrade, default);
        b.WeaponData.overpressureDamageMult = dmgMult.GetValue(ref rand, upgrade, default);
        b.WeaponData.overpressureMoveMult = moveMult.GetValue(ref rand, upgrade, default);
        b.WeaponData.overpressureMagPenalty = magPenalty.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.overpressure = s.overpressure;
        b.WeaponData.overpressureChargeDuration = s.overpressureChargeDuration;
        b.WeaponData.overpressureDamageMult = s.overpressureDamageMult;
        b.WeaponData.overpressureMoveMult = s.overpressureMoveMult;
        b.WeaponData.overpressureMagPenalty = s.overpressureMagPenalty;
    }
}

[Serializable]
public class SniperProp_TwinLink : UpgradeProperty
{
    public Range<int> extra = new Range<int>(1, 1);
    public Range<float> fenceDmg = new Range<float>(90f, 130f);
    public Range<float> fenceR = new Range<float>(3.5f, 5f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Extra pellets:", extra, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Fence damage:", fenceDmg, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.extraBulletsPerShot += extra.GetValue(ref rand, upgrade, default);
        b.WeaponData.twinLinkFenceDamage = fenceDmg.GetValue(ref rand, upgrade, default);
        b.WeaponData.twinLinkFenceRadius = fenceR.GetValue(ref rand, upgrade, default);
        if (gear is IWeapon w) w.GunData.bulletsPerShot += extra.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.extraBulletsPerShot = s.extraBulletsPerShot;
        b.WeaponData.twinLinkFenceDamage = s.twinLinkFenceDamage;
        b.WeaponData.twinLinkFenceRadius = s.twinLinkFenceRadius;
        if (gear is IWeapon w && prefab is IWeapon p) w.GunData.bulletsPerShot = p.GunData.bulletsPerShot;
    }
}

[Serializable]
public class SniperProp_Exhaustion : UpgradeProperty
{
    public Range<float> cd = new Range<float>(3.5f, 4.5f);
    public Range<float> dur = new Range<float>(3.5f, 5f);
    public Range<float> strength = new Range<float>(12f, 18f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Mark cooldown:", cd, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Slow duration:", dur, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.exhaustionCooldown = cd.GetValue(ref rand, upgrade, default);
        b.WeaponData.exhaustionSlowDuration = dur.GetValue(ref rand, upgrade, default);
        b.WeaponData.exhaustionSlowStrength = strength.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.exhaustionCooldown = s.exhaustionCooldown;
        b.WeaponData.exhaustionSlowDuration = s.exhaustionSlowDuration;
        b.WeaponData.exhaustionSlowStrength = s.exhaustionSlowStrength;
    }
}

[Serializable]
public class SniperProp_Longwatch : UpgradeProperty
{
    public Range<float> aimSec = new Range<float>(3f, 3f);
    public Range<float> range = new Range<float>(80f, 120f);
    public Range<float> grav = new Range<float>(0.25f, 0.4f);
    public Range<float> speed = new Range<float>(1.45f, 1.7f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("ADS hold:", aimSec, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Range:", range, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Bullet speed:", speed, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.longwatchAimSeconds = aimSec.GetValue(ref rand, upgrade, default);
        b.WeaponData.longwatchRangeBonus = range.GetValue(ref rand, upgrade, default);
        b.WeaponData.longwatchGravityMult = grav.GetValue(ref rand, upgrade, default);
        b.WeaponData.longwatchSpeedMult = speed.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.longwatchAimSeconds = s.longwatchAimSeconds;
        b.WeaponData.longwatchRangeBonus = s.longwatchRangeBonus;
        b.WeaponData.longwatchGravityMult = s.longwatchGravityMult;
        b.WeaponData.longwatchSpeedMult = s.longwatchSpeedMult;
    }
}

[Serializable]
public class SniperProp_Scouter : UpgradeProperty
{
    public Range<float> interval = new Range<float>(2.5f, 3f);
    public Range<float> radius = new Range<float>(45f, 60f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Pulse interval:", interval, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Pulse range:", radius, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.scouterInterval = interval.GetValue(ref rand, upgrade, default);
        b.WeaponData.scouterRadius = radius.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.scouterInterval = s.scouterInterval;
        b.WeaponData.scouterRadius = s.scouterRadius;
    }
}

[Serializable]
public class SniperProp_Perforator : UpgradeProperty
{
    public Range<int> pierce = new Range<int>(5, 5);
    public Range<float> falloff = new Range<float>(0.22f, 0.28f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Pierce targets:", pierce, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Dmg loss/pierce:", falloff, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.pierceTargets = pierce.GetValue(ref rand, upgrade, default);
        b.WeaponData.pierceFalloff = falloff.GetValue(ref rand, upgrade, default);
        // Pierce is runtime via SimpleProjectileBullet.OnHit — no bounce mutation.
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.pierceTargets = s.pierceTargets;
        b.WeaponData.pierceFalloff = s.pierceFalloff;
    }
}


[Serializable]
public class SniperProp_Spotter : UpgradeProperty
{
    public Range<float> radius = new Range<float>(8f, 12f);
    public Range<float> duration = new Range<float>(6f, 8f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Mark radius:", radius, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Mark duration:", duration, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.spotterRadius = radius.GetValue(ref rand, upgrade, default);
        b.WeaponData.spotterDuration = duration.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.spotterRadius = s.spotterRadius;
        b.WeaponData.spotterDuration = s.spotterDuration;
    }
}

[Serializable]
public class SniperProp_Deadbolt : UpgradeProperty
{
    public Range<float> duration = new Range<float>(3f, 3.5f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Perfect accuracy:", duration, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.deadboltDuration = duration.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.deadboltDuration = b.GetPrefabSnapshot().deadboltDuration;
    }
}

[Serializable]
public class SniperProp_DeathMark : UpgradeProperty
{
    public Range<float> fuse = new Range<float>(1.6f, 2.2f);
    public Range<float> radius = new Range<float>(5f, 7f);
    public Range<float> dmgScale = new Range<float>(1.4f, 1.8f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Fuse:", fuse, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Blast radius:", radius, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Blast damage:", dmgScale, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.deathMark = true;
        b.WeaponData.deathMarkFuse = fuse.GetValue(ref rand, upgrade, default);
        b.WeaponData.deathMarkRadius = radius.GetValue(ref rand, upgrade, default);
        b.WeaponData.deathMarkDamageScale = dmgScale.GetValue(ref rand, upgrade, default);
        b.WeaponData.deathMarkHeadshotFuseMult = 0.55f;
        b.WeaponData.deathMarkStackScale = 0.4f;
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.deathMark = s.deathMark;
        b.WeaponData.deathMarkFuse = s.deathMarkFuse;
        b.WeaponData.deathMarkRadius = s.deathMarkRadius;
        b.WeaponData.deathMarkDamageScale = s.deathMarkDamageScale;
    }
}

[Serializable]
public class SniperProp_AutoTrigger : UpgradeProperty
{
    public Range<float> interval = new Range<float>(0.26f, 0.32f);
    public Range<float> dmgMult = new Range<float>(0.4f, 0.48f);
    public Range<int> mag = new Range<int>(8, 10);
    public Range<float> adsSpread = new Range<float>(0.9f, 1.2f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Fire interval:", interval, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Damage:", dmgMult, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Magazine:", mag, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.autoTrigger = true;
        b.WeaponData.autoTriggerFireInterval = interval.GetValue(ref rand, upgrade, default);
        b.WeaponData.autoTriggerDamageMult = dmgMult.GetValue(ref rand, upgrade, default);
        b.WeaponData.autoTriggerMagBonus = mag.GetValue(ref rand, upgrade, default);
        b.WeaponData.autoTriggerAdsSpread = adsSpread.GetValue(ref rand, upgrade, default);
        // Keep single-round reload — Auto Trigger only changes fire mode / mag / damage.
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.autoTrigger = s.autoTrigger;
        b.WeaponData.autoTriggerFireInterval = s.autoTriggerFireInterval;
        b.WeaponData.autoTriggerDamageMult = s.autoTriggerDamageMult;
        b.WeaponData.autoTriggerMagBonus = s.autoTriggerMagBonus;
        b.WeaponData.autoTriggerAdsSpread = s.autoTriggerAdsSpread;
    }
}


[Serializable]
public class SniperProp_HighExplosive : UpgradeProperty
{
    public Range<float> dmg = new Range<float>(240f, 320f);
    public Range<float> radius = new Range<float>(9f, 12f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("C4 damage:", dmg, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("C4 radius:", radius, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.highExplosive = true;
        b.WeaponData.c4Damage = dmg.GetValue(ref rand, upgrade, default);
        b.WeaponData.c4Radius = radius.GetValue(ref rand, upgrade, default);
        b.WeaponData.c4ThrowForce = 20f;
        b.WeaponData.c4ArmTime = 0.4f;
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.highExplosive = s.highExplosive;
        b.WeaponData.c4Damage = s.c4Damage;
        b.WeaponData.c4Radius = s.c4Radius;
    }
}

[Serializable]
public class SniperProp_Clipped : UpgradeProperty
{
    public Range<float> reloadMult = new Range<float>(0.28f, 0.38f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Reload time:", reloadMult, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.clipped = true;
        b.WeaponData.clippedReloadMult = reloadMult.GetValue(ref rand, upgrade, default);
        b.WeaponData.singleRoundReload = false;
        if (gear is IWeapon w)
        {
            w.GunData.refillAmmoOnReload = true;
            w.GunData.reloadDuration *= b.WeaponData.clippedReloadMult;
        }
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.clipped = s.clipped;
        b.WeaponData.clippedReloadMult = s.clippedReloadMult;
        b.WeaponData.singleRoundReload = s.singleRoundReload;
        if (gear is IWeapon w && prefab is IWeapon p)
        {
            w.GunData.refillAmmoOnReload = p.GunData.refillAmmoOnReload;
            w.GunData.reloadDuration = p.GunData.reloadDuration;
        }
    }
}

[Serializable]
public class SniperProp_Anchor : UpgradeProperty
{
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Stationary recoil:", new Range<float>(0f, 0f), upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.anchor = true;
        b.WeaponData.anchorSpeedThreshold = 0.2f;
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.anchor = b.GetPrefabSnapshot().anchor;
    }
}

[Serializable]
public class SniperProp_OneInChamber : UpgradeProperty
{
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Bonus round after full reload:", new Range<int>(1, 1), upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.oneInTheChamber = true;
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.oneInTheChamber = b.GetPrefabSnapshot().oneInTheChamber;
    }
}

[Serializable]
public class SniperProp_Disrupt : UpgradeProperty
{
    public Range<float> duration = new Range<float>(10f, 10f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Shield disrupt:", duration, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.disruptDuration = duration.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.disruptDuration = b.GetPrefabSnapshot().disruptDuration;
    }
}

[Serializable]
public class SniperProp_HeavyGrain : UpgradeProperty
{
    public Range<float> mult = new Range<float>(1.4f, 1.6f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Damage:", mult, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        float m = mult.GetValue(ref rand, upgrade, default);
        b.WeaponData.heavyGrainDamageMult *= m;
        if (gear is IWeapon w) w.GunData.damage *= m;
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.heavyGrainDamageMult = b.GetPrefabSnapshot().heavyGrainDamageMult;
        if (gear is IWeapon w && prefab is IWeapon p) w.GunData.damage = p.GunData.damage;
    }
}

[Serializable]
public class SniperProp_ReserveLoad : UpgradeProperty
{
    public Range<float> mult = new Range<float>(1.5f, 1.75f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Reserve ammo:", mult, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        float m = mult.GetValue(ref rand, upgrade, default);
        b.WeaponData.reserveAmmoMult *= m;
        if (gear is IWeapon w) w.GunData.ammoCapacity = Mathf.Max(1, Mathf.RoundToInt(w.GunData.ammoCapacity * m));
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.reserveAmmoMult = b.GetPrefabSnapshot().reserveAmmoMult;
        if (gear is IWeapon w && prefab is IWeapon p) w.GunData.ammoCapacity = p.GunData.ammoCapacity;
    }
}

[Serializable]
public class SniperProp_MycoSplash : UpgradeProperty
{
    public Range<float> rot = new Range<float>(10f, 14f);
    public Range<float> radius = new Range<float>(4f, 5.5f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Rot:", rot, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Splash radius:", radius, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.rotAmount = rot.GetValue(ref rand, upgrade, default);
        b.WeaponData.rotSplashRadius = radius.GetValue(ref rand, upgrade, default);
        if (gear is IWeapon w)
        {
            w.GunData.damageEffect = EffectType.Rot;
            w.GunData.damageEffectAmount = Mathf.Max(w.GunData.damageEffectAmount, b.WeaponData.rotAmount);
        }
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.rotAmount = s.rotAmount;
        b.WeaponData.rotSplashRadius = s.rotSplashRadius;
        if (gear is IWeapon w && prefab is IWeapon p)
        {
            w.GunData.damageEffect = p.GunData.damageEffect;
            w.GunData.damageEffectAmount = p.GunData.damageEffectAmount;
        }
    }
}

[Serializable]
public class SniperProp_WetRounds : UpgradeProperty
{
    public Range<float> water = new Range<float>(10f, 14f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Wet:", water, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.waterAmount = water.GetValue(ref rand, upgrade, default);
        if (gear is IWeapon w)
        {
            w.GunData.damageEffect = EffectType.Water;
            w.GunData.damageEffectAmount = Mathf.Max(w.GunData.damageEffectAmount, b.WeaponData.waterAmount);
        }
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.waterAmount = b.GetPrefabSnapshot().waterAmount;
        if (gear is IWeapon w && prefab is IWeapon p)
        {
            w.GunData.damageEffect = p.GunData.damageEffect;
            w.GunData.damageEffectAmount = p.GunData.damageEffectAmount;
        }
    }
}

[Serializable]
public class SniperProp_Reposition : UpgradeProperty
{
    public Range<float> moveBonus = new Range<float>(0.28f, 0.38f);
    public Range<float> adsMult = new Range<float>(0.5f, 0.6f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Move speed:", moveBonus, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("ADS move:", adsMult, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.repositionMoveBonus = moveBonus.GetValue(ref rand, upgrade, default);
        b.WeaponData.repositionAdsMoveMult = adsMult.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.repositionMoveBonus = s.repositionMoveBonus;
        b.WeaponData.repositionAdsMoveMult = s.repositionAdsMoveMult;
    }
}

[Serializable]
public class SniperProp_PoweredEcho : UpgradeProperty
{
    public Range<float> delay = new Range<float>(0.18f, 0.28f);
    public Range<float> dmgScale = new Range<float>(0.75f, 0.95f);
    public Range<float> shock = new Range<float>(8f, 12f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Echo delay:", delay, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Echo damage:", dmgScale, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Electrocute:", shock, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.echoDelay = delay.GetValue(ref rand, upgrade, default);
        b.WeaponData.echoDamageScale = dmgScale.GetValue(ref rand, upgrade, default);
        b.WeaponData.echoShockAmount = shock.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.echoDelay = s.echoDelay;
        b.WeaponData.echoDamageScale = s.echoDamageScale;
        b.WeaponData.echoShockAmount = s.echoShockAmount;
    }
}

[Serializable]
public class SniperProp_TransferRelay : UpgradeProperty
{
    public Range<float> scale = new Range<float>(0.85f, 1.15f);
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Transferred damage:", scale, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }
    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.transferRelayScale = scale.GetValue(ref rand, upgrade, default);
    }
    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.transferRelayScale = b.GetPrefabSnapshot().transferRelayScale;
    }
}

[Serializable]
public class SniperProp_Synchronize : UpgradeProperty
{
    public Range<float> window = new Range<float>(3f, 3f);
    public Range<float> mult = new Range<float>(2.6f, 3.2f);

    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Ally window:", window, upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
        yield return StatData.Create("Sync damage:", mult, upgrade, ref rand, OverrideType.Multiply, StatData.LabelType.BeforeWithColon, default);
    }

    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.syncWindow = window.GetValue(ref rand, upgrade, default);
        b.WeaponData.syncDamageMult = mult.GetValue(ref rand, upgrade, default);
    }

    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        var s = b.GetPrefabSnapshot();
        b.WeaponData.syncWindow = s.syncWindow;
        b.WeaponData.syncDamageMult = s.syncDamageMult;
    }
}

[Serializable]
public class SniperProp_Overkill : UpgradeProperty
{
    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        yield return StatData.Create("Shell overflow carry:", new Range<float>(1f, 1f), upgrade, ref rand, OverrideType.Add, StatData.LabelType.BeforeWithColon, default);
    }

    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.overkill = true;
    }

    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (!BallisticSniperBehaviour.TryGet(gear, out var b)) return;
        b.WeaponData.overkill = b.GetPrefabSnapshot().overkill;
    }
}

