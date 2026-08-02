# AGENTS.md

Handoff notes for humans and AI agents working in this repo. Do not rely on prior chat history; this file plus `docs/` are the source of truth.

## What this is

**Exploding Chickens** is a Unity 6 (6000.5.4f1) 2D URP jam game with post-jam expansions (more maps, chicken types, music, UI). Public page: https://itch.io/jam/gmtk-jam-2026/rate/4827331

Open **`GMTK26 V1`** in the Unity Hub. The git root is one level above the Unity project.

## Before you change code

1. Read [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/CHICKENS.md](docs/CHICKENS.md), and [docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md).
2. Prefer small, scoped edits. Match existing naming and patterns.
3. Do not invent Animator parameters. Check controllers under `Assets/Animations/` first.
4. Do not commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or builds.
5. Ignore `Assets/_Recovery/`.

## Script map (start here)

| Area | Key files |
| ---- | --------- |
| Player | `PlayerMovement.cs`, `HighlightCluck.cs`, `GrabCluck.cs` |
| Flock AI | `ChickenWander.cs` |
| Waves / modes | `ChickenSpawner.cs`, `GameMode.cs`, `MapRoster.cs` |
| Farm threats | `Bomb.cs`, `MindCluck.cs`, `ElectricChicken.cs`, `LaserChicken.cs`, `PanicChicken.cs`, `RogueChicken.cs` |
| Dusk threats | `FireChicken.cs`, `AlienChicken.cs`, `AlienLiftVictim.cs`, `FireballProjectile.cs` |
| Graveyard threats | `SkeleCluck.cs`, `GhostChicken.cs`, `HauntVisionController.cs`, `ArrowProjectile.cs` (+ zombie/rogue prefabs on `Bomb`/`RogueChicken`) |
| Boss | `BossChicken.cs`, `BossMissile.cs`, `BossSideBeam.cs`, `BossLivesDisplay.cs` |
| UI / flow | `HomeMenu.cs`, `MapSelectUI.cs`, `WaveTimerUI.cs`, `CluckLivesUI.cs`, `PauseMenu.cs`, `HowToPlayIntro.cs`, `SceneFader.cs`, `LevelPortal.cs`, `ChickenDirectoryUI.cs` |
| Audio | `GameAudio.cs` |

All gameplay scripts live flat in `GMTK26 V1/Assets/Scripts/` (plus `Editor/ChickenDirectoryAssetBuilder.cs`).

## Adding a new chicken type

1. Prefab under `Assets/Prefabs/` + art under `Assets/Sprites/Variety Clucks/` (and animators).
2. Behavior script if needed; otherwise reuse `Bomb` / markers and teach `ChickenWander.RefreshTypeFlags()` if AI must change.
3. Add directory entry in `ChickenDirectoryCatalog.CreateRuntimeDefaults()` (and regenerate assets if you use the editor menu).
4. Add `MapRoster` slot(s) with the **exact** display name.
5. Wire prefab arrays on each world's `ChickenSpawner` in the scene / prefab setup.

## Hard rules from past bugs

- Grab: always null-check `hc.GetSelectedClucks()` before using the result.
- Wander bounds: use `SetWanderArea`; do not poke private `areaMin` / `areaMax`.
- After spawning, set `wander.farmerTransform` (spawner already does this).
- Type behavior is mostly components + `RefreshTypeFlags()`, not subclassing wander.
- Gate player Update logic with `PauseMenu.IsPaused` where other scripts already do.
- Grab uses Space **or** E on `Keyboard.current`. Pause uses **P**.
- Ghosts are protected from generic explode cleanup (`GhostChicken.IsProtected`).

## Scenes and modes

| Scene | Map |
| ----- | --- |
| `HomeScene` | Menu |
| `SampleScene` | Farm |
| `World2` | Dusk |
| `World3` | Graveyard |

`GameMode`: Story vs Chaos; map ids `farm`, `dusk`, `graveyard`, `combo`. Single-map Story waves are endless until flock wipe. Chaos is the right-edge rush + gun lane.

## Docs style

When editing markdown in this repo: no emojis, no em-dashes. Prefer plain ASCII punctuation.
