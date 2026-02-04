using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Simple popup UI that handles closing on click outside or on close button
/// </summary>
public class SimplePopupUI : MonoBehaviour
{
    [Header("Popup References")]
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject infoContainer; // Zone that should NOT close the popup when clicked
    [SerializeField] private float clickGuardDuration = 0.05f; // Ignore the pointer down that triggered the show
    
    private bool isVisible = false;
    private float ignorePointerUntilTime = 0f;

    private void Awake()
    {
        SetupCloseButton();
        
        // Initially hide
        gameObject.SetActive(false);
    }
    
    private void SetupCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
    }
    
    /// <summary>
    /// Set the close button reference (useful when component is added dynamically)
    /// </summary>
    public void SetCloseButton(Button button)
    {
        closeButton = button;
        SetupCloseButton();
    }
    

    private void Update()
    {
        if (!isVisible) return;
        
        // Handle tap to close (tap outside info container)
        if (Time.unscaledTime < ignorePointerUntilTime) return;
        
        bool pointerDown = Input.GetMouseButtonDown(0);
        bool touchDown = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        
        if (pointerDown || touchDown)
        {
            // Get pointer position
            Touch touch = touchDown ? Input.GetTouch(0) : default;
            Vector2 pointerPos = touchDown ? touch.position : (Vector2)Input.mousePosition;
            
            // Check if tap is over UI
            if (EventSystem.current != null && (touchDown ? EventSystem.current.IsPointerOverGameObject(touch.fingerId) : EventSystem.current.IsPointerOverGameObject()))
            {
                // Check if tap is on the info container (don't close if it is)
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = pointerPos;
                
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);
                
                // Check if we hit the info container or its children
                bool hitInfoContainer = false;
                GameObject safeContainer = infoContainer != null ? infoContainer : gameObject;
                foreach (var result in results)
                {
                    if (result.gameObject.transform.IsChildOf(safeContainer.transform) || result.gameObject == safeContainer)
                    {
                        hitInfoContainer = true;
                        break;
                    }
                }
                
                // If tap is NOT on info container, close the popup
                if (!hitInfoContainer)
                {
                    // Check if tap is on the popup itself or its children
                    bool hitPopup = false;
                    foreach (var result in results)
                    {
                        if (result.gameObject.transform.IsChildOf(transform) || result.gameObject == gameObject)
                        {
                            hitPopup = true;
                            break;
                        }
                    }
                    
                    // Close if tap is on popup but not on info container
                    if (hitPopup)
                    {
                        Close();
                    }
                }
            }
            else
            {
                // Tap is not on any UI, close the popup
                Close();
            }
        }
    }

    public void Show()
    {
        isVisible = true;
        ignorePointerUntilTime = Time.unscaledTime + clickGuardDuration;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        isVisible = false;
        gameObject.SetActive(false);
    }

    public bool IsVisible()
    {
        return isVisible;
    }
}
