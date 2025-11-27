using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;


namespace DataRepository
{
    // Custom contract resolver to ensure properties are always serialized in alphabetical order
    public class OrderedContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            return properties.OrderBy(p => p.PropertyName).ToList();
        }
    }


public class ProgressSaveManager<T> : MonoBehaviour where T : new()
{
    private static ProgressSaveManager<T> _instance;
    public static ProgressSaveManager<T> Instance => _instance;

    private const string EncryptionKey = "YourSecretKey123!@#"; // This should be a secure key in production
    private const string EncryptionIV = "InitializationV"; // This should be a secure IV in production
    private T _gameData;
    private const string SAVE_FILE_NAME = "gameData.dat";

    private const string LAST_CLOUD_DATA_HASH_KEY = "lastCloudDataHash.dat";
    private const string TUTORIAL_COMPLETED_KEY = "TutorialCompleted";
    private FirebaseFirestore _firestore;

    public T GetGameData() => _gameData;

    public bool HasData => _gameData != null;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        InitializeFirestore();
        LoadGameData();
    }

    private void InitializeFirestore()
    {
        _firestore = FirebaseFirestore.DefaultInstance;
    }

    private string CalculateDataHash<T>(T data)
    {
        using (var sha256 = SHA256.Create())
        {
            // Create a copy of the data without the hash field for consistent hashing
            var dataForHash = data;


            var json = JsonConvert.SerializeObject(dataForHash);

            Debug.Log("json: " + json);
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));

            var result = Convert.ToBase64String(hashBytes);
            Debug.Log("result: " + result);
            return result;
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

    public void CreateNewSaveData()
    {
        _gameData = new T();
        SaveGameData();
    }

    private void LoadGameData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        
        if (File.Exists(filePath))
        {
            try
            {
                string encryptedData = File.ReadAllText(filePath);
                string decryptedData = Decrypt(encryptedData);
                _gameData = JsonConvert.DeserializeObject<T>(decryptedData);
                Debug.Log("Game data loaded: " + _gameData);
                Debug.Log("Game data loaded: " + _gameData.GetType());
                Debug.Log("Game data loaded: " + _gameData.ToString());
                Debug.Log("Game data loaded: " + _gameData.GetHashCode());
                Debug.Log("Game data loaded: " + _gameData.GetHashCode());
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading game data: {e.Message}");
            }
        }
        else
        {
            Debug.Log("No game data found");
        }
    }


    public void SaveGameData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        
        try
        {
            string serializedData = JsonConvert.SerializeObject(_gameData);
            string encryptedData = Encrypt(serializedData);
            File.WriteAllText(filePath, encryptedData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving game data: {e.Message}");
        }
    }

    public void ClearAllSavedData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        string lastCloudDataHashPath = Path.Combine(Application.persistentDataPath, LAST_CLOUD_DATA_HASH_KEY);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        if (File.Exists(lastCloudDataHashPath))
        {
            File.Delete(lastCloudDataHashPath);
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("All saved data has been cleared");
    }

    public bool IsTutorialCompleted()
    {
        return PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
    }

    public void SaveTutorialCompleted(bool completed)
    {
        PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, completed ? 1 : 0);
        PlayerPrefs.Save();
    }

    private bool isInSync = false;

    public async Task<bool> SyncWithCloud(bool forceSync = false)
    {
        if(isInSync)
        {
            return false;
        }
        isInSync = true;
        var authManager = AuthServices.FirebaseAuthManager.Instance;
        if (!authManager.IsAuthenticated)
        {
            await authManager.Auth();
            if (!authManager.IsAuthenticated)
            {
                Debug.LogWarning("Cannot sync: User is not authenticated");
                isInSync = false;
                return false;
            }
        }

        try
        {
            string userId = authManager.CurrentUser.UserId;
            Debug.Log("Syncing with cloud: " + userId);
            DocumentReference docRef = _firestore.Collection("user_data").Document(userId);

            string lastCloudDataHash = "";
            if (File.Exists(Path.Combine(Application.persistentDataPath, LAST_CLOUD_DATA_HASH_KEY)))
            {
                lastCloudDataHash = File.ReadAllText(Path.Combine(Application.persistentDataPath, LAST_CLOUD_DATA_HASH_KEY));
            }

            if (forceSync)
            {
                await docRef.SetAsync(_gameData);
                File.WriteAllText(Path.Combine(Application.persistentDataPath, LAST_CLOUD_DATA_HASH_KEY), CalculateDataHash(_gameData));

                Debug.Log("Force sync upload to cloud");
                return true;
            }

            // Get cloud data
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            
            if (snapshot.Exists)
            {
                // Cloud data exists, compare hashes
                var cloudData = snapshot.ConvertTo<T>();
                string cloudDataHash = CalculateDataHash(cloudData);
                string lastCloudHash = lastCloudDataHash;

                Debug.Log("lastCloudHash: " + lastCloudHash);
                Debug.Log("cloudDataHash: " + cloudDataHash);

                if (string.IsNullOrEmpty(lastCloudHash) || !cloudDataHash.Equals(lastCloudHash))
                {
                    // Cloud data has changed since our last sync, update local data
                    _gameData = cloudData;
                        File.WriteAllText(Path.Combine(Application.persistentDataPath, LAST_CLOUD_DATA_HASH_KEY), cloudDataHash);
                        SaveGameData();
                    Debug.Log("Local data updated from cloud new Hash: " + cloudDataHash);
                }
                else
                {
                    if (!cloudDataHash.Equals(CalculateDataHash(_gameData)))
                    {
                        var newHash = CalculateDataHash(_gameData);
                        // Cloud data hasn't changed, update cloud with our local data
                       
                        await docRef.SetAsync(_gameData);
                        File.WriteAllText(Path.Combine(Application.persistentDataPath, LAST_CLOUD_DATA_HASH_KEY), newHash);
                         
                         
    
                        Debug.Log("Cloud data updated from local.new Hash: " + newHash);
                    }
                    else
                    {
                        Debug.Log("Cloud data is up to date");
                    }
                }
            }
            else
            {
                var newHash = CalculateDataHash(_gameData);
               
                // No cloud data, upload local data
                await docRef.SetAsync(_gameData);
                File.WriteAllText(Path.Combine(Application.persistentDataPath, LAST_CLOUD_DATA_HASH_KEY), newHash);

                Debug.Log("Initial cloud data uploaded");
                Debug.Log("Initial cloud data uploaded new Hash: " + newHash);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error syncing with cloud: {e.Message}");
            return false;
        }
        finally
        {
            isInSync = false;
        }
    }

}
}