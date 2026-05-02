# COS 412 — 2D Action Platformer

**Largely coded with AI per instructors permission**

A semester project built in **Unity 6** (2D, URP): sword-and-gun combat, two main levels, bosses, traps, and light game-feel (camera shake, hit stop).

---

## Requirements

| | |
|---|---|
| **Unity** | **6000.2.x** (Unity 6) — see `ProjectSettings/ProjectVersion.txt` |
| **Template** | 2D / URP |

Clone or download the repo, open the project folder in Unity Hub, then open the project.

---

## How to play

1. Open a scene under **`Assets/Scene Levels/`** (recommended):
   - **`first_level`** — start here for the main flow.
   - **`second_level`** — continues after the first boss; the player **starts with the gun** unlocked in this scene.
2. Press **Play** in the Editor.

### Controls (typical setup)

| Action | Input |
|--------|--------|
| Move | Arrow keys / WASD |
| Aim gun | Mouse |
| Shoot | Hold **mouse button** (fire rate capped in `GunController`) |
| Melee | Bound in `PlayerCombat` (often mouse / attack input) |
| Switch sword ↔ gun | **Q** (after the gun is unlocked — `WeaponManager`) |

Exact bindings depend on your `Input Manager` / component setup in the scene.

---

## Main systems (scripts)

All gameplay scripts live in **`Assets/Scripts/`**.

| Area | Scripts (examples) |
|------|---------------------|
| **Player** | `GlitchPlayerController`, `PlayerHealth`, `PlayerCombat`, `WeaponManager`, `GunController`, `HeadAim` |
| **Enemies** | `EnemyAI`, `EnemyHealth`, `EnemyAttack`, `SkeletonAI`, `FlyingDiveEnemy`, `EnemySpawner` |
| **Bosses** | `BossController` (level 1), `boss2Script` (level 2), `BossActivator` / `Boss2Activator` |
| **World** | `Checkpoint`, `KillZone`, `CameraFollow`, `CameraShake`, `HitStop` |
| **Traps** | `Spike`, `FallingSpear`, `PendulumSwing`, `BossArenaCheckpointSeal` |
| **Progression** | `TreasureChest`, `GunPickup`, `BossDefeatTeleportZone` |

---

## Project layout (short)

```
Assets/
├── Scene Levels/          # first_level, second_level (main game)
├── Scripts/               # C# gameplay code
├── Scenes/                # Extra / legacy scenes (e.g. Level1, SampleScene)
├── Settings/              # URP / quality / project render settings
└── …                      # Art, audio, third-party asset folders
```

---

## Roadmap ideas

Ideas from earlier design notes (not all implemented): extra movement (dash, wall jump, jump pads), more enemy types, audio/music, UI for controls, polish passes.
