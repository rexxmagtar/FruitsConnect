using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages connection cutting mechanics - detects finger swipes across connections to destroy them
/// </summary>
public class ConnectionCutManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private GameController gameController;
    
    [Header("Particle Settings")]
    [Tooltip("Optional particle prefab for cut effect. If null, a simple particle system will be created automatically.")]
    [SerializeField] private GameObject cutParticlePrefab;
    [SerializeField] private float particleFollowSpeed = 20f;
    [SerializeField] private float particleLifetime = 0.5f;
    
    [Header("Cut Detection Settings")]
    [SerializeField] private float cutDetectionDistance = 0.3f; // Distance threshold for detecting line crossing
    [SerializeField] private LayerMask planeLayerMask = -1; // Layer mask for plane detection
    
    [Header("Line Trajectory Settings")]
    [SerializeField] private bool useLineTrajectory = true;
    [SerializeField] private int maxLinePoints = 15;
    [SerializeField] private float minPointDistance = 0.05f;
    [SerializeField] private float trajectoryPointLifetime = 0.5f; // How long each point stays before disappearing
    [SerializeField] private Color trajectoryColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    [SerializeField] private float trajectoryWidthMultiplier = 0.15f;
    [SerializeField] private AnimationCurve trajectoryWidthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
    [SerializeField] private int trajectoryCornerVertices = 4;
    [SerializeField] private int trajectoryEndCapVertices = 4;
    [SerializeField] private Material trajectoryMaterial;
    
    // Struct to track point position and time for lifecycle
    private struct TrajectoryPoint
    {
        public Vector3 position;
        public float timeCreated;

        public TrajectoryPoint(Vector3 pos, float time)
        {
            position = pos;
            timeCreated = time;
        }
    }

    // Cut state
    private bool isCutting = false;
    private Vector3 lastTouchPosition;
    private GameObject activeCutParticles;
    private ParticleSystem activeParticleSystem;
    private LineRenderer trajectoryLine;
    private List<TrajectoryPoint> trajectoryPoints = new List<TrajectoryPoint>();
    
    // Track connections that have been cut in this swipe (to avoid cutting same connection multiple times)
    private HashSet<Connection> cutConnectionsThisSwipe = new HashSet<Connection>();
    
    // Singleton
    private static ConnectionCutManager _instance;
    public static ConnectionCutManager Instance => _instance;
    
    private void Awake()
    {
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.Log("ConnectionCutManager: Destroying duplicate instance");
            Destroy(gameObject);
            return;
        }
        
        // Get references if not assigned
        if (connectionManager == null)
        {
            connectionManager = ConnectionManager.Instance;
        }
        
        if (gameController == null)
        {
            gameController = GameController.Instance;
        }
    }
    
    private void Update()
    {
        // Update trajectory points lifecycle even if not currently cutting
        // This allows the line to fade out naturally after the cut ends
        if (useLineTrajectory && trajectoryPoints.Count > 0)
        {
            UpdateTrajectoryLifecycle();
        }

        // Only process cuts when gameplay is enabled
        if (gameController == null || !gameController.GameplayEnabled)
        {
            if (isCutting)
            {
                EndCut();
            }
            return;
        }
        
        // Handle input
        bool isTouching = false;
        Vector3 touchPosition = Vector3.zero;
        
        // Check for mouse/touch input
        if (Input.GetMouseButtonDown(0))
        {
            isTouching = true;
            touchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            isTouching = true;
            touchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isCutting)
            {
                EndCut();
            }
            return;
        }
        
        // Handle touch input (mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            isTouching = true;
            touchPosition = touch.position;
            
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (isCutting)
                {
                    EndCut();
                }
                return;
            }
        }
        
        if (isTouching)
        {
            ProcessCutInput(touchPosition);
        }
    }

    /// <summary>
    /// Remove old points based on their lifetime
    /// </summary>
    private void UpdateTrajectoryLifecycle()
    {
        bool changed = false;
        float currentTime = Time.time;

        // Remove points that have exceeded their lifetime
        while (trajectoryPoints.Count > 0 && currentTime - trajectoryPoints[0].timeCreated > trajectoryPointLifetime)
        {
            trajectoryPoints.RemoveAt(0);
            changed = true;
        }

        // Update line renderer if points were removed
        if (changed)
        {
            UpdateTrajectoryLine();
            
            // If all points are gone, hide the line
            if (trajectoryPoints.Count == 0 && !isCutting && trajectoryLine != null)
            {
                trajectoryLine.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Process cut input - detect plane tap and track finger movement
    /// </summary>
    private void ProcessCutInput(Vector3 screenPosition)
    {
        // Raycast to find plane/level platform
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        
        // Try to hit the plane (level platform)
        // We'll use a plane at ground level Y
        float groundLevelY = connectionManager != null ? connectionManager.GroundLevelY : -0.48f;
        Plane plane = new Plane(Vector3.up, new Vector3(0, groundLevelY, 0));
        
        if (plane.Raycast(ray, out float distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance);
            
            if (!isCutting)
            {
                // Start cutting - check if we hit the plane (not a node or connection)
                // Also check if we're not currently dragging a node
                if (!IsHittingNodeOrConnection(ray) && !IsDraggingNode())
                {
                    StartCut(worldPosition);
                }
            }
            else
            {
                // Continue cutting - update particle position and check for line crossings
                UpdateCut(worldPosition);
            }
        }
    }
    
    /// <summary>
    /// Check if player is currently dragging a node (to avoid conflicts)
    /// </summary>
    private bool IsDraggingNode()
    {
        if (gameController == null) return false;
        return gameController.IsDragging;
    }
    
    /// <summary>
    /// Check if raycast hits a node or connection (to avoid starting cut when clicking on those)
    /// </summary>
    private bool IsHittingNodeOrConnection(Ray ray)
    {
        // Check for node hits
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            // Check if we hit a node
            if (hit.collider.GetComponent<BaseNode>() != null)
            {
                return true;
            }
            
            // Check if we hit a connection
            if (hit.collider.GetComponent<Connection>() != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Start cutting - spawn particles at touch position
    /// </summary>
    private void StartCut(Vector3 worldPosition)
    {
        isCutting = true;
        lastTouchPosition = worldPosition;
        cutConnectionsThisSwipe.Clear();
        
        // Spawn particles
        SpawnCutParticles(worldPosition);
        
        // Initialize trajectory line
        if (useLineTrajectory)
        {
            InitializeTrajectoryLine();
            trajectoryPoints.Clear();
            trajectoryPoints.Add(new TrajectoryPoint(worldPosition+Vector3.up*0.1f, Time.time));
            UpdateTrajectoryLine();
        }
        
        Debug.Log($"Started cutting at {worldPosition}");
    }
    
    /// <summary>
    /// Update cut - move particles and check for line crossings
    /// </summary>
    private void UpdateCut(Vector3 worldPosition)
    {
        // Update particle position to follow finger
        if (activeCutParticles != null)
        {
            // Smoothly move particles to new position
            Vector3 targetPos = worldPosition;
            activeCutParticles.transform.position = Vector3.Lerp(
                activeCutParticles.transform.position,
                targetPos,
                particleFollowSpeed * Time.deltaTime
            );
        }
        
        // Update trajectory line
        if (useLineTrajectory && trajectoryLine != null)
        {
            // Only add point if it moved enough
            if (trajectoryPoints.Count == 0 || Vector3.Distance(worldPosition, trajectoryPoints[trajectoryPoints.Count - 1].position) > minPointDistance)
            {
                trajectoryPoints.Add(new TrajectoryPoint(worldPosition, Time.time));
                
                // Keep only last N points
                if (trajectoryPoints.Count > maxLinePoints)
                {
                    trajectoryPoints.RemoveAt(0);
                }
                
                UpdateTrajectoryLine();
            }
        }
        
        // Check if finger path crosses any connections
        CheckConnectionCrossings(lastTouchPosition, worldPosition);
        
        // Update last position
        lastTouchPosition = worldPosition;
    }
    
    /// <summary>
    /// End cutting - cleanup particles
    /// </summary>
    private void EndCut()
    {
        isCutting = false;
        cutConnectionsThisSwipe.Clear();
        
        // Destroy particles after a delay
        if (activeCutParticles != null)
        {
            Destroy(activeCutParticles, particleLifetime);
            activeCutParticles = null;
            activeParticleSystem = null;
        }
        
        // Don't clear points immediately here, let UpdateTrajectoryLifecycle handle it
        // so the line fades out naturally
        
        Debug.Log("Ended cutting");
    }
    
    /// <summary>
    /// Initialize the trajectory line renderer
    /// </summary>
    private void InitializeTrajectoryLine()
    {
        if (trajectoryLine == null)
        {
            GameObject lineObj = new GameObject("TrajectoryLine");
            trajectoryLine = lineObj.AddComponent<LineRenderer>();
            
            // Set material
            if (trajectoryMaterial != null)
            {
                trajectoryLine.material = trajectoryMaterial;
            }
            else
            {
                // Create a simple material if none provided
                trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            trajectoryLine.startColor = trajectoryColor;
            trajectoryLine.endColor = trajectoryColor;
            trajectoryLine.widthCurve = trajectoryWidthCurve;
            trajectoryLine.widthMultiplier = trajectoryWidthMultiplier;
            trajectoryLine.numCornerVertices = trajectoryCornerVertices;
            trajectoryLine.numCapVertices = trajectoryEndCapVertices;
            trajectoryLine.positionCount = 0;
            trajectoryLine.useWorldSpace = true;
            
            // Use a higher sorting order to ensure it's visible over the ground
            trajectoryLine.sortingOrder = 10;
        }
        
        trajectoryLine.gameObject.SetActive(true);
        trajectoryLine.positionCount = 0;
        
        // Update visual settings in case they changed in inspector
        trajectoryLine.startColor = trajectoryColor;
        trajectoryLine.endColor = trajectoryColor;
        trajectoryLine.widthCurve = trajectoryWidthCurve;
        trajectoryLine.widthMultiplier = trajectoryWidthMultiplier;
        trajectoryLine.numCornerVertices = trajectoryCornerVertices;
        trajectoryLine.numCapVertices = trajectoryEndCapVertices;
    }
    
    /// <summary>
    /// Update the line renderer positions from tracked points
    /// </summary>
    private void UpdateTrajectoryLine()
    {
        if (trajectoryLine == null) return;
        
        trajectoryLine.positionCount = trajectoryPoints.Count;
        
        Vector3[] positions = new Vector3[trajectoryPoints.Count];
        for (int i = 0; i < trajectoryPoints.Count; i++)
        {
            positions[i] = trajectoryPoints[i].position;
        }
        
        trajectoryLine.SetPositions(positions);
    }
    
    /// <summary>
    /// Spawn cut particles at position
    /// </summary>
    private void SpawnCutParticles(Vector3 position)
    {
        if (cutParticlePrefab != null)
        {
            activeCutParticles = Instantiate(cutParticlePrefab, position, Quaternion.identity);
            activeParticleSystem = activeCutParticles.GetComponent<ParticleSystem>();
            
            if (activeParticleSystem == null)
            {
                activeParticleSystem = activeCutParticles.GetComponentInChildren<ParticleSystem>();
            }
        }
        else
        {
            // Create a simple particle system if no prefab is assigned
            activeCutParticles = new GameObject("CutParticles");
            activeCutParticles.transform.position = position;
            
            activeParticleSystem = activeCutParticles.AddComponent<ParticleSystem>();
            var main = activeParticleSystem.main;
            main.startColor = Color.red;
            main.startSize = 0.2f;
            main.startLifetime = particleLifetime;
            main.maxParticles = 100;
            
            var emission = activeParticleSystem.emission;
            emission.rateOverTime = 50f;
            
            var shape = activeParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
        }
    }
    
    /// <summary>
    /// Check if finger path crosses any connections and destroy them
    /// </summary>
    private void CheckConnectionCrossings(Vector3 startPos, Vector3 endPos)
    {
        if (connectionManager == null) return;
        
        List<Connection> activeConnections = connectionManager.GetActiveConnections();
        
        foreach (Connection connection in activeConnections)
        {
            if (connection == null || connection.IsCaptured) continue;
            
            // Skip if already cut in this swipe
            if (cutConnectionsThisSwipe.Contains(connection)) continue;
            
            // Check if finger path crosses this connection
            if (DoesPathCrossConnection(startPos, endPos, connection))
            {
                // Cut the connection!
                cutConnectionsThisSwipe.Add(connection);
                connectionManager.RemoveConnection(connection);
                
                Debug.Log($"Cut connection from {connection.FromNode?.NodeID} to {connection.ToNode?.NodeID}");
            }
        }
    }
    
    /// <summary>
    /// Check if a path (line segment) crosses a connection line
    /// Uses 2D line-line intersection in XZ plane (ignoring Y)
    /// </summary>
    private bool DoesPathCrossConnection(Vector3 pathStart, Vector3 pathEnd, Connection connection)
    {
        if (connection == null || connection.FromNode == null || connection.ToNode == null)
            return false;
        
        // Get connection line endpoints (at ground level)
        Vector3 connStart = connection.FromNode.transform.position;
        Vector3 connEnd = connection.ToNode.transform.position;
        float groundLevelY = connectionManager != null ? connectionManager.GroundLevelY : -0.48f;
        connStart.y = groundLevelY;
        connEnd.y = groundLevelY;
        
        // Project to XZ plane (2D)
        Vector2 pathStart2D = new Vector2(pathStart.x, pathStart.z);
        Vector2 pathEnd2D = new Vector2(pathEnd.x, pathEnd.z);
        Vector2 connStart2D = new Vector2(connStart.x, connStart.z);
        Vector2 connEnd2D = new Vector2(connEnd.x, connEnd.z);
        
        // Check if lines intersect
        if (LineSegmentsIntersect(pathStart2D, pathEnd2D, connStart2D, connEnd2D))
        {
            return true;
        }
        
        // Also check if path comes close to connection (within threshold)
        float minDistance = GetMinDistanceToLineSegment(pathStart2D, pathEnd2D, connStart2D, connEnd2D);
        if (minDistance < cutDetectionDistance)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if two line segments intersect (2D)
    /// </summary>
    private bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float d1 = Direction(p3, p4, p1);
        float d2 = Direction(p3, p4, p2);
        float d3 = Direction(p1, p2, p3);
        float d4 = Direction(p1, p2, p4);
        
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Calculate direction of point relative to line segment
    /// </summary>
    private float Direction(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p3.x - p1.x) * (p2.y - p1.y) - (p2.x - p1.x) * (p3.y - p1.y);
    }
    
    /// <summary>
    /// Get minimum distance from one line segment to another
    /// </summary>
    private float GetMinDistanceToLineSegment(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End)
    {
        // Check distance from line1 endpoints to line2
        float dist1 = DistanceToLineSegment(line1Start, line2Start, line2End);
        float dist2 = DistanceToLineSegment(line1End, line2Start, line2End);
        
        // Check distance from line2 endpoints to line1
        float dist3 = DistanceToLineSegment(line2Start, line1Start, line1End);
        float dist4 = DistanceToLineSegment(line2End, line1Start, line1End);
        
        return Mathf.Min(dist1, dist2, dist3, dist4);
    }
    
    /// <summary>
    /// Get distance from a point to a line segment
    /// </summary>
    private float DistanceToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        
        if (lineLength < 0.001f)
        {
            return Vector2.Distance(point, lineStart);
        }
        
        float t = Mathf.Clamp01(Vector2.Dot(point - lineStart, line) / (lineLength * lineLength));
        Vector2 projection = lineStart + t * line;
        
        return Vector2.Distance(point, projection);
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
