# Known issues and history

## Fixed (early prototype / jam)

| Issue | Fix |
| ----- | --- |
| NullReferenceException when grabbing with nothing highlighted | `GrabCluck` null-checks `GetSelectedClucks()` before using `.transform` |
| Spawner could not set private wander bounds | `ChickenWander.SetWanderArea(Vector2 min, Vector2 max)` |
| Chickens needed flee-from-farmer | Flee layer in `ChickenWander` (edge-aware slide so they do not stick in corners) |
| Farmer shadow / polish | Git history includes ShadowFix work on farmer presentation |

## Fixed (post-jam / continued development)

| Issue | Notes (from recent history) |
| ----- | --------------------------- |
| Lightning / electric chicken bug | Fixed on main (`fixed lightning chicken bug`) |
| Ghost chicken spam | Tuned to appear once per wave |
| Lives UI | Fixed lives icon; added red blinker on damage |
| Single-map length | Infinite / endless waves for solo Story maps |
| Music coverage | Map OSTs and ability SFX wired through `GameAudio` (+ Resources fallbacks) |
| Game over UX | Centered game over card with retry / home actions |
| Map select | Upgraded map selection UI with roster / previews |
| Difficulty | Tuning pass (`Difficulty AdjustmentV1`) |

## Intentional design (often reported as "bugs")

| Feedback from jam | Notes |
| ----------------- | ----- |
| Difficulty ramps hard / chaotic early | Deliberate chaos fantasy. Holding one chicken and running is a common strategy. |
| Hard to protect many chickens at once | Grab is one-at-a-time; lasers/gun unlock later or in Chaos. |
| Pickup range feels short | `HighlightCluck` range is inspector-tuned (default ~1). |

## Remaining quirks / tech debt

| Item | Detail |
| ---- | ------ |
| Grab input | Space/E hard-coded on `Keyboard.current` in `GrabCluck`, not Input System Interact |
| Pause input | P hard-coded (not Esc) |
| `HighlightCluck.ToogleInteraction` | Method name misspelled (`Toogle`) |
| Directory display name | Zombie entry uses a long joke name; `MapRoster` must match it exactly |
| Folder typo | `Sprites/Variety Clucks/SkeeltonClcuk` (art folder spelling) |
| `Assets/_Recovery/` | Unity crash recovery scenes; ignore for builds |
| Testing shortcuts on spawner | `startAtWaveForTesting` / `startAtBossWaveForTesting` must stay off for shipping |
| Nested Unity folder | Open `GMTK26 V1` in the Hub, not the git root |

## Theme fit (jam statement)

Different exploding (and other) chickens explode on countdown timers in waves that also have countdowns.
