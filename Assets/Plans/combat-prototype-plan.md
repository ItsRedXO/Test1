# Project Overview
- **Game Title**: 3D Action RPG Prototype ("Phantom Blade / Project Phantom")
- **High-Level Concept**: Fast-paced, stylized 3D action RPG featuring top-down/isometric melee combat, fluid attack combos with movement, and reactive shield blocking against swarming enemies.
- **Players**: Single-player combat prototype
- **Inspiration / Reference Games**: *Spiral Knights*, *Minecraft Dungeons*
- **Tone / Art Direction**: Stylized, lightweight 3D visual style using simple primitive geometries and clean visual indicators, optimized for low-end PCs.
- **Target Platform**: PC Standalone (Windows 64-bit)
- **Screen Orientation / Resolution**: Landscape (1920x1080)
- **Render Pipeline**: Universal Render Pipeline (URP - PC_RPAsset)

---

# Game Mechanics

## Core Gameplay Loop
1. **Spawn & Explore**: The player spawns in an open test arena equipped with either a **Sword** or **Sword & Shield**.
2. **Engagement & Positioning**: Player moves with **WASD** and aims smoothly using **Mouse Facing** (raycast to ground plane).
3. **Aggressive Melee Combat**: Player performs 3-hit attack combo chains (**LMB**) toward the mouse cursor. Movement speed is maintained during attacks to keep combat fluid and mobile.
4. **Reactive Defense**: When enemies strike, holding **Block (RMB)** with the shield equipped mitigates incoming damage. Block Durability depletes per hit. If durability reaches zero, the player suffers a **Guard Break** and blocking is temporarily disabled.
5. **Regeneration & Counter-Attack**: Releasing block or avoiding hits initiates a recovery delay, after which Block Durability regenerates. Player switches weapons (**Key 1 / Key 2**) to evaluate combat feel across weapon archetypes.
6. **Enemy AI Loop**: Enemies spawn, detect the player, approach using NavMesh, wind up an attack, deal damage, react to hit-stun, and despawn/die when health reaches zero.

## Controls and Input Methods
- **Input System**: Unity New Input System (`UnityEngine.InputSystem`).
- **Movement**: WASD (2D Vector composite -> 3D horizontal movement vector).
- **Aim / Facing**: Mouse position projected onto the horizontal ground plane via camera raycast.
- **Primary Attack**: Mouse 1 (LMB) — Click/Hold to perform attack combos.
- **Block**: Mouse 2 (RMB) — Hold to raise shield (only available when Sword & Shield is equipped).
- **Switch Weapon**: Digit Key `1` (Sword) / Digit Key `2` (Sword & Shield) or Mouse Scroll.

---

# UI

A lightweight, high-visibility prototype Canvas HUD overlay:
1. **Player Health Bar**: Upper left screen corner, red fill bar showing `Current Health / Max Health`.
2. **Block Durability Bar**: Cyan fill bar underneath the Health Bar showing shield durability. Displays a yellow/red **"GUARD BROKEN!"** alert when guard break occurs.
3. **Active Weapon Indicator**: Text box displaying currently active weapon (`SWORD` vs `SWORD & SHIELD`).
4. **Enemy Health Bars**: Floating world-space health bar canvas above active enemies.
5. **Combat Feedback**: Floating text popups or console messages on hit/block/guard break for immediate gameplay tuning feedback.

---

# Key Asset & Context

### Project Architecture & Scripts

#### 1. Core Interfaces & Combat Data
- `IDamageable.cs`: Interface defining `void TakeDamage(DamageInfo damageInfo)`.
- `DamageInfo.cs`: Struct containing `float damageAmount`, `GameObject attacker`, `Vector3 knockbackForce`, `bool isBlockable`.
- `Faction.cs`: Enum (`Player`, `Enemy`) used by hitboxes to filter valid targets.
- `Health.cs`: Component managing `maxHealth`, `currentHealth`, death events, and hit reaction invocations.

#### 2. Weapon Data & Combat Systems
- `WeaponData.cs` (ScriptableObject): Modular asset holding weapon metadata:
  - `weaponName`, `weaponType` (`Sword`, `SwordAndShield`)
  - `comboChain`: Array of `ComboStep` structs containing damage multiplier, swing angle/arc, swing duration, combo reset window, attack forward speed factor.
  - `canBlock`: Boolean indicating shield support.
  - `maxBlockDurability`, `blockDamageReduction` (0.0 to 1.0 multiplier), `blockRegenDelay` (sec), `blockRegenRate` (durability/sec), `guardBreakThreshold`.
- `MeleeHitbox.cs`: Collider/SphereCast trigger attached to weapon transform or player front. Detects `IDamageable` targets during active swing frames.
- `WeaponHandler.cs`: Manages weapon mesh visibility, active attack combo states, block durability parameters, and hitbox toggling.

#### 3. Player Character Systems
- `PlayerController.cs`: Handles WASD movement using `CharacterController`, calculates mouse ground-intersection for rotation, and applies attack move speed factors.
- `PlayerCombatController.cs`: Listens to Attack/Block inputs, triggers combo steps on `WeaponHandler`, and controls weapon switching.
- `PlayerBlockSystem.cs`: Manages current shield durability, regeneration delay timer, blocking state evaluation (facing angle check vs hit direction), and Guard Break state machine.

