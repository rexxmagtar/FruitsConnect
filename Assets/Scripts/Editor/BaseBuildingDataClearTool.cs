using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using DataRepository;

public class BaseBuildingDataClearTool : EditorWindow
{
    private const string EncryptionKey = "YourSecretKey123!@#";
    private const string EncryptionIV = "InitializationV";
    private const string SAVE_FILE_NAME = "gameData.dat";

    [MenuItem("Tools/FruitsConnect/Clear Building Base Data")]
    public static void ShowWindow()
    {
        GetWindow<BaseBuildingDataClearTool>("Clear Building Base Data");
    }

    private void OnGUI()
    {
        GUILayout.Label("Clear Building Base Save Data", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This will reset the building base progress data:\n" +
            "- Base Level (reset to 0)\n" +
            "- Base Stage Progress (reset to 0)\n\n" +
            "All other game data (levels, coins, energy spheres, etc.) will remain unchanged.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // Display current values if available
        string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        if (File.Exists(filePath))
        {
            try
            {
                SaveData currentData = LoadSaveData();
                if (currentData != null)
                {
                    EditorGUILayout.LabelField("Current Base Level:", currentData.BaseLevel.ToString());
                    EditorGUILayout.LabelField("Current Base Stage Progress:", currentData.BaseStageProgress.ToString());
                    EditorGUILayout.Space();
                }
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error reading save data: {e.Message}", MessageType.Error);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No save data file found.", MessageType.Warning);
        }

        EditorGUILayout.Space();

        GUI.enabled = File.Exists(filePath);
        if (GUILayout.Button("Clear Building Base Data"))
        {
            if (EditorUtility.DisplayDialog(
                "Clear Building Base Data",
                "Are you sure you want to reset the building base progress?\n\n" +
                "This will set Base Level and Base Stage Progress to 0.\n" +
                "All other game data will remain unchanged.",
                "Yes, Clear Base Data",
                "Cancel"
            ))
            {
                ClearBaseBuildingData();
            }
        }
        GUI.enabled = true;
    }

    private void ClearBaseBuildingData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        
        if (!File.Exists(filePath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "No save data file found. Nothing to clear.",
                "OK"
            );
            return;
        }

        try
        {
            // Load current save data
            SaveData saveData = LoadSaveData();
            
            if (saveData == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Failed to load save data. The file may be corrupted.",
                    "OK"
                );
                return;
            }

            // Store old values for logging
            int oldBaseLevel = saveData.BaseLevel;
            int oldBaseStageProgress = saveData.BaseStageProgress;

            // Reset building base data
            saveData.BaseLevel = 0;
            saveData.BaseStageProgress = 0;

            // Save the modified data
            SaveSaveData(saveData);

            Debug.Log($"[BaseBuildingDataClearTool] Building base data cleared. " +
                     $"BaseLevel: {oldBaseLevel} -> 0, BaseStageProgress: {oldBaseStageProgress} -> 0");

            EditorUtility.DisplayDialog(
                "Success",
                $"Building base data has been cleared successfully.\n\n" +
                $"Base Level: {oldBaseLevel} -> 0\n" +
                $"Base Stage Progress: {oldBaseStageProgress} -> 0",
                "OK"
            );

            // Refresh the window
            Repaint();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BaseBuildingDataClearTool] Error clearing building base data: {e.Message}");
            EditorUtility.DisplayDialog(
                "Error",
                $"Failed to clear building base data:\n{e.Message}",
                "OK"
            );
        }
    }

    private SaveData LoadSaveData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            string encryptedData = File.ReadAllText(filePath);
            string decryptedData = Decrypt(encryptedData);
            SaveData saveData = JsonConvert.DeserializeObject<SaveData>(decryptedData);
            return saveData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BaseBuildingDataClearTool] Error loading save data: {e.Message}");
            return null;
        }
    }

    private void SaveSaveData(SaveData saveData)
    {
        string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        
        try
        {
            string serializedData = JsonConvert.SerializeObject(saveData);
            string encryptedData = Encrypt(serializedData);
            File.WriteAllText(filePath, encryptedData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BaseBuildingDataClearTool] Error saving save data: {e.Message}");
            throw;
        }
    }

    private string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
            aes.IV = Encoding.UTF8.GetBytes(EncryptionIV.PadRight(16).Substring(0, 16));

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    private string Decrypt(string cipherText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
            aes.IV = Encoding.UTF8.GetBytes(EncryptionIV.PadRight(16).Substring(0, 16));

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }
}
