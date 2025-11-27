using UnityEngine;
using UnityEditor;
using System.IO;

namespace DataRepository.Editor
{
    public class DataClearTool : EditorWindow
    {
        [MenuItem("Tools/Data Repository/Clear Saved Data")]
        public static void ShowWindow()
        {
            GetWindow<DataClearTool>("Clear Saved Data");
        }

        private void OnGUI()
        {
            GUILayout.Label("Clear All Saved Data", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This will delete all saved game data including:\n" +
                "- Game progress data\n" +
                "- Treasure data\n" +
                "- Completed levels\n" +
                "- PlayerPrefs data\n" +
                "- GDPR consent\n" +
                "- Child age settings",
                MessageType.Warning
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Clear All Data"))
            {
                if (EditorUtility.DisplayDialog(
                    "Clear All Data",
                    "Are you sure you want to clear all saved data? This action cannot be undone.",
                    "Yes, Clear All",
                    "Cancel"
                ))
                {
                    ClearAllData();
                }
            }
        }

        private void ClearAllData()
        {
            // Clear game data file
            string gameDataPath = Path.Combine(Application.persistentDataPath, "gameData.dat");
            if (File.Exists(gameDataPath))
            {
                File.Delete(gameDataPath);
                Debug.Log("Deleted game data file: " + gameDataPath);
            }

            // Clear treasure data file
            string treasureDataPath = Path.Combine(Application.persistentDataPath, "treasuredata.json");
            if (File.Exists(treasureDataPath))
            {
                File.Delete(treasureDataPath);
                Debug.Log("Deleted treasure data file: " + treasureDataPath);
            }

            // Clear PlayerPrefs data
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("Cleared all PlayerPrefs data");

            // Show completion message
            EditorUtility.DisplayDialog(
                "Data Cleared",
                "All saved data has been cleared successfully.",
                "OK"
            );
        }
    }
}

