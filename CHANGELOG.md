# Changelog

## 1.2.0

- Reload interrupt: fire during single-round reload cancels remaining shells and shoots
- Switched from RailBullet (hitscan) to standard SimpleProjectileBullet so speed/gravity apply
- Perforator + Ricochet Protocol rewritten for projectile bullets
- New upgrades: Boundary Incursion (gridgrow), Synchronize, Overkill (28 total)
- Longwatch now meaningfully affects flight path after projectile swap

## 1.1.0


- Full upgrade pool (25 modules) from upgrade notes — overtuned numbers
- Paths: Death Mark, Auto Trigger, High Explosive C4, Longwatch, Perforator, Twin Link, etc.
- Combat hooks: damage, fire, recoil, move speed, reload C4, chamber bonus, spread (Deadbolt / Auto Trigger)
- Behaviour Data host mirrors CyclerRework pattern
- QA fixes: Hullbreaker, Reposition/Overpressure move, Spotter/Scouter Highlighter, Powered Echo bullet, HE cube, Auto Trigger keeps tube reload
- Round 2: Perforator via RailBullet.PierceTargets + falloff; Ricochet post-bounce retarget; Twin Link instant Shock; One in the Chamber clears block on fire; Scouter centered on aim point



## 1.0.0


- Anti-Material Rifle (`ballistic_sniper`, gear id `87421`)
- Runtime clone of CartridgeSMG with full ballistic sniper GunData rewrite
- Base stats: 145 damage, 48 RPM, mag 5 / reserve 20, 3.1s reload, projectile travel + drop
- Single-round reload (one shell at a time; full empty reload ≈ total reload duration)
- ADS spread fix (standing ADS near-perfect; hip stays wide)
- Persistence: register before PlayerData.AddGear; re-bind GearData after save load (levels / equip / unlock)
- Display name via TextBlocks; upgrade grid copied from SMG
- Starter upgrade **Match-Grade Rounds** (enables hex grid + inventory UI via HasUpgrades)
- Auto-unlock into gear select
- Equip remap + identity stamp for NGO-safe spawning


