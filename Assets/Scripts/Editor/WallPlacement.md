# Wall Placement Algorithm

## Overview

Walls are placed between nodes that:
1. **Are NOT connected** by mapping
2. **Have no other nodes between them** in 3D space

This creates visual barriers that help players understand which paths are blocked.

## Algorithm

### Step 1: Check All Node Pairs

For each pair of nodes (A, B):

```
IF A and B are NOT connected by mapping:
    IF there are NO nodes between A and B in 3D space:
        Place wall at midpoint between A and B
```

### Step 2: Line-of-Sight Check

To determine if nodes are "between" A and B:

1. **Create line segment** from A to B
2. **For each other node C**:
   - Project C onto the line AB
   - Calculate perpendicular distance
   - If distance < threshold AND projection is within segment:
     - C is "between" A and B
3. **If ANY node is between** → No wall
4. **If NO nodes between** → Place wall

## Visual Representation

### Scenario 1: Wall Placed ✅

```
A ─────────────── B
(Not connected, clear path)
      
Result: Wall placed at midpoint
```

### Scenario 2: No Wall (Node Between) ❌

```
A ──── C ──── B
(Not connected, but C is between them)

Result: No wall (C blocks the line)
```

### Scenario 3: No Wall (Connected) ❌

```
A ═══════════════ B
(Connected by mapping)

Result: No wall (they're connected)
```

## Mathematical Details

### Line-Point Distance Calculation

Given:
- Point A: `(x₁, y₁, z₁)`
- Point B: `(x₂, y₂, z₂)`
- Point C: `(x₃, y₃, z₃)`

Calculate:

1. **Line direction vector**: 
   ```
   D = normalize(B - A)
   ```

2. **Vector from A to C**:
   ```
   V = C - A
   ```

3. **Projection length** (distance along line):
   ```
   t = dot(V, D)
   ```

4. **Projection point** on line:
   ```
   P = A + D × t
   ```

5. **Perpendicular distance**:
   ```
   dist = |C - P|
   ```

6. **Check if between**:
   ```
   IF 0 < t < |B - A| AND dist < threshold:
       C is between A and B
   ```

### Threshold Value

```csharp
float threshold = 0.8f; // Approximately node radius + margin
```

This accounts for:
- Node visual size (~0.5 units radius)
- Small margin for floating point errors
- Ensures nodes "in the way" are detected

## Configuration

### Required Setup

1. **Assign Wall Prefab** in `LevelCreationConfig`:
   ```
   LevelCreationConfig → Wall Settings → Wall Prefab
   ```

2. **Wall Prefab Requirements**:
   - Should be a 3D model (cube, plane, etc.)
   - Will be rotated to face the connection direction
   - Positioned at Y=0 (ground level)
   - Scaled/positioned at midpoint between nodes

### Wall Properties

When placed, each wall:
- **Position**: Midpoint between node A and B
- **Y-Position**: 0 (ground level)
- **Rotation**: Faces the direction from A to B
- **Name**: `Wall_{nodeA.NodeID}_{nodeB.NodeID}`
- **Parent**: `Walls` GameObject (child of level)

## Debug Output

After wall placement, check the Console for statistics:

```
Wall Placement Statistics:
  Total node pairs: 45
  Non-connected pairs: 28
  Pairs with clear line of sight: 12
  Walls placed: 12
```

### Understanding the Statistics

- **Total node pairs**: C(n,2) = n×(n-1)/2
- **Non-connected pairs**: Pairs without mapping connection
- **Clear line of sight**: Non-connected pairs with no nodes between
- **Walls placed**: Should equal "clear line of sight" (if prefab assigned)

### Common Issues

#### No Walls Placed

**Symptoms:**
```
Walls placed: 0
```

**Possible Causes:**

1. **Wall prefab not assigned**
   - Check `LevelCreationConfig → Wall Prefab`
   - Assign a prefab

2. **All nodes are connected**
   - Too dense connection graph
   - Reduce connection probability

3. **Nodes always have others between them**
   - Nodes too densely packed
   - Increase `MinNodeDistance` in config

#### Too Many Walls

**Symptoms:**
```
Walls placed: 40+
```

