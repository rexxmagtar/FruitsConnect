using UnityEngine;
using Firebase.Analytics;
using System.Collections.Generic;
using Firebase;
using System.Threading.Tasks;
using System;
using Firebase.Extensions;

namespace AnalyticsServices
{
public class AnalyticsService : MonoBehaviour
{
    private static AnalyticsService instance;
    public static AnalyticsService Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AnalyticsService>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public async Task Initialize(){

        bool firebaseInitialized = false;   
        
         FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            try{
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            }
            else
            {
                Debug.LogWarning($"Firebase initialization failed: {dependencyStatus}");
            }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during Firebase initialization: {e.Message} + {e.StackTrace}");
            }
            finally{
                firebaseInitialized = true;
            }
        });

        while(!firebaseInitialized){
            await Task.Delay(100);
        }
        
    }

    public void TrackSessionStart()
    {
        Parameter[] parameters = {
            new Parameter("game_name", "PuzzleTask"),
            new Parameter("game_version", Application.version)
        };
        FirebaseAnalytics.LogEvent("session_start", parameters);
    }

    public void TrackLevelStart(string missionId)
    {
        Parameter[] parameters = {
            new Parameter("level_name", missionId)
        };
        FirebaseAnalytics.LogEvent("level_start", parameters);
    }

    public void TrackLevelComplete(string missionId)
    {
        Parameter[] parameters = { new Parameter("level_name", missionId)
        };
        FirebaseAnalytics.LogEvent("level_complete", parameters);
    }

    public void TrackLayerRotation(int layerIndex, float angle)
    {
        Parameter[] parameters = {
            new Parameter("layer_index", layerIndex),
            new Parameter("rotation_angle", angle)
        };
        FirebaseAnalytics.LogEvent("layer_rotation", parameters);
    }

    public void TrackHintUsed()
    {
        FirebaseAnalytics.LogEvent("hint_used");
    }

    public void TrackPuzzleReset()
    {
        FirebaseAnalytics.LogEvent("puzzle_reset");
    }

    public void TrackTutorialStart()
    {
        FirebaseAnalytics.LogEvent("tutorial_begin");
    }

    public void TrackTutorialStep(string stepName)
    {
        Parameter[] parameters = {
            new Parameter("tutorial_step", stepName)
        };
        FirebaseAnalytics.LogEvent("tutorial_step", parameters);
    }

    public void TrackTutorialComplete()
    {
        FirebaseAnalytics.LogEvent("tutorial_complete");
    }
} 
}
