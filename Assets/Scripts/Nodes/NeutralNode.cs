using UnityEngine;

/// <summary>
/// Neutral Node - Red outline sphere
/// Pass-through nodes with variable outgoing connection slots
/// </summary>
public class NeutralNode : BaseNode
{
    protected override void Awake()
    {
        base.Awake();
        
        // Set up visual appearance for neutral (red outline sphere)
        SetupNeutralVisuals();
    }
    
    private void SetupNeutralVisuals()
    {
        // If materials aren't assigned, create default red outline material
        if (defaultMaterial == null && meshRenderer != null)
        {
            // For now, use a simple red material with lower alpha
            // In production, you'd use a custom shader for outline effect
            defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = new Color(1f, 0.3f, 0.3f, 1f); // Light red
            meshRenderer.material = defaultMaterial;
        }
    }
}

