# Spawn Zone System

## Overview

Nodes spawn in specific zones to create natural gameplay flow:
- **Producers** → Bottom of game zone (Z min)
- **Consumers** → Top of game zone (Z max)  
- **Neutral nodes** → Middle area

This creates a natural progression from producers (sources) to consumers (destinations).

## Visual Layout

```
┌─────────────────────────────────┐
│     CONSUMER ZONE (Top 30%)     │  ← Consumers spawn here
│                                 │
├─────────────────────────────────┤
│                                 │
│     NEUTRAL ZONE (Middle)       │  ← Neutral nodes spawn here
│                                 │
├─────────────────────────────────┤
│    PRODUCER ZONE (Bottom 30%)   │  ← Producers spawn here
└─────────────────────────────────┘
     Game Zone (Z-axis)
```

## Configuration

### In LevelCreationConfig

Three new settings control spawn zones:

#### 1. Producer Zone Size
```
Range: 0.1 - 0.5 (10% - 50% of Z-axis)
Default: 0.3 (30%)
```
- Controls how much of the bottom area is used for producers
- **Larger value** → Producers more spread out vertically
- **Smaller value** → Producers clustered near bottom

#### 2. Consumer Zone Size
```
Range: 0.1 - 0.5 (10% - 50% of Z-axis)
Default: 0.3 (30%)
```
- Controls how much of the top area is used for consumers
- **Larger value** → Consumers more spread out vertically
- **Smaller value** → Consumers clustered near top

#### 3. Neutral Zone Margin
```
Range: 0.0 - 0.3 (0% - 30% of Z-axis)
Default: 0.2 (20%)
```
- Margin from both edges for neutral nodes
- **Larger margin** → Neutrals more centered
- **Smaller margin** → Neutrals can be near edges

## Examples

### Default Configuration (30/30/20)

```
Z-axis: 0 to 10 units

Consumer Zone:  7.0 - 10.0  (top 30%)
Neutral Zone:   2.0 - 8.0   (middle 60%, 20% margin)
Producer Zone:  0.0 - 3.0   (bottom 30%)

Overlap zones allow smooth transitions
```

### Tight Configuration (20/20/10)

```
More concentrated spawns:

Consumer Zone:  8.0 - 10.0  (top 20%)
Neutral Zone:   1.0 - 9.0   (middle 80%, 10% margin)
Producer Zone:  0.0 - 2.0   (bottom 20%)

Results in:
✓ Producers very close to bottom
✓ Consumers very close to top
✓ More vertical spread
```

### Spread Configuration (40/40/5)

```
More distributed spawns:

Consumer Zone:  6.0 - 10.0  (top 40%)
Neutral Zone:   0.5 - 9.5   (middle 90%, 5% margin)
Producer Zone:  0.0 - 4.0   (bottom 40%)

Results in:
✓ Producers spread across bottom half
✓ Consumers spread across top half
✓ Significant overlap possible
```

## Spawn Zone Calculations

### For Producers

```csharp
spawnZMin = bounds.min.z
spawnZMax = bounds.min.z + (zRange × producerZoneSize)

// Example: zRange = 10, producerZoneSize = 0.3
// spawnZMin = 0
// spawnZMax = 0 + (10 × 0.3) = 3.0
```

### For Consumers

```csharp
spawnZMin = bounds.max.z - (zRange × consumerZoneSize)
spawnZMax = bounds.max.z

// Example: zRange = 10, consumerZoneSize = 0.3
// spawnZMin = 10 - (10 × 0.3) = 7.0
// spawnZMax = 10
```

### For Neutral Nodes

```csharp
spawnZMin = bounds.min.z + (zRange × neutralZoneMargin)
spawnZMax = bounds.max.z - (zRange × neutralZoneMargin)

// Example: zRange = 10, neutralZoneMargin = 0.2
// spawnZMin = 0 + (10 × 0.2) = 2.0
// spawnZMax = 10 - (10 × 0.2) = 8.0
```

## Gameplay Benefits

### 1. Clear Visual Flow

Players immediately understand:
- Bottom nodes are **sources** (producers)
- Top nodes are **destinations** (consumers)
- Middle nodes are **bridges** (neutrals)

### 2. Natural Progression

Paths naturally flow from:
```
Producer (bottom) → Neutral (middle) → Consumer (top)
```

### 3. Difficulty Scaling

Adjust zones to change difficulty:

**Easier:**
- Larger zones (40/40)
- More overlap
- Shorter required paths

**Harder:**
- Smaller zones (20/20)
- Less overlap
- Longer required paths

## Considerations

### Zone Overlap

Zones can overlap! This is intentional:

```
Producer Zone:  0.0 - 3.0
Neutral Zone:   2.0 - 8.0
                  ↑
            Overlap: 2.0 - 3.0
```

**Benefits:**
- Smooth transitions
- More natural layouts
- Flexibility in positioning

### Minimum Zone Sizes

Constraints prevent invalid configurations:
- Producer/Consumer: 0.1 - 0.5 (10% - 50%)
- Neutral Margin: 0.0 - 0.3 (0% - 30%)