#### 4. Camera System
- `IsometricCameraController.cs`: Smoothly follows player position maintaining an elevated third-person offset (`Vector3(0, 12, -8)`), looking at player with configurable smoothing.

#### 5. Enemy AI
- `EnemyAI.cs`: Simple FSM (`Idle`, `Chase`, `AttackWindup`, `Attacking`, `HitStun`, `Dead`) using `NavMeshAgent`.
- `EnemyMeleeAttack.cs`: Handles telegraph visual (color change/indicator), active attack frame, player damage delivery, and attack cooldown.

#### 6. Environment & Test Arena
- `TestArena`: Floor plane with NavMesh surface, wall boundaries, test obstacles, Player Spawn Point, and Enemy Spawners.

---

# Implementation Steps

| Step | Description | Assigned Role | Dependencies | Parallelizable |
|:---|:---|:---|:---|:---|
| **1** | **Project Setup & Layers Matrix**: Configure project tags, physics layers (`Player`, `Enemy`, `PlayerHitbox`, `EnemyHitbox`, `Ground`), and layer collision matrix. | developer | None | No |
| **2** | **Input Action Asset Setup**: Define Input Actions for Movement (WASD), Attack (LMB), Block (RMB), and Weapon Switch (Keys 1/2) using Unity New Input System. | developer | Step 1 | No |
| **3** | **Core Combat Architecture (`IDamageable`, `DamageInfo`, `Health`)**: Implement modular damage interface, health component with events, and target filtering. | developer | Step 1 | Yes |
| **4** | **Isometric Camera Controller**: Create script that smoothly follows the player from an elevated isometric perspective with configurable distance and tilt. | developer | Step 1 | Yes |
| **5** | **Player Movement & Mouse Facing**: Build `PlayerController` with WASD movement using `CharacterController` and mouse cursor ground raycast rotation. | developer | Step 2 | No |
| **6** | **Weapon Architecture & Hitbox System**: Create `WeaponData` ScriptableObject schema, `MeleeHitbox` trigger detector, and `WeaponHandler` component. | developer | Step 3 | No |
| **7** | **Player Attack Combo Logic**: Implement 3-hit attack combo chain in `PlayerCombatController`. Ensure movement while attacking is allowed with configurable speed scaling. | developer | Step 5, Step 6 | No |
| **8** | **Shield & Block Durability System**: Implement blocking state, directional hit mitigation, durability depletion, recovery delay timer, regeneration rate, and Guard Break state. | developer | Step 3, Step 6 | No |
| **9** | **Weapon Switching System**: Wire keybindings (1 = Sword, 2 = Sword & Shield) to switch active weapon data, visual mesh, and blocking capability seamlessly. | developer | Step 7, Step 8 | No |
| **10** | **Enemy AI & Navigation**: Set up NavMesh surface, `EnemyAI` FSM (Chase, Attack, HitStun, Death), and simple melee attack dealing damage to player. | developer | Step 3, Step 6 | No |
| **11** | **Test Arena Construction**: Build simple 3D arena layout with floor, boundary walls, obstacle pillars, player spawn point, and enemy spawner. | developer | Step 1 | Yes |
| **12** | **Prototype Combat HUD**: Create UGUI canvas displaying Player Health, Block Durability bar, Guard Break warning, Active Weapon name, and Enemy health bars. | developer | Step 3, Step 8 | No |
| **13** | **Integrated Gameplay Verification & Balance Tuning**: Conduct full end-to-end combat loop testing (movement, combo chain, blocking, guard break, weapon swap, enemy killing). | developer | Steps 1-12 | No |

---

# Verification & Testing

### Test Cases & Manual Verification Steps
1. **WASD Movement & Mouse Aiming**:
   - Verify player moves smoothly in 8 directions using WASD.
   - Verify character rotation constantly points toward the mouse cursor on the arena floor.
2. **Attack Combo & Mobility**:
   - Perform LMB clicks to chain Attack 1 -> Attack 2 -> Attack 3.
   - Verify timing window resets combo to step 1 if player pauses between clicks.
   - Verify player can move freely while executing attacks (without being locked in place).
3. **Blocking & Damage Mitigation**:
   - Equip Sword & Shield (Key 2) and hold RMB.
   - Verify incoming enemy hits deal reduced damage to health while deducting from Block Durability.
4. **Block Durability & Guard Break**:
   - Continue holding block while taking hits until durability reaches 0.
   - Verify Guard Break triggers: block lowers, "GUARD BROKEN!" alert appears, and block input is rejected.
   - Verify durability regenerates only after the configured recovery delay when unblocked/guard broken.
5. **Weapon Switching**:
   - Press Key 1 (Sword): verify blocking is disabled and single fast combo is active.
   - Press Key 2 (Sword & Shield): verify block functionality is restored.
6. **Enemy AI Loop**:
   - Spawn enemy: verify it detects player, navigates around obstacles via NavMesh, stops at attack range, performs telegraphed strike, flashes red on receiving hits, and dies when health reaches zero.
