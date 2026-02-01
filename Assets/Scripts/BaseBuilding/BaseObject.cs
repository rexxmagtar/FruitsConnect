using UnityEngine;
using System.Collections.Generic;

public class BaseObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 20f;
    
    [Header("References")]
    [SerializeField] private List<BaseObjectStage> stages;

    private void Update()
    {
        // Continuous auto-rotation for better visual performance
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
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
            }
            else if (i == currentStageIndex)
            {
                // Currently building stage
                stages[i].gameObject.SetActive(true);
                stages[i].SetProgress(currentStageProgress, immediate);
            }
            else
            {
                // Future stages
                stages[i].gameObject.SetActive(false);
                stages[i].SetProgress(0f, immediate);
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
