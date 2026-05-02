# COS 412 - 2D Action Platformer

Semester project built in **Unity 6** (2D, URP).  
Current playable state: melee-focused platformer with two main levels, enemy encounters, hazards, checkpoints, and two boss fights.
**Largely coded with AI per instructors permission**

## Requirements

- **Unity:** `6000.2.8f1` (Unity 6) from `ProjectSettings/ProjectVersion.txt`
- **Render pipeline:** URP (2D setup)

Open this folder in Unity Hub, then open the project.

## Scenes and Play Flow

Enabled build scenes (from `ProjectSettings/EditorBuildSettings.asset`):

1. `Assets/Scene Levels/first_level.unity`
2. `Assets/Scene Levels/second_level.unity`
3. `Assets/Scenes/SampleScene.unity`

Recommended flow for grading/playtest:

1. Open `Assets/Scene Levels/first_level.unity`
2. Press **Play** in the editor
3. Progress to level 2 through the in-game boss-defeat teleport sequence

## Controls (verified in current scripts)

- Move: `A/D` or left/right input axis
- Jump: `Space`
- Melee attack: left mouse button (`PlayerCombat`)

Notes:
- Facing is mouse-directed (`WeaponsManager` / `WeaponManager`).
- Exact bindings can still vary based on Unity Input Manager setup in scene.

## Implemented Systems

All gameplay scripts are in `Assets/Scripts/`.

- **Player core**
  - Movement/jump tuning with extra jumps and fall behavior (`GlitchPlayerController`)
  - Melee combat and sword hitbox damage (`PlayerCombat`, `SwordHitbox`)
  - Player health, i-frames, heart UI, respawn at checkpoints (`PlayerHealth`, `Checkpoint`, `KillZone`)
- **Game feel / camera / audio**
  - Camera follow and shake (`CameraFollow`, `CameraShake`)
  - Hit stop on melee impact (`HitStop`)
  - SFX manager and death effects (`AudioManager`, `SamuraiDeathVFX`)
- **Enemies**
  - Ground enemy AI and health (`EnemyAI`, `EnemyHealth`, `EnemyAttack`)
  - Skeleton enemy behavior (`SkeletonAI`)
  - Flying dive enemy with patrol/dive states (`FlyingDiveEnemy`)
  - Spawn trigger system (`EnemySpawner`)
- **Bosses**
  - Boss 1 encounter and activation (`BossController`, `BossActivator`)
  - Boss 2 encounter and activation (`boss2Script`, `Boss2Activator`)
  - Boss-clear progression hooks (`BossDefeatTeleportZone`, `BossDefeatRaiseBlock`)
- **Hazards / world interactions**
  - Spikes, falling spears, pendulum trap (`Spike`, `FallingSpear`, `PendulumSwing`)
  - Falling platform bridge behavior (`FallingPlatform`)
  - Boss arena checkpoint seal persistence (`BossArenaCheckpointSeal`)
  - Post-boss chest reward object (`TreasureChest`)

## Known Scope Notes

- Current script set is **melee-first**. A fully wired gun/ranged system is not represented in `Assets/Scripts/` as submitted.
- `SampleScene` remains in build settings and may be a test/legacy scene depending on your local setup.
