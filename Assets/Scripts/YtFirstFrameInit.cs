using UnityEngine;
using YTGameSDK;


public class YtFirstFrameInit : MonoBehaviour
{

    void Awake()
    {
        Debug.Log($"YtFirstFrameInit Awake Frame{Time.frameCount}");
        FindFirstObjectByType<YTGameWrapper>().SendGameFirstFrameReady();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
