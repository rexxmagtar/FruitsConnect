using UnityEngine;

/// <summary>
/// Neutral Node - Red outline sphere
/// Pass-through nodes with variable outgoing connection slots
/// </summary>
public class NeutralNode : BaseNode
{
    private GameObject currentHelperSpirit;

    protected override void Awake()
    {
        base.Awake();
        
        // Set up visual appearance for neutral (red outline sphere)
        SetupNeutralVisuals();
    }
    
    protected override void ActivateNode()
    {
        base.ActivateNode();
        SpawnHelperSpirit();
    }

    public override void ResetDeliveries()
    {
        base.ResetDeliveries();
        DespawnHelperSpirit();
    }

    private void SpawnHelperSpirit()
    {
        if (currentHelperSpirit != null) return;

        // Only spawn spirits after level 5
        if (LevelsManager.Instance != null && LevelsManager.Instance.GetCurrentLevelNumber() <= 5)
        {
            return;
        }

        var currentParticleData = HitParticlesManager.Instance.GetCurrentParticle();
        if (currentParticleData != null && currentParticleData.helperSpiritPrefab != null)
        {
            currentHelperSpirit = Instantiate(currentParticleData.helperSpiritPrefab, transform.position, Quaternion.identity);
            var helperSpirit = currentHelperSpirit.GetComponent<HelperSpirit>();
            if (helperSpirit != null)
            {
                helperSpirit.Initialize(this);
            }
        }
    }

    private void DespawnHelperSpirit()
    {
        if (currentHelperSpirit != null)
        {
            var helperSpirit = currentHelperSpirit.GetComponent<HelperSpirit>();
            if (helperSpirit != null)
            {
                helperSpirit.Despawn();
            }
            else
            {
                Destroy(currentHelperSpirit);
            }
            currentHelperSpirit = null;
        }
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

