using UnityEngine;
using System.Collections.Generic;

public class BaseObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<BaseObjectStage> stages;

    /// <summary>
    /// Set the camera for all stages to use for billboarding
    /// </summary>
    public void SetCamera(Camera camera)
    {
        foreach (var stage in stages)
        {
            if (stage != null) stage.SetCamera(camera);
        }
    }

    /// <summary>
    /// Update the visual state of the base object based on current stage index
    /// </summary>
    /// <param name="currentStageIndex">Index from 0 to 9</param>
    public void UpdateVisuals(int currentStageIndex, float currentStageProgress, bool immediate = false)
    {
        for (int i = 0; i < stages.Count; i++)
        {
            if (i < currentStageIndex)
            {
                // Fully built stages
                stages[i].gameObject.SetActive(true);
                stages[i].SetProgress(1f, immediate);
                stages[i].SetPriceUIActive(false);
            }
            else if (i == currentStageIndex)
            {
                // Currently building stage
                stages[i].gameObject.SetActive(true);
                stages[i].SetProgress(currentStageProgress, immediate);
                stages[i].SetPriceUIActive(true);
            }
            else
            {
                // Future stages
                stages[i].gameObject.SetActive(false);
                stages[i].SetProgress(0f, immediate);
                stages[i].SetPriceUIActive(false);
            }
        }
    }

    public BaseObjectStage GetStage(int stageIndex)
    {
        if (stageIndex >= 0 && stageIndex < stages.Count)
        {
            return stages[stageIndex];
        }
        return null;
    }
}
