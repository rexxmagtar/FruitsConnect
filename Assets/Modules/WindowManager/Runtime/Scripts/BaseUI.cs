using UnityEngine;

namespace WindowManager
{
    public abstract class BaseUI : MonoBehaviour
{
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }
    
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
    
    public virtual void Initialize()
    {
        // Override in derived classes for specific initialization
    }
}
} 