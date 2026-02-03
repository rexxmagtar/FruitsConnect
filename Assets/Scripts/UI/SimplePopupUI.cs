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
    
    private bool isVisible = false;

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
    
    /// <summary>
    /// Set the info container reference (zone that should NOT close the popup when clicked)
    /// </summary>
    public void SetInfoContainer(GameObject container)
    {
        infoContainer = container;
    }

    private void Update()
    {
        if (!isVisible) return;
        
        // Handle tap to close (tap outside info container)
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            // Get pointer position
            Vector2 pointerPos = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            
            // Check if tap is over UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Check if tap is on the info container (don't close if it is)
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = pointerPos;
                
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);
                
                // Check if we hit the info container or its children
                bool hitInfoContainer = false;
                if (infoContainer != null)
                {
                    foreach (var result in results)
                    {
                        if (result.gameObject.transform.IsChildOf(infoContainer.transform) || result.gameObject == infoContainer)
                        {
                            hitInfoContainer = true;
                            break;
                        }
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
