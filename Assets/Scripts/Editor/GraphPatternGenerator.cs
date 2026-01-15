using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Generates beautiful graph patterns for level layouts
/// Creates structured patterns like triangular, grid, circular, and diamond formations
/// </summary>
public static class GraphPatternGenerator
{
    /// <summary>
    /// Calculate neutral-only zone (excluding producer/consumer zones)
    /// </summary>
    public static Bounds GetNeutralZone(Bounds gameZoneBounds, LevelCreationConfig config)
    {
        float zRange = gameZoneBounds.size.z;
        float producerZone = zRange * config.ProducerZoneSize;
        float consumerZone = zRange * config.ConsumerZoneSize;
        
        // Neutral zone excludes producer/consumer areas
        float neutralZMin = gameZoneBounds.min.z + producerZone;
        float neutralZMax = gameZoneBounds.max.z - consumerZone;
        
        return new Bounds(
            center: new Vector3(gameZoneBounds.center.x, 0, (neutralZMin + neutralZMax) / 2),
            size: new Vector3(gameZoneBounds.size.x, 0, neutralZMax - neutralZMin)
        );
    }
    
    /// <summary>
    /// Generate nodes in a specific pattern within bounds
    /// Now uses neutral-only zone to avoid producer/consumer areas
    /// </summary>
    public static List<BaseNode> GeneratePattern(
        GraphPattern pattern,
        int nodeCount,
        Bounds bounds,
        LevelCreationConfig config,
        LevelController level)
    {
        // Calculate neutral-only zone (excluding producer/consumer areas)
        Bounds neutralZone = GetNeutralZone(bounds, config);
        
        const float gridSize = 2f;
        List<Vector3> positions = pattern switch
        {
            GraphPattern.Triangular => GenerateTriangularPattern(nodeCount, neutralZone),
            GraphPattern.Grid => GenerateGridPattern(nodeCount, neutralZone),
            GraphPattern.Circular => GenerateCircularPattern(nodeCount, neutralZone),
            GraphPattern.Diamond => GenerateDiamondPattern(nodeCount, neutralZone),
            GraphPattern.Tree => GenerateTreePattern(nodeCount, neutralZone),
            GraphPattern.Mixed => GenerateMixedPattern(nodeCount, neutralZone),
            _ => GenerateGridPattern(nodeCount, neutralZone)
        };
        
        // Deduplicate positions to ensure no two nodes are at the same grid cell
        List<Vector3> uniquePositions = new List<Vector3>();
        HashSet<string> positionKeys = new HashSet<string>();
        
        foreach (Vector3 pos in positions)
        {
            Vector3 snappedPos = GridSnapper.SnapToGrid(pos, gridSize);
            string key = $"{snappedPos.x:F2}_{snappedPos.z:F2}";
            
            if (!positionKeys.Contains(key))
            {
                positionKeys.Add(key);
                uniquePositions.Add(snappedPos);
            }
        }
        
        // Get all existing nodes to check for collisions
        List<BaseNode> existingNodes = level != null ? level.GetAllNodes() : new List<BaseNode>();
        
        List<BaseNode> nodes = new List<BaseNode>();
        
        // Create nodes at calculated positions, ensuring no overlap with existing nodes
        foreach (Vector3 pos in uniquePositions)
        {
            if (nodes.Count >= nodeCount) break;
            
            // Check if position conflicts with existing nodes
            bool positionOccupied = GridSnapper.IsGridPositionOccupied(pos, existingNodes, gridSize);
            
            if (!positionOccupied)
            {
                BaseNode node = CreateNodeAtPosition(pos, NodeType.Neutral, config, level);
                if (node != null)
                {
                    nodes.Add(node);
                    existingNodes.Add(node); // Add to existing list to prevent future overlaps
                }
            }
            else
            {
                // Try to find nearby unoccupied position
                Vector3 alternativePos = GridSnapper.FindNearestUnoccupiedGridPosition(pos, existingNodes, gridSize, 10);
                if (!GridSnapper.IsGridPositionOccupied(alternativePos, existingNodes, gridSize))
                {
                    BaseNode node = CreateNodeAtPosition(alternativePos, NodeType.Neutral, config, level);
                    if (node != null)
                    {
                        nodes.Add(node);
                        existingNodes.Add(node);
                    }
                }
            }
        }
        
        return nodes;
    }
    
