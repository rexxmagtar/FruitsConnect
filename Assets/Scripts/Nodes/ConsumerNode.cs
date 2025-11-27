using UnityEngine;

/// <summary>
/// Consumer Node - Blue outline sphere
/// Shop/destination nodes that must be connected to producers to win
/// Consumers are endpoints - they cannot have outgoing connections
/// </summary>
public class ConsumerNode : BaseNode
{
    protected override void Awake()
    {
        base.Awake();
        
        // Consumers are endpoints - no outgoing connections allowed
        maxOutgoingConnections = 0;
        
        // Set up visual appearance for consumer (blue outline sphere)
        SetupConsumerVisuals();
    }
    
    private void SetupConsumerVisuals()
    {
        // If materials aren't assigned, create default blue outline material
        if (defaultMaterial == null && meshRenderer != null)
        {
            // For now, use a simple blue material
            // In production, you'd use a custom shader for outline effect
            defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = new Color(0.2f, 0.5f, 1f, 1f); // Blue
            meshRenderer.material = defaultMaterial;
        }
    }
}

