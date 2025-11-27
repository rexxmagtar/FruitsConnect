using UnityEngine;

/// <summary>
/// Producer Node - Red filled sphere
/// Starting point for fruit flow
/// </summary>
public class ProducerNode : BaseNode
{
    protected override void Awake()
    {
        base.Awake();
        
        // Set up visual appearance for producer (red filled sphere)
        SetupProducerVisuals();
    }
    
    private void SetupProducerVisuals()
    {
        // If materials aren't assigned, create default red material
        if (defaultMaterial == null && meshRenderer != null)
        {
            defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = Color.red;
            defaultMaterial.EnableKeyword("_EMISSION");
            defaultMaterial.SetColor("_EmissionColor", Color.red * 0.3f);
            meshRenderer.material = defaultMaterial;
        }
    }
}