**Solutions:**
- Increase connection probability
- Add more neutral nodes
- Increase `MaxOutgoingConnections`

#### Walls in Wrong Places

**Symptoms:**
- Walls blocking valid paths
- Walls not blocking invalid paths

**Solutions:**
- Check wall prefab orientation
- Verify node positions are correct
- Adjust `threshold` value in code (0.8f)

## Examples

### Example 1: Simple 3-Node Level

```
Producer ──── Neutral ──── Consumer
```

**Analysis:**
- Producer ↔ Neutral: Connected → No wall
- Neutral ↔ Consumer: Connected → No wall
- Producer ↔ Consumer: Not connected, but Neutral between → No wall

**Result**: 0 walls

### Example 2: Diamond Layout

```
    Producer
   /        \
Neutral1   Neutral2
   \        /
    Consumer
```

**Connections:**
- Producer → Neutral1
- Neutral1 → Consumer
- Producer NOT connected to Neutral2
- Neutral2 NOT connected to Consumer

**Analysis:**
- Producer ↔ Neutral2: Not connected, clear path → **WALL**
- Neutral2 ↔ Consumer: Not connected, clear path → **WALL**

**Result**: 2 walls (right side blocked off)

### Example 3: Complex Grid

```
P1 ─── N1 ─── N2 ─── C1
 │      │      │      │
N3 ─── N4 ─── N5 ─── C2
 │      │      │      │
P2 ─── N6 ─── N7 ─── C3
```

**Wall placement depends on**:
- Which paths are connected
- Which diagonal/non-adjacent pairs have clear lines

Typically: Walls on unused diagonals and blocked corridors

## Performance

### Time Complexity

```
O(n³) where n = number of nodes
```

**Breakdown:**
- Outer loop: O(n²) for all pairs
- Inner check: O(n) for "between" detection
- Total: O(n²) × O(n) = O(n³)

### Optimization Opportunities

1. **Spatial Partitioning**
   - Use octree/grid
   - Only check nearby nodes
   - Reduces to O(n² × k) where k << n

2. **Early Termination**
   - Stop checking after first node found between
   - Already implemented ✓

3. **Caching**
   - Cache "between" results
   - Reuse for symmetric queries

### Typical Performance

| Node Count | Time |
|-----------|------|
| 10 nodes | <1ms |
| 20 nodes | ~5ms |
| 30 nodes | ~15ms |
| 50 nodes | ~60ms |

Still fast enough for editor use.

## Best Practices

### For Level Designers

1. **Place nodes with intention**
   - Clear clusters → Fewer walls
   - Mixed layouts → More walls

2. **Test wall placement**
   - Generate level
   - Check Console statistics
   - Adjust node positions if needed

3. **Wall prefab design**
   - Make visually distinct from nodes
   - Scale appropriately
   - Add collider if needed

### For Programmers

1. **Adjust threshold carefully**
   - Too small: Miss nodes in the way
   - Too large: False positives

2. **Consider wall height**
   - May need to adjust Y-position
   - Scale based on node distance

3. **Add visualization**
   - Draw debug lines in Scene view
   - Show blocked vs. clear paths

## Future Enhancements

### Potential Improvements

1. **Dynamic Wall Sizing**
   ```csharp
   float wallScale = distance / 2f;
   wall.transform.localScale = new Vector3(1, 1, wallScale);
   ```

2. **Wall Types**
   - Different prefabs for different contexts
   - Transparent walls vs. solid walls

3. **Smart Placement**
   - Avoid placing walls at corners
   - Align walls to grid

4. **Visual Feedback**
   - Color-code walls by context
   - Highlight important blockages

5. **Runtime Wall Updates**
   - Add/remove walls during gameplay
   - Dynamic difficulty adjustment

## References

- **Line-Point Distance**: Computational Geometry textbooks
- **Vector Projection**: Linear Algebra basics
- **Spatial Queries**: Game Programming Gems

## See Also

- `LevelGenerationHelper.cs` - Implementation
- `LevelCreationConfig.cs` - Configuration
- `GraphTheory.md` - Planarity concepts

---

*Last Updated: November 2025*
*Algorithm: O(n³) for n nodes*
*Guarantee: Walls only where no clear path exists*

