using UnityEngine;

namespace WindowManager
{
    public abstract class BaseUIWithSound : BaseUI
    {
        [Header("UI Sounds")]
        [SerializeField] protected AudioClip buttonClickSound;
        
       [SerializeField] protected AudioSource audioSource;
        
        protected virtual void Awake()
        {

        }
        
        protected void PlayButtonClick()
        {

            audioSource.PlayOneShot(buttonClickSound);
            
        }
    }
} 