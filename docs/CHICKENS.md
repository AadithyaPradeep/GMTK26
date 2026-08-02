# Chicken types

Canonical display names and copy live in `ChickenDirectoryCatalog.CreateRuntimeDefaults()` and `Resources/ChickenDirectory/`. Map unlock previews use `MapRoster` (keep that in sync with each world's `ChickenSpawner` inspector unlocks).

Boss (`BossChicken` / `BossCluck`) is a special encounter scripted by the spawner, not a directory catalog entry.

## Allies (flock lives)

| Name | Component / prefab | Behavior |
| ---- | ------------------ | -------- |
| Regular Cluck | `Cluck.prefab` + `ChickenWander` | Default protect target. All normals dead = game over. |
| Panic Cluck | `PanicChicken` + `PanicCluck.prefab` | Always sprinting (no idle). Still counts as flock. |

## World 1 - Farm (`SampleScene`)

| Name | Unlock wave (MapRoster) | Component / prefab | Behavior |
| ---- | ----------------------- | ------------------ | -------- |
| Regular Cluck | 1 | `Cluck` | Ally |
| Bomb Cluck | 1 | `Bomb` + `BombCluck` | Fuse countdown, AoE kill |
| Panic Cluck | 2 | `PanicChicken` | Ally sprinter |
| Rogue Cluck | 2 | `Bomb` + `RogueChicken` | Bomb fuse + panic sprint |
| Mind Cluck | 2 | `MindCluck` | Pulse attract aura |
| Electric Cluck | 3 | `ElectricChicken` | Countdown, then lightning AoE |
| Laser Cluck | 4 | `LaserChicken` + `LaserCluck` | Eye beam; can be held / used as weapon |

Chaos on Farm: endless Bomb + Electric rush (gun lane).

## World 2 - Dusk (`World2`)

| Name | Unlock wave | Component / prefab | Behavior |
| ---- | ----------- | ------------------ | -------- |
| Regular Cluck | 1 | `Cluck` | Ally |
| Blue Fire Cluck | 1 | `FireChicken` + `BlueFireCluck` | Fireball bursts (blue variant) |
| Panic Cluck | 2 | `PanicChicken` | Ally |
| Rogue Cluck | 2 | `RogueChicken` | Sprinting bomb |
| Alien Cluck | 3 | `AlienChicken` + `AlienCluck` | Gravity pulse, lifts and kills victims |
| Fire Cluck | 3 | `FireChicken` + `FireCluck` | Fireball volleys |

## World 3 - Graveyard (`World3`)

| Name | Unlock wave | Component / prefab | Behavior |
| ---- | ----------- | ------------------ | -------- |
| Regular Cluck | 1 | `Cluck` | Ally |
| Zombie Cluck | 1 | `Bomb` + `ZombieCluck` | Graveyard bomb (directory name is long / joke-flavored) |
| Panic Cluck | 2 | `PanicChicken` | Ally |
| Rogue Zombie Cluck | 2 | `RogueChicken` + `RogueZombieCluck` | Undead bomb + sprint |
| Skele Cluck | 3 | `SkeleCluck` + `SkelCluck` | Arrow volleys |
| Ghost Cluck | 4 | `GhostChicken` + `GhostCluck` | Chase farmer, lights-out haunt (limited haunts, then despawn) |

## Boss and projectiles

| Piece | Files | Notes |
| ----- | ----- | ----- |
| Boss Cluck | `BossChicken`, `BossCluck.prefab` | Missile salvos, side lasers, lives UI |
| Boss missile | `BossMissile`, `Missile.prefab` | Homing / salvo projectiles |
| Side beam | `BossSideBeam` | Boss special |
| Fireball | `FireballProjectile`, `Fireball.prefab` | Fire Cluck shots |
| Arrow | `ArrowProjectile`, `Arrow.prefab` | Skele Cluck shots |
| Gun | `Gun.prefab` + `LaserChicken` boss-gun config | Chaos / boss forced weapon |
| Explosions | `Explosion`, `BlueExplosion`, `GreenExplosion` | Blast VFX |
| Gate / Lives / Spawn | `Gate`, `Lives`, `Spawn` | Portal, boss HP icons, spawn FX |

## Prefab checklist (`Assets/Prefabs/`)

`AlienCluck`, `Arrow`, `BlueExplosion`, `BlueFireCluck`, `BombCluck`, `BossCluck`, `Cluck`, `Electric`, `ElectricChicken`, `Explosion`, `Fireball`, `FireCluck`, `Gate`, `GhostCluck`, `GreenExplosion`, `Gun`, `Laser`, `LaserCluck`, `Lives`, `MindCluck`, `Missile`, `PanicCluck`, `RogueZombieCluck`, `SkelCluck`, `Spawn`, `ZombieCluck`

## Art and directory portraits

- Sprite sheets / variants: `Assets/Sprites/Variety Clucks/` (Alien, Electric, Fire, Laser, Mind, Panic, RoboBoss, Skeleton, Zombie, ...)
- Map select previews: `Assets/Sprites/MapPreviews/`
- In-game directory portraits: `Assets/Resources/ChickenDirectory/`
- Branding / splash pack: `Assets/EXPLODING_CHICKENS/`
- Cover art: `Assets/Sprites/MainCoverArt.png`

## Audio (per type / map)

Clips under `Assets/Music/` and mirrored in `Assets/Resources/Music/` where needed for runtime loads (`GameAudio`):

- Map BGM: `OST_Loop`, `OST_NIGHT`, `OST_GRAVEYARD`, home jazz
- Ability / spawn: Alien, Fire, Laser, Laser_Hit, Hypnose / Hypnotic_BG, Zombie
