using UnityEngine;
using System.Collections.Generic;
using System;
#if UNITY_ANDROID || UNITY_IOS
using Io.AppMetrica;
using Newtonsoft.Json;
#endif

namespace AnalyticsServices
{
    public class AppMetricaAnalytics
    {
        private static AppMetricaAnalytics _instance;
        public static AppMetricaAnalytics Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppMetricaAnalytics();
                }
                return _instance;
            }
        }

        private AppMetricaAnalytics()
        {
        }

        /// <summary>
        /// Check if AppMetrica is activated (activation happens via AppMetricaActivator)
        /// Since activation happens automatically, we assume it's available if we're on mobile platform
        /// </summary>
        private bool IsActivated()
        {
#if UNITY_ANDROID || UNITY_IOS
            // AppMetrica is activated by AppMetricaActivator via RuntimeInitializeOnLoadMethod
            // We assume it's activated if we're on a mobile platform
            return true;
#else
            // On non-mobile platforms, allow events to be logged (but won't be sent)
            return true;
#endif
        }

        /// <summary>
        /// Report a custom event with parameters
        /// </summary>
        public void ReportEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!IsActivated())
            {
                Debug.LogWarning($"AppMetrica not activated. Cannot report event: {eventName}");
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            try
            {
                if (parameters != null && parameters.Count > 0)
                {
                    // AppMetrica ReportEvent expects event name and JSON string parameters
                    string jsonParams = JsonConvert.SerializeObject(parameters);
                    AppMetrica.ReportEvent(eventName, jsonParams);
                }
                else
                {
                    AppMetrica.ReportEvent(eventName);
                }

                Debug.Log($"[AppMetrica] Event: {eventName}, Parameters: {(parameters != null ? string.Join(", ", parameters) : "None")}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to report AppMetrica event {eventName}: {e.Message}");
            }
#else
            Debug.Log($"[AppMetrica] Event: {eventName}, Parameters: {(parameters != null ? string.Join(", ", parameters) : "None")}");
#endif
        }

        /// <summary>
        /// Report level_start event
        /// </summary>
        public void ReportLevelStart(int level, int daysSinceReg)
        {
            var parameters = new Dictionary<string, object>
            {
                { "level", level },
                { "days_since_reg", daysSinceReg }
            };
            ReportEvent("level_start", parameters);
        }

        /// <summary>
        /// Report level_complete event
        /// </summary>
        public void ReportLevelComplete(int level, int timeSpent, int daysSinceReg)
        {
            var parameters = new Dictionary<string, object>
            {
                { "level", level },
                { "time_spent", timeSpent },
                { "days_since_reg", daysSinceReg }
            };
            ReportEvent("level_complete", parameters);
        }

        /// <summary>
        /// Report level_fail event
        /// </summary>
        public void ReportLevelFail(int level, string reason, int timeSpent, int daysSinceReg)
        {
            var parameters = new Dictionary<string, object>
            {
                { "level", level },
                { "reason", reason ?? "unknown" },
                { "time_spent", timeSpent },
                { "days_since_reg", daysSinceReg }
            };
            ReportEvent("level_fail", parameters);
        }

        /// <summary>
        /// Report tutorial_start event
        /// </summary>
        public void ReportTutorialStart(string tutorialName, int daysSinceReg)
        {
            var parameters = new Dictionary<string, object>
            {
                { "tutorial_name", tutorialName },
                { "days_since_reg", daysSinceReg }
            };
            ReportEvent("tutorial_start", parameters);
        }

        /// <summary>
        /// Report tutorial_complete event
        /// </summary>
        public void ReportTutorialComplete(string tutorialName, int daysSinceReg)
        {
            var parameters = new Dictionary<string, object>
            {
                { "tutorial_name", tutorialName },
                { "days_since_reg", daysSinceReg }
            };
            ReportEvent("tutorial_complete", parameters);
        }

        /// <summary>
        /// Report purchase event
        /// </summary>
        public void ReportPurchase(string productId, string productName, double price, string currency, int daysSinceReg)
        {
            var parameters = new Dictionary<string, object>
            {
                { "product_id", productId },
                { "product_name", productName },
                { "price", price },
                { "currency", currency },
                { "days_since_reg", daysSinceReg }
            };
            ReportEvent("purchase", parameters);
        }

        /// <summary>
        /// Report puzzle_complete event
        /// </summary>
        public void ReportPuzzleComplete(string puzzleId, int daysSinceReg)
        {
            var parameters = new Dictionary<string, object>
            {
                { "puzzle_id", puzzleId },
                { "days_since_reg", daysSinceReg }
            };
            ReportEvent("puzzle_complete", parameters);
        }

        /// <summary>
        /// Report base_building_new_level event
        /// </summary>
        public void ReportBaseBuildingNewLevel(int baseLevel, int daysSinceReg)
        {
            var parameters = new Dictionary<string, object>
            {
                { "base_level", baseLevel },
                { "days_since_reg", daysSinceReg }
            };
            ReportEvent("base_building_new_level", parameters);
        }

    }
}
