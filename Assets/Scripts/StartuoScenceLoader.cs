using UnityEngine;
using UnityEngine.SceneManagement;

public class StartuoScenceLoader : MonoBehaviour
{
    public string sceneName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
     async void Start()
    {
        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive); 
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
