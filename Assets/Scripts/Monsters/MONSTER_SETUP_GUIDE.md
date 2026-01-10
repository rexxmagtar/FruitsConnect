# Monster System Setup Guide

## Overview
The monster system has been implemented with all required scripts. This guide explains how to set up the monster prefab in Unity.

## Required Components for Monster Prefab

The monster prefab needs the following components:

1. **Model/Mesh Renderer** - The visual representation of the monster
2. **Collider** - Required for tap detection (BoxCollider, SphereCollider, or CapsuleCollider)
3. **Animator** - Must use the `Enemy1AnimationController` controller
4. **Monster** script - Main behavior component
5. **MonsterAiController** script - Animation control
6. **MonsterHealthBar** (optional child object) - Can be created at runtime if not provided

## Setup Steps

### 1. Create Monster GameObject
- Create a new GameObject in the scene
- Add your monster model/mesh as a child or assign to the main GameObject

### 2. Add Required Components
- **Collider**: Add a Collider component (BoxCollider recommended)
  - Size it appropriately to match the monster model
  - Ensure "Is Trigger" is **unchecked** (needed for OnMouseDown to work)
  
- **Animator**: Add Animator component
  - Assign `Enemy1AnimationController` controller
  - Located at: `Assets/Art/Animation/Enemy1AnimationController.controller`

- **Monster Script**: Add Monster component
  - Configure:
    - Max Health: 5 (default)
    - Movement Speed: 1.5 (default)
    - Reach Distance: 0.5 (default)
    - Position Offset Y: 1.5 (height above captured target)

- **MonsterAiController Script**: Add MonsterAiController component
  - Animator reference will be auto-assigned

### 3. Create Healthbar (Optional)
- Create a child GameObject named "MonsterHealthBar"
- Add Canvas component (set to World Space)
- Add CanvasScaler component
- Add GraphicRaycaster component
- Add MonsterHealthBar script
- Or leave it empty - Monster script will create it at runtime

### 4. Create Prefab
- Drag the configured GameObject to `Assets/Prefabs/Monsters/` folder
- Name it something like "Monster_Prefab"

### 5. Assign Prefab to MonsterAiManager
- Find or create MonsterAiManager in the scene
- Assign the monster prefab to the "Monster Prefab" field
- Configure spawn settings:
  - Min Spawn Interval: 10 seconds
  - Max Spawn Interval: 30 seconds
  - Max Active Monsters: 3

## Animation Controller Setup

The `Enemy1AnimationController` should have the following triggers:
- **FallSleep** - Trigger when goal is completed
- **FallingDown** - Trigger when taking damage

The controller should have these states:
- **Running** - When moving toward target
- **Sleep_Normally** - Idle state after capture

## Testing

1. Start the game
2. Create some connections between nodes
3. Monsters should spawn randomly
4. Monsters should move toward connections or nodes
5. Tap monsters multiple times to destroy them
6. When a monster reaches its target, it should capture it and sit on top

## Notes

- Monsters only spawn when gameplay is active (`GameController.GameplayEnabled == true`)
- Monsters handle their own tap detection via `OnMouseDown()`
- Captured nodes/connections remain grayscale and block player interaction
- When a monster dies, the captured target is freed
