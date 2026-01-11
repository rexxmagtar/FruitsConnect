# Boss Setup Guide

This guide explains how to set up a boss for boss fights in levels.

## Overview

Bosses are manually placed in level prefabs and will trigger a boss fight mini-game when the level is completed (all consumers connected). The boss fight includes:
- Tap damage mechanics (same as monsters)
- Vulnerable zones that spawn periodically (deal 2x damage)
- Timer-based gameplay
- Win/loss conditions based on timer

## Creating a Boss Prefab

### 1. Create Boss GameObject

1. Create a new GameObject in your scene
2. Add the following components:
   - **Boss** script (from `Assets/Scripts/Boss/Boss.cs`)
   - **Animator** component (for animations)
   - **Collider** component (BoxCollider or SphereCollider for tap detection)
   - **Renderer** component (MeshRenderer or SkinnedMeshRenderer for visual)

### 2. Configure Boss Script

In the Boss component inspector, configure:

#### Boss Configuration
- **Max Health**: Starting health for the boss (e.g., 20)
- **Hit Scale Amount**: Scale multiplier when hit (e.g., 1.2)
- **Hit Scale Duration**: Duration of hit scale animation (e.g., 0.2)

#### Vulnerable Zone Settings
- **Vulnerable Zone Prefab**: Prefab for vulnerable zones (see Vulnerable Zone Setup below)
- **Vulnerable Zone Spawn Interval**: Time between vulnerable zone spawns (e.g., 5 seconds)
- **Vulnerable Zone Duration**: How long vulnerable zones last (e.g., 3 seconds)
- **Vulnerable Zone Spawn Radius**: Distance from boss to spawn zones (e.g., 2 units)
- **Max Vulnerable Zones**: Maximum number of zones active at once (e.g., 2)

#### Component References
- **Health Bar**: Reference to MonsterHealthBar component (can be auto-created)
- **Health Bar Prefab**: Optional prefab for health bar
- **Animator**: Reference to Animator component

#### Audio
- **Hit Sound**: AudioClip played when boss takes damage
- **Die Sound**: AudioClip played when boss dies
- **Escape Sound**: AudioClip played when boss escapes (timer runs out)
- **Audio Source**: AudioSource component (auto-created if not assigned)

#### Particle Effects
- **Hit Particle Effect**: ParticleSystem played when boss takes damage
- **Die Particle Effect**: ParticleSystem played when boss dies

#### Perfect Hit Feedback
- **Perfect Hit Sprite Prefab**: Prefab for "Perfect" sprite that appears when vulnerable zone is tapped

### 3. Setup Health Bar

The boss uses MonsterHealthBar for health display. You can either:

**Option A: Auto-create health bar**
- Leave Health Bar and Health Bar Prefab empty
- Health bar will be created automatically at runtime

**Option B: Use prefab**
- Assign a health bar prefab to Health Bar Prefab
- Health bar will be instantiated from prefab

**Option C: Manual setup**
- Create health bar GameObject as child of boss
- Add MonsterHealthBar component
- Assign to Health Bar reference

### 4. Setup Animator

The boss Animator Controller needs the following triggers:

- **GetHit**: Triggered when boss takes damage
- **Die**: Triggered when boss health reaches 0 (within time limit)
- **Escape**: Triggered when timer runs out

The Animator should have:
- **Idle** state (default, plays when boss is placed in level)
- Transitions from Idle to GetHit, Die, Escape states

### 5. Setup Boss Camera View

1. Create an empty GameObject in the scene (not as a child of the boss)
2. Name it "BossCameraView" or similar
3. Position this GameObject where you want the camera to be during the boss fight
4. Rotate it to face the boss (the camera will use this rotation)
5. Assign this Transform to the "Boss Camera View Transform" field in BossFightManager component
6. This transform can be reused for all boss fights - place it once in the scene

### 6. Place Boss in Level Prefab

1. Open your level prefab in the scene
2. Place the boss GameObject at the desired location
3. Make sure the boss is a child of the LevelController GameObject (or anywhere in the level hierarchy)
4. The boss will be found automatically by BossFightManager when the level completes

## Vulnerable Zone Setup

### 1. Create Vulnerable Zone Prefab

1. Create a new GameObject
2. Add the following components:
   - **VulnerableZone** script (from `Assets/Scripts/Boss/VulnerableZone.cs`)
   - **Collider** component (SphereCollider recommended)
   - **Renderer** component (for visual feedback)

### 2. Configure Vulnerable Zone

In the VulnerableZone component inspector:

#### Visual Settings
- **Pulse Speed**: Speed of pulsing animation (e.g., 2)
- **Pulse Scale Min**: Minimum scale during pulse (e.g., 0.9)
- **Pulse Scale Max**: Maximum scale during pulse (e.g., 1.1)
- **Glow Color**: Color for glow effect (e.g., yellow)

#### References
- **Zone Renderer**: Reference to Renderer component
- **Glow Light**: Optional point light for glow effect (auto-created if not assigned)

### 3. Visual Design

Vulnerable zones should be visually distinct:
- Use bright colors (yellow, orange, red)
- Add glow effects (emission material, point light)
- Add pulsing/scaling animation
- Make them clearly visible around the boss

## Level Configuration

### 1. Configure LevelConfig

In your LevelConfig ScriptableObject, set:

- **Is Boss Fight**: Check this box to enable boss fight for this level
- **Boss Gold Reward**: Gold awarded when boss is defeated (e.g., 50)
- **Boss Fight Time Limit**: Time limit in seconds to defeat boss (e.g., 30)

### 2. Example Configuration

```
Level Name: Level 5 - Boss Fight
Is Boss Fight: ✓
Boss Gold Reward: 50
Boss Fight Time Limit: 30
```

## Boss Fight Flow

1. **Level Completion**: Player connects all consumers
2. **Transition**: Map elements hide, camera moves to boss view
3. **Boss Alert**: "Alert Boss Fight!" UI appears
4. **Fight Starts**: Timer begins, boss becomes tappable
5. **Vulnerable Zones**: Spawn periodically around boss
6. **Win Condition**: Boss health reaches 0 within time limit
7. **Loss Condition**: Timer reaches 0 before boss is defeated
8. **Completion**: Level complete screen shows (with or without boss gold)

## Testing

1. Create a test level with a boss
2. Set `Is Boss Fight = true` in LevelConfig
3. Complete the level (connect all consumers)
4. Verify:
   - Boss alert appears
   - Camera moves to boss
   - Boss takes damage when tapped
   - Vulnerable zones spawn and work correctly
   - Timer counts down
   - Boss dies when health reaches 0
   - Boss escapes when timer runs out
   - Level complete screen appears after fight

## Troubleshooting

### Boss not found
- Ensure boss is in the level hierarchy
- Check that Boss component is attached
- Verify boss GameObject is active

### Health bar not showing
- Check that MonsterHealthBar component exists
- Verify health bar is child of boss
- Check health bar canvas settings

### Vulnerable zones not spawning
- Verify Vulnerable Zone Prefab is assigned in Boss script
- Check spawn interval and duration settings
- Ensure max vulnerable zones limit isn't reached

### Animations not playing
- Verify Animator Controller has correct triggers (GetHit, Die, Escape)
- Check that Animator component is assigned
- Ensure animation states are properly configured

## Notes

- Boss health is fixed in the prefab (not configurable per level)
- Timer is configurable per level in LevelConfig
- Boss gold is only awarded if boss is defeated within time limit
- Normal level coin reward is still awarded after boss fight
- Boss fight does not affect normal level completion flow (level is already complete)
