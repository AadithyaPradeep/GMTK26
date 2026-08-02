# Controls

## Player

| Action | Input |
| ------ | ----- |
| Move | WASD or arrow keys (Unity Input System move action) |
| Grab / drop chicken | Space or E |
| Fire held laser / boss gun | Space or E (hold for gun bursts) |
| Pause | P (`PauseMenu`; music and SFX sliders) |

Aiming for grab uses a short 2D raycast in the farmer's last move direction (`HighlightCluck`). The highlighted chicken is the grab target.

## Modes (Home menu)

| Mode | What it does |
| ---- | ------------ |
| Story | Wave-based flock protection. Combo cycles Farm, Dusk, Graveyard via portals. Single-map Story is endless waves until the flock dies. |
| Chaos | Bomb (then electric) rush from the right edge with a gun in hand. Farmer locked to a vertical left lane. |

Maps: Farm (`SampleScene`), Dusk (`World2`), Graveyard (`World3`), or Combo.

Chicken roster and unlock waves per map: [CHICKENS.md](CHICKENS.md).

## Goal (Story)

Keep at least one protected chicken (normals + panic flock) alive. Threats explode or attack on timers. Losing the whole flock shows the game over card (retry / home). During the story boss laser protect phase, losing that laser also ends the run.

## Goal (Chaos)

Survive the endless rush. No flock-lives fail state in the same way; focus is dodging and shooting.

## Inspector knobs (common)

- `ChickenSpawner`: spawn area, wave duration, unlock waves, spawn %, threat caps, chaos timings
- `ChickenWander`: flee distance, flee speed, wander bounds (usually set by spawner via `SetWanderArea`)
- `Bomb` / `ElectricChicken` / fire / skele / laser: timers and radii
- `PlayerMovement`: base speed; Chaos applies lane + speed multiplier at runtime
- `GameAudio`: BGM and SFX volumes (also exposed in pause menu)
