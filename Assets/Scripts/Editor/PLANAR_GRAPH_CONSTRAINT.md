# Planar Graph Constraint

## Overview
The game enforces a **planar graph** constraint: **no two connections can intersect** (except at shared endpoints).

## Why This Matters
- **Visual Clarity**: Players can easily trace paths without confusion
- **Strategic Depth**: Limited connection options force careful planning
- **Beautiful Layouts**: Pattern-based generation creates clean, readable graphs
- **Puzzle Complexity**: Constraint adds natural difficulty without artificial barriers

## Implementation

### Editor-Time Validation (`ConnectionIntersectionChecker.cs`)
Used during level generation to ensure only valid connections are created.

**Key Methods:**
- `WouldConnectionIntersect(fromID, toID, allNodes, level)` - Check if proposed connection intersects
- `DoLinesIntersect(p1, p2, p3, p4)` - Line segment intersection test
- `GetAllConnectionLines(allNodes, level)` - Get all existing connection lines

**Algorithm:**
```
For each existing connection:
    Get endpoints (A, B) of existing line
    Get endpoints (C, D) of new proposed line
    
    If lines share an endpoint:
        Allow (not considered intersection)
    
    Use cross product to check if:
        - C and D are on opposite sides of line AB
        - A and B are on opposite sides of line CD
    
    If both true:
        Lines intersect - reject connection
```

### Runtime Validation (`ConnectionManager.cs`)
Prevents players from creating intersecting connections during gameplay.

**Validation Rule:**
- Added as Rule #7 in `ValidateConnection()`
- Checks proposed connection against all active connections
- Provides debug warning: "Cannot create connection: would intersect with existing connection"

**Methods:**
- `WouldConnectionIntersect(fromPos, toPos)` - Check against active connections
- `DoLinesIntersect(p1, p2, p3, p4)` - Same algorithm as editor version
- `CrossProduct2D(a, b)` - Helper for cross product calculation

## Cross Product Method Explained

Given two line segments:
- Segment 1: from A to B
- Segment 2: from C to D

**Step 1:** Calculate cross products to determine which side of each line the other points fall on:
```
d1 = CrossProduct(C - A, B - A)  // Which side of AB is C?
d2 = CrossProduct(D - A, B - A)  // Which side of AB is D?
d3 = CrossProduct(A - C, D - C)  // Which side of CD is A?
d4 = CrossProduct(B - C, D - C)  // Which side of CD is B?
```

**Step 2:** Lines intersect if:
- d1 and d2 have opposite signs (C and D on opposite sides of AB)
- AND d3 and d4 have opposite signs (A and B on opposite sides of CD)

**2D Cross Product:**
```
CrossProduct2D(Vector2 a, Vector2 b) = a.x * b.y - a.y * b.x
```

## Generation Integration

### Core Path Generation
- Each connection in core paths checked before adding
- If intersection detected, connection skipped
- Ensures core solvable paths are planar

### Cycle Addition
- Random cycles only added if they don't intersect
- May result in fewer cycles than requested
- Natural limit on complexity

### Noise Generation
- Dead-end paths validated for intersections
- False paths must also be planar
- Maintains visual clarity even with noise

## Pattern Selection Impact

Different patterns have different natural intersection rates:

### Low Intersection Patterns (Easy to connect):
- **Triangular**: Layered structure minimizes crossing opportunities
- **Tree**: Hierarchical prevents most intersections
- **Circular**: Radial layout naturally separates connections

### Higher Intersection Risk (More selective):
- **Grid**: Many nearby nodes increase intersection chances
- **Diamond**: Focused center can create congestion
- **Mixed**: Variable depending on combination

## Debugging

### Visualizing Intersections
In Scene View, connections are drawn as green lines. Look for:
- Crossing lines (should never occur)
- Dense areas where many connections converge
- Isolated nodes that couldn't connect due to intersections

### Common Issues
1. **Too few connections generated**: Pattern too dense, increase spacing
2. **Unsolvable levels**: Core paths blocked by earlier connections
3. **Asymmetric layouts**: One side gets more connections due to generation order

### Solutions
- Adjust node spacing in pattern generation
- Increase neutral node count for more connection options
- Try different patterns for the same node counts
- Lower difficulty tier for more connection attempts

## Performance Notes

**Time Complexity:**
- Checking one connection: O(n) where n = existing connections
- Generating full level: O(m * n) where m = attempted connections
- Typical: < 100ms for standard levels (10-20 nodes, 20-40 connections)

**Optimization Opportunities:**
- Spatial partitioning could reduce checks to O(log n)
- Currently not needed - levels generate fast enough
- Future: Consider if levels become very large (50+ nodes)

## Examples

### Valid (No Intersection):
```
A---B
    |
    C---D
```

### Invalid (Intersection):
```
A---B
 \ /
  X
 / \
C---D
```

### Valid (Shared Endpoint):
```
    B
   /|
  A |
   \|
    C
```

## Testing

### Manual Testing:
1. Generate level with desired pattern
2. Check Scene View for any crossing lines
3. Try to create crossing connection in play mode
4. Verify rejection message appears

### Expected Behavior:
- ✅ Generation completes without errors
- ✅ No visible crossing lines in Scene View
- ✅ Cannot create crossing connections during gameplay
- ✅ Clear feedback when connection blocked

## Future Enhancements

Potential improvements:
- Visual feedback showing why connection blocked (highlight intersecting line)
- Spatial indexing for faster intersection checks
- Alternative connection suggestions when blocked
- Intersection-aware auto-routing (suggest path around obstacles)

