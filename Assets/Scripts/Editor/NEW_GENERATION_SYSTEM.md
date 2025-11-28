# New Level Generation System

## Overview
Complete redesign of level generation focusing on beautiful graph patterns and strategic complexity through core paths + noise.

## Key Changes

### 1. Removed Wall Generation
- **Removed**: `PlaceWalls()` method and all wall spawning logic
- **Reason**: Too difficult to handle and unnecessary with pattern-based layouts

### 2. Planar Graph Constraint (No Intersections)
- **New Constraint**: Connections cannot cross each other
- **Implementation**: `ConnectionIntersectionChecker.cs` validates all connections
- **Runtime Check**: `ConnectionManager` prevents players from creating intersecting connections
- **Generation Check**: Core+Noise generator only creates non-intersecting connections
- **Algorithm**: Uses 2D line segment intersection test (XZ plane)
- **Exception**: Connections that share endpoints are allowed

### 3. Pattern-Based Node Placement (`GraphPatternGenerator.cs`)
Creates beautiful, structured node layouts inspired by graph theory.
These patterns naturally minimize connection intersections:

#### Available Patterns:
- **Triangular**: Pyramid/triangle layers for strategic vertical flow
- **Grid**: Rectangular layout with multiple parallel paths
- **Circular**: Concentric rings creating radial paths with natural cycles
- **Diamond**: Rhombus shape with focused center spreading to edges
- **Tree**: Hierarchical branching structure (top-down)
- **Mixed**: Combination of patterns for varied complexity

#### Features:
- Nodes arranged in aesthetically pleasing formations
- Natural flow from producers (bottom) to consumers (top)
- Balanced spacing and symmetry
- Each pattern creates different strategic challenges

### 4. Core + Noise Generation Strategy (`CoreNoiseGenerator.cs`)

#### Phase 1: Core Solvable Paths
- Creates guaranteed solvable paths from each producer to consumers
- **Distributes producers evenly** across consumers (no single consumer gets all paths)
- Path length varies by difficulty:
  - Easy: 2-4 nodes
  - Medium: 3-5 nodes
  - Hard: 4-6 nodes
  - Expert: 5-7 nodes
- Uses nearest-neighbor heuristic for natural-looking paths
- **Checks for intersections** before adding each connection

#### Phase 1.5: Multiple Possible Paths to Consumers
- Adds **alternative connection options** to consumers in connection mappings
- Creates multiple nodes that CAN connect to each consumer (but player chooses which one)
- Makes puzzle less obvious - players see multiple possible paths and must choose
- **Does NOT build connections** - only adds to mappings (player builds during gameplay)
- Since consumers can only have 1 incoming connection, player can only build ONE path
- Difficulty-based alternative options per consumer:
  - Easy: 0 alternatives (simple, clear solution)
  - Medium: 1 alternative option per consumer
  - Hard: 2 alternative options per consumer
  - Expert: 3 alternative options per consumer
- Creates strategic decision points - which path is optimal?
- **All options validated** for no intersections

#### Phase 2: Add Cycles
- Connects existing path nodes to create alternative routes
- Number of cycles based on difficulty:
  - Easy: 2x producer count (many options)
  - Medium: 1x producer count
  - Hard: 0.5x producer count
  - Expert: 0.33x producer count (minimal options)
- Creates strategic decision points
- **Only adds cycles that don't intersect** existing connections

#### Phase 3: Add Noise (Dead Ends)
- Connects unused neutral nodes to create false paths
- Noise intensity by difficulty:
  - Easy: 20% (light confusion)
  - Medium: 40%
  - Hard: 60%
  - Expert: 80% (heavy confusion)
- Creates appearance of more possibilities than actually exist
- May chain dead-end nodes for deeper false paths
- **All noise connections validated** for no intersections

## Level Editor Updates

### New UI Controls:
- **Graph Pattern Selector**: Choose visual layout pattern
- **Pattern Description**: Explains each pattern's characteristics
- **Increased Neutral Node Range**: 5-30 nodes (was 0-20)
- **Updated Help Text**: Describes new generation approach

### Generation Process:
1. Select pattern and difficulty
2. System generates neutral nodes in chosen pattern
3. Places producers at bottom (spread horizontally)
4. Places consumers at top (spread horizontally)
5. Applies node weights based on difficulty
6. Creates core solvable paths
7. Adds cycles for multiple solution options
8. Adds noise to increase apparent complexity
9. Validates solvability

## Benefits

### For Players:
- **Beautiful Visual Layouts**: Graphs look intentional and aesthetically pleasing
- **Clear Spatial Structure**: Pattern-based layouts are easier to parse visually
- **Strategic Depth**: Core paths + cycles create meaningful choices
- **Manageable Complexity**: Noise adds challenge without making levels impossible

### For Designers:
- **Guaranteed Solvability**: Core-first approach ensures levels can be beaten
- **Predictable Difficulty**: Difficulty tiers produce consistent challenge levels
- **Quick Iteration**: Generate multiple levels rapidly to find good ones
- **No Wall Management**: Simpler system without collision/wall placement logic

## Usage in Level Editor

1. Open: `Tools > Fruit Connect Level Editor`
2. Expand "Automatic Level Generation"
3. Set node counts (producers, consumers, neutrals)
4. Choose Graph Pattern from dropdown
5. Select Difficulty Tier
6. Click "Generate Level Automatically"
7. Review solvability and metrics
8. Adjust and regenerate if needed
9. Save level when satisfied

## Technical Notes

### Intersection Detection Algorithm
- Uses 2D line segment intersection test in XZ plane (ignores Y)
- Based on cross product method for computational geometry
- Checks if two line segments cross (not just if lines would intersect when extended)
- Special case: Connections sharing endpoints are allowed (not considered intersecting)
- Epsilon tolerance of 0.01 units for endpoint comparison
- O(n) complexity per connection check (n = existing connections)

### Generation Algorithms
- Patterns use mathematical formulas for node placement
- Core paths use greedy nearest-neighbor algorithm
- Cycles created through random sampling of core nodes
- Noise added with distance-based connectivity
- Energy weights assigned per difficulty tier
- All connections validated for:
  1. Capacity constraints
  2. Reverse connection check
  3. Intersection check
  
### Runtime Validation
- `ConnectionManager` validates intersections during gameplay
- Prevents players from dragging connections across existing ones
- Same intersection algorithm used as in generation
- Provides clear feedback when connection is blocked