    /// <summary>
    /// Triangular/pyramid pattern - nodes form triangular layers
    /// </summary>
    private static List<Vector3> GenerateTriangularPattern(int nodeCount, Bounds bounds)
    {
        const float gridSize = 2f; // 2 unit spacing for better separation
        List<Vector3> positions = new List<Vector3>();
        
        float width = bounds.size.x * 0.8f;
        float height = bounds.size.z * 0.8f;
        Vector3 center = bounds.center;
        
        // Calculate layers needed
        int layers = Mathf.CeilToInt(Mathf.Sqrt(nodeCount * 2));
        
        for (int layer = 0; layer < layers && positions.Count < nodeCount; layer++)
        {
            int nodesInLayer = layer + 1;
            float layerHeight = center.z - height / 2 + (height * layer / layers);
            
            for (int i = 0; i < nodesInLayer && positions.Count < nodeCount; i++)
            {
                float t = nodesInLayer > 1 ? (float)i / (nodesInLayer - 1) : 0.5f;
                float layerWidth = width * (layer + 1) / layers;
                float x = center.x - layerWidth / 2 + layerWidth * t;
                
                Vector3 pos = new Vector3(x, 0, layerHeight);
                positions.Add(GridSnapper.SnapToGrid(pos, gridSize));
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// Grid pattern - nodes in neat rows and columns
    /// </summary>
    private static List<Vector3> GenerateGridPattern(int nodeCount, Bounds bounds)
    {
        const float gridSize = 2f; // 2 unit spacing for better separation
        List<Vector3> positions = new List<Vector3>();
        
        int cols = Mathf.CeilToInt(Mathf.Sqrt(nodeCount * 1.5f));
        int rows = Mathf.CeilToInt((float)nodeCount / cols);
        
        float width = bounds.size.x * 0.8f;
        float height = bounds.size.z * 0.8f;
        Vector3 center = bounds.center;
        
        // Use grid spacing (1 unit)
        float xSpacing = gridSize;
        float zSpacing = gridSize;
        
        // Calculate starting position to center the grid
        float startX = center.x - (cols - 1) * xSpacing / 2f;
        float startZ = center.z - (rows - 1) * zSpacing / 2f;
        
        for (int row = 0; row < rows && positions.Count < nodeCount; row++)
        {
            for (int col = 0; col < cols && positions.Count < nodeCount; col++)
            {
                float x = startX + col * xSpacing;
                float z = startZ + row * zSpacing;
                
                Vector3 pos = new Vector3(x, 0, z);
                positions.Add(GridSnapper.SnapToGrid(pos, gridSize));
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// Circular pattern - nodes arranged in concentric circles
    /// </summary>
    private static List<Vector3> GenerateCircularPattern(int nodeCount, Bounds bounds)
    {
        const float gridSize = 2f; // 2 unit spacing for better separation
        List<Vector3> positions = new List<Vector3>();
        
        float maxRadius = Mathf.Min(bounds.size.x, bounds.size.z) * 0.4f;
        Vector3 center = bounds.center;
        
        // Calculate rings needed
        int rings = Mathf.CeilToInt(Mathf.Sqrt(nodeCount / 2f)) + 1;
        
        for (int ring = 0; ring < rings && positions.Count < nodeCount; ring++)
        {
            float radius = maxRadius * (ring + 1) / rings;
            int nodesInRing = ring == 0 ? 1 : Mathf.CeilToInt(6 * (ring + 1));
            
            for (int i = 0; i < nodesInRing && positions.Count < nodeCount; i++)
            {
                float angle = (Mathf.PI * 2 * i / nodesInRing) + (ring * 0.3f); // Offset each ring
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;
                
                Vector3 pos = new Vector3(x, 0, z);
                positions.Add(GridSnapper.SnapToGrid(pos, gridSize));
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// Diamond pattern - nodes form diamond/rhombus shape
    /// </summary>
    private static List<Vector3> GenerateDiamondPattern(int nodeCount, Bounds bounds)
    {
        const float gridSize = 2f; // 2 unit spacing for better separation
        List<Vector3> positions = new List<Vector3>();
        
        float width = bounds.size.x * 0.8f;
        float height = bounds.size.z * 0.8f;
        Vector3 center = bounds.center;
        
        // Start with minimum layers and increase until we can fit nodeCount nodes
        int layers = 1;
        int maxLayers = 20; // Safety limit
        
        // Calculate how many layers we need
        while (layers < maxLayers)
        {
            int totalNodes = 0;
            for (int l = 0; l < layers; l++)
            {
                bool isUpperHalf = l < layers / 2f;
                int nodesInLayer = isUpperHalf ? l + 1 : layers - l;
                totalNodes += nodesInLayer;
            }
            
            if (totalNodes >= nodeCount)
                break;
            
            layers++;
        }
        
        // Generate positions up to nodeCount
        for (int layer = 0; layer < layers && positions.Count < nodeCount; layer++)
        {
            bool isUpperHalf = layer < layers / 2f;
            int nodesInLayer = isUpperHalf ? layer + 1 : layers - layer;
            
            float layerHeight = center.z - height / 2 + (height * layer / Mathf.Max(1, layers - 1));
            
            for (int i = 0; i < nodesInLayer && positions.Count < nodeCount; i++)
            {
                float t = nodesInLayer > 1 ? (float)i / (nodesInLayer - 1) : 0.5f;
                float layerWidth = width * nodesInLayer / layers;
                float x = center.x - layerWidth / 2 + layerWidth * t;
                
                Vector3 pos = new Vector3(x, 0, layerHeight);
                positions.Add(GridSnapper.SnapToGrid(pos, gridSize));
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// Tree pattern - hierarchical branching structure
    /// </summary>
    private static List<Vector3> GenerateTreePattern(int nodeCount, Bounds bounds)
    {
        const float gridSize = 2f; // 2 unit spacing for better separation
        List<Vector3> positions = new List<Vector3>();
        
        float width = bounds.size.x * 0.8f;
        float height = bounds.size.z * 0.8f;
        Vector3 center = bounds.center;
        
        int levels = Mathf.CeilToInt(Mathf.Log(nodeCount, 2)) + 1;
        
        for (int level = 0; level < levels && positions.Count < nodeCount; level++)
        {
            int nodesInLevel = Mathf.Min((int)Mathf.Pow(2, level), nodeCount - positions.Count);
            float levelHeight = center.z + height / 2 - (height * level / levels);
            
            for (int i = 0; i < nodesInLevel; i++)
            {
                float t = nodesInLevel > 1 ? (float)i / (nodesInLevel - 1) : 0.5f;
                float x = center.x - width / 2 + width * t;
                
                Vector3 pos = new Vector3(x, 0, levelHeight);
                positions.Add(GridSnapper.SnapToGrid(pos, gridSize));
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// Mixed pattern - combines multiple patterns
    /// </summary>
    private static List<Vector3> GenerateMixedPattern(int nodeCount, Bounds bounds)
    {
        const float gridSize = 2f; // 2 unit spacing for better separation
        // Use a combination of patterns
        List<Vector3> positions = new List<Vector3>();
        
        // Central circular cluster
        int circularCount = nodeCount / 3;
        var circularPositions = GenerateCircularPattern(circularCount, bounds);
        foreach (var pos in circularPositions.Take(circularCount))
        {
            positions.Add(GridSnapper.SnapToGrid(pos, gridSize));
        }
        
        // Grid around edges
        int gridCount = nodeCount - positions.Count;
        var gridBounds = new Bounds(bounds.center, bounds.size * 0.6f);
        var gridPositions = GenerateGridPattern(gridCount, gridBounds);
        foreach (var pos in gridPositions.Take(gridCount))
        {
            positions.Add(GridSnapper.SnapToGrid(pos, gridSize));
        }
        
        return positions;
    }
    
    /// <summary>
    /// Create a node at a specific position
    /// </summary>
    private static BaseNode CreateNodeAtPosition(Vector3 position, NodeType nodeType, LevelCreationConfig config, LevelController level)
    {
        GameObject prefab = config.GetNodePrefabByType(nodeType);
        if (prefab == null) return null;
        
        GameObject nodeObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (nodeObj == null) return null;
        
        nodeObj.transform.SetParent(level.transform);
        nodeObj.transform.position = position;
        
        BaseNode node = nodeObj.GetComponent<BaseNode>();
        if (node != null)
        {
            string nodeID = $"Node_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            node.NodeID = nodeID;
            nodeObj.name = $"Neutral_{nodeID}";
            level.AddNode(node);
        }
        
        return node;
    }
}

/// <summary>
/// Available graph patterns for level generation
/// </summary>
public enum GraphPattern
{
    Triangular,  // Pyramid/triangle formation
    Grid,        // Rectangular grid
    Circular,    // Concentric circles
    Diamond,     // Diamond/rhombus shape
    Tree,        // Hierarchical tree
    Mixed        // Combination of patterns
}

