# Ballistic Sniper

BepInEx mod for **Mycopunk** that adds the **Anti-Material Rifle** — a dedicated long-range kinetic sniper.

## Weapon

| | |
|---|---|
| Name | Anti-Material Rifle |
| API | `ballistic_sniper` |
| Gear ID | `87421` |
| Base model | Cartridge SMG (runtime clone) |

### Fantasy

Pure long-range precision. Slow, deliberate, high-impact kinetic shots. No energy charge, no smart targeting.

### Base stats

| Stat | Value |
|---|---|
| Damage | 145 |
| Fire rate | 48 RPM (1.25 s between shots) |
| Magazine | 5 |
| Reserve | 20 |
| Reload | 3.1 s total (single-round; ~0.62 s per shell) |
| Fire mode | Semi / bolt-action |
| Projectile | Travel time + bullet drop |
| Falloff | Soft after long range |
| ADS | Slow (~0.55 s), strong zoom |
| Hip-fire | Very poor |

### Single-round reload

Reload chambers **one round at a time**. Each shell’s animation is scaled so a full empty magazine still takes about the full reload duration. **Press fire during reload** (with at least one round chambered) to cancel the remaining shells and shoot immediately.


## Architecture

Runtime clone of `CartridgeSMG` (always present in `Global.AllGear`):

1. Clone prefab → new `GearInfo` + empty upgrade pool  
2. `ApplyBallisticSniperStats` rewrites `GunData`  
3. `BallisticSniperBehaviour` marks the gear for reload hooks  
4. Equip remaps NGO spawn to the vanilla base gun, then stamps catalog identity  

| File | Role |
|---|---|
| `Plugin.cs` | BepInEx entry, registration timing |
| `WeaponRegistration.cs` | Clone, GearInfo, sniper stats, AllGear inject |
| `BallisticSniperBehaviour.cs` | Identity host on catalog / live instances |
| `BallisticSniperReloadHook.cs` | Single-round reload + per-shell duration |
| `SpawnGearHooks.cs` | Equip remap + GearSelectionWindow safety |

## Install

```
<Mycopunk>/BepInEx/plugins/BallisticSniper.dll
```

Dependencies: Mycopunk, BepInEx 5.4.2403+.

## Build

```bash
dotnet build --configuration Release
```

Output: `bin/Release/netstandard2.1/BallisticSniper.dll`

## Persistence

Save data is keyed by gear **ID** (`87421`). The mod registers into `AllGear` **before** `PlayerData.OnAwake` runs `AddGear`, then re-binds `GearData.Gear` afterward so levels, unlock, and equip (`weapon1ID` / `weapon2ID`) survive relaunch.

Look for: `[BallisticSniper] Persistence OK: level=… unlocked=…`

## In-game checklist

1. BepInEx log shows gear registration (`ballistic_sniper`, id 87421)  
2. Gear select lists **Anti-Material Rifle** (auto-unlocked)  
3. Equip primary → slow semi fire, high damage, small mag  
4. Reload loads one round at a time  
5. ADS is accurate when standing still; hip-fire is poor  
6. Level up / equip, full quit, relaunch → progress and loadout kept  
7. Upgrade UI shows hex grid + inventory (Match-Grade Rounds is in the pool; own instances via drops/craft)  



## Notes

- Visuals currently reuse the Cartridge SMG model; swap later via `ModelImportHooks` / AssetBundle.  
- **28 upgrades** registered (Heavy Grain through Overkill, plus Boundary Incursion). See `upgrade notes.txt`.  
- Hold reload with full mag + High Explosive equipped to throw/detonate C4.  
- Uses standard projectile bullets (travel time + drop), not rail/hitscan.  


- Multiplayer: all clients need the same mod and matching gear id.

## License

MIT — see `LICENSE`