This ensures:
- Always enough space for spawning
- Zones don't consume entire area
- Neutral zone always exists

### X-Axis (Width)

All node types can spawn **anywhere** along X-axis:
```
X: bounds.min.x to bounds.max.x (full width)
```

Only Z-axis (depth) is constrained by zones.

## Advanced Usage

### Asymmetric Zones

Different sizes for producers vs consumers:

```csharp
producerZoneSize = 0.4  // 40% of bottom
consumerZoneSize = 0.2  // 20% of top

Result: Producers more spread, consumers tighter
```

### Dynamic Zone Adjustment

Adjust based on node counts:

```csharp
if (producerCount > 5) {
    producerZoneSize = 0.4f; // Need more space
}
```

### Level-Specific Zones

Different games/levels can use different zones:
- Puzzle game: Tight zones (20/20)
- Action game: Spread zones (40/40)
- Story mode: Progressive (start tight, end spread)

## Troubleshooting

### Problem: Nodes Too Clustered

**Symptoms:**
- All producers in tiny area
- All consumers in tiny area

**Solution:**
Increase zone sizes:
```
producerZoneSize: 0.3 → 0.4
consumerZoneSize: 0.3 → 0.4
```

### Problem: Nodes Too Spread Out

**Symptoms:**
- Producers across entire bottom half
- Long distances between nodes

**Solution:**
Decrease zone sizes:
```
producerZoneSize: 0.4 → 0.25
consumerZoneSize: 0.4 → 0.25
```

### Problem: Neutrals Overlapping Producers/Consumers

**Symptoms:**
- Neutral nodes at very top/bottom
- Confusing which is which

**Solution:**
Increase neutral margin:
```
neutralZoneMargin: 0.2 → 0.3
```

### Problem: Can't Find Valid Positions

**Symptoms:**
- Warning: "Could not find valid position after 100 attempts"

**Solutions:**
1. Increase game zone size
2. Decrease node counts
3. Increase zone sizes
4. Decrease `minNodeDistance`

## Testing Recommendations

### Visual Test

Generate level and check:
- [ ] Producers clustered near bottom
- [ ] Consumers clustered near top
- [ ] Neutrals in middle area
- [ ] Clear visual flow bottom → top

### Distance Test

Measure average distances:
```
Avg Producer Z: Should be < 30% of max Z
Avg Consumer Z: Should be > 70% of max Z
Avg Neutral Z:  Should be ~50% of max Z
```

### Overlap Test

Check zone overlaps make sense:
- Producer/Neutral overlap: OK
- Consumer/Neutral overlap: OK
- Producer/Consumer overlap: Should be minimal

## Performance

### Impact: Negligible

Zone calculations add:
- 3 float multiplications per node
- 2 range comparisons
- Total: <0.1ms overhead

No performance impact on generation.

## Future Enhancements

### Potential Features

1. **Curved Zones**
   ```csharp
   // Sinusoidal distribution instead of uniform
   float z = zMin + sin(random) * zoneSize;
   ```

2. **Clustered Spawning**
   ```csharp
   // Group nodes in clusters
   Vector3 clusterCenter = PickRandomInZone();
   SpawnNearby(clusterCenter, radius);
   ```

3. **Layered Zones**
   ```csharp
   // Multiple horizontal layers
   Layer[] layers = { Producer, Neutral1, Neutral2, Consumer };
   ```

4. **Dynamic Zone Heights**
   ```csharp
   // Adjust based on difficulty
   float zoneHeight = difficulty == Hard ? 0.2f : 0.4f;
   ```

5. **Custom Zone Shapes**
   ```csharp
   // Non-rectangular spawn areas
   bool IsInCustomZone(Vector3 pos, ZoneShape shape);
   ```

## Code Reference

### Implementation

See `LevelGenerationHelper.cs`:
```csharp
public static BaseNode CreateNodeAtRandomPosition(...)
{
    // Zone calculation
    switch (nodeType) {
        case NodeType.Producer:
            spawnZMin = zMin;
            spawnZMax = zMin + (zRange × config.ProducerZoneSize);
            break;
        // ... etc
    }
}
```

### Configuration

See `LevelCreationConfig.cs`:
```csharp
[Range(0.1f, 0.5f)]
[SerializeField] private float producerZoneSize = 0.3f;

[Range(0.1f, 0.5f)]
[SerializeField] private float consumerZoneSize = 0.3f;

[Range(0.0f, 0.3f)]
[SerializeField] private float neutralZoneMargin = 0.2f;
```

## Summary

### Key Points

✅ **Natural Flow**: Bottom → Middle → Top  
✅ **Configurable**: Adjust zones per game needs  
✅ **Flexible**: Zones can overlap  
✅ **Intuitive**: Players understand layout immediately  
✅ **No Performance Cost**: Negligible overhead  

### Default Settings

```
Producer Zone:  30% (bottom)
Consumer Zone:  30% (top)
Neutral Margin: 20% (from edges)
```

Works well for most games!

---

*Last Updated: November 2025*
*Default zones: Producer 30% bottom, Consumer 30% top, Neutral 20% margin*
*Fully configurable in LevelCreationConfig*

