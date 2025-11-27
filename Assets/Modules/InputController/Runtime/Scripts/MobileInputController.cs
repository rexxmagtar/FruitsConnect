using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace InputController
{
    public class MobileInputController : MonoBehaviour
    {
        // Singleton instance
        private static MobileInputController _instance;
        public static MobileInputController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MobileInputController>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MobileInputController");
                        _instance = go.AddComponent<MobileInputController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        [Header("Input Settings")]
        [SerializeField] private float maxTouchRangeY = 0.8f;
        [SerializeField] private float minTouchRangeY = 0.1f;
        [SerializeField] private float touchCooldown = 0.5f;
        
        [Header("Zoom Settings")]
        [SerializeField] private float touchZoomSensitivity = 0.5f;
        [SerializeField] private float mouseWheelSensitivity = 1f;
        
        [Header("Drag Settings")]
        [SerializeField] private float dragSensitivityMobile = 0.2f;
        [SerializeField] private float dragSensitivityDesktop = 1f;
        
        // Events
        public event Action OnTouchBegan;
        public event Action OnTouchEnded;
        public event Action OnTouchMoved;
        public event Action OnMouseButtonDown;
        public event Action OnMouseButtonUp;
        public event Action<float> OnZoomChanged;
        public event Action<Vector2> OnDragDelta;
        
        // Input state
        private bool isTouching = false;
        private bool isMousePressed = false;
        private Vector2 lastTouchPosition;
        private Vector2 lastMousePosition;
        private float lastInputTime;
        private float initialTouchDistance;
        private bool isZooming = false;
        
        // Input Actions
        private InputAction touchAction;
        private InputAction mouseAction;
        private InputAction mousePositionAction;
        private InputAction mouseScrollAction;
        
        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Create input actions
                CreateInputActions();
            }
            else if (_instance != this)
            {
                Debug.LogWarning("Multiple MobileInputController instances found. Destroying duplicate.");
                Destroy(gameObject);
            }
        }
        
        private void CreateInputActions()
        {
            // Touch input
            touchAction = new InputAction("Touch", InputActionType.Button);
            touchAction.AddBinding("<Touchscreen>/primaryTouch");
            touchAction.AddBinding("<Mouse>/leftButton");
            
            // Mouse position
            mousePositionAction = new InputAction("MousePosition", InputActionType.Value);
            mousePositionAction.AddBinding("<Mouse>/position");
            mousePositionAction.AddBinding("<Touchscreen>/primaryTouch/position");
            
            // Mouse scroll
            mouseScrollAction = new InputAction("MouseScroll", InputActionType.Value);
            mouseScrollAction.AddBinding("<Mouse>/scroll");
            
            // Enable actions
            touchAction.Enable();
            mousePositionAction.Enable();
            mouseScrollAction.Enable();
            
            // Subscribe to events
            touchAction.performed += OnTouchPerformed;
            touchAction.canceled += OnTouchCanceled;
            mouseScrollAction.performed += OnMouseScrollPerformed;
        }
        
        private void Update()
        {
            HandleTouchInput();
            HandleMouseInput();
            HandleZoomInput();
        }
        
        private void HandleTouchInput()
        {
            // Check for touch input
            if (Touchscreen.current != null)
            {
                var primaryTouch = Touchscreen.current.primaryTouch;
                bool isPressed = primaryTouch.press.ReadValue() > 0;
                
                if (isPressed)
                {
                    Vector2 touchPosition = primaryTouch.position.ReadValue();
                    var normalizedY = touchPosition.y / Screen.height;
                    
                    // Check if touch is within allowed range
                    if (normalizedY > maxTouchRangeY || normalizedY < minTouchRangeY)
                    {
                        return;
                    }
                    
                    if (!isTouching && Time.time - lastInputTime > touchCooldown)
                    {
                        isTouching = true;
                        lastTouchPosition = touchPosition;
                        lastInputTime = Time.time;
                        OnTouchBegan?.Invoke();
                    }
                    else if (isTouching)
                    {
                        Vector2 delta = touchPosition - lastTouchPosition;
                        if (delta.magnitude > 1f) // Threshold to avoid micro-movements
                        {
                            OnDragDelta?.Invoke(delta);
                            OnTouchMoved?.Invoke();
                        }
                        lastTouchPosition = touchPosition;
                    }
                }
                else if (isTouching)
                {
                    isTouching = false;
                    OnTouchEnded?.Invoke();
                }
            }
        }
        
        private void HandleMouseInput()
        {
            // Check for mouse input
            if (Mouse.current != null)
            {
                bool isPressed = Mouse.current.leftButton.ReadValue() > 0;
                
                if (isPressed)
                {
                    Vector2 mousePosition = Mouse.current.position.ReadValue();
                    var normalizedY = mousePosition.y / Screen.height;
                    
                    // Check if mouse is within allowed range
                    if (normalizedY > maxTouchRangeY || normalizedY < minTouchRangeY)
                    {
                        return;
                    }
                    
                    if (!isMousePressed && Time.time - lastInputTime > touchCooldown)
                    {
                        isMousePressed = true;
                        lastMousePosition = mousePosition;
                        lastInputTime = Time.time;
                        OnMouseButtonDown?.Invoke();
                    }
                    else if (isMousePressed)
                    {
                        Vector2 delta = mousePosition - lastMousePosition;
                        if (delta.magnitude > 1f) // Threshold to avoid micro-movements
                        {
                            OnDragDelta?.Invoke(delta);
                        }
                        lastMousePosition = mousePosition;
                    }
                }
                else if (isMousePressed)
                {
                    isMousePressed = false;
                    OnMouseButtonUp?.Invoke();
                }
            }
        }
        
        private void HandleZoomInput()
        {
            // Handle mouse wheel zoom
            if (Mouse.current != null)
            {
                Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
                if (scrollDelta.y != 0)
                {
                    float zoomDelta = -scrollDelta.y * mouseWheelSensitivity;
                    OnZoomChanged?.Invoke(zoomDelta);
                }
            }
            
            // Handle touch zoom (two finger pinch)
            if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                if (touches.Count == 2)
                {
                    var touch0 = touches[0];
                    var touch1 = touches[1];
                    
                    bool touch0Pressed = touch0.press.ReadValue() > 0;
                    bool touch1Pressed = touch1.press.ReadValue() > 0;
                    
                    if (touch0Pressed && touch1Pressed)
                    {
                        Vector2 pos0 = touch0.position.ReadValue();
                        Vector2 pos1 = touch1.position.ReadValue();
                        float currentDistance = Vector2.Distance(pos0, pos1);
                        
                        if (!isZooming)
                        {
                            initialTouchDistance = currentDistance;
                            isZooming = true;
                        }
                        else
                        {
                            float zoomDelta = (currentDistance - initialTouchDistance) * touchZoomSensitivity * Time.deltaTime;
                            OnZoomChanged?.Invoke(-zoomDelta);
                            initialTouchDistance = currentDistance;
                        }
                    }
                }
                else
                {
                    isZooming = false;
                }
            }
        }
        
        private void OnTouchPerformed(InputAction.CallbackContext context)
        {
            // This is handled in Update for more precise control
        }
        
        private void OnTouchCanceled(InputAction.CallbackContext context)
        {
            // This is handled in Update for more precise control
        }
        
        private void OnMouseScrollPerformed(InputAction.CallbackContext context)
        {
            // This is handled in Update for more precise control
        }
        
        // Public methods for external access
        public bool IsTouching => isTouching;
        public bool IsMousePressed => isMousePressed;
        public bool IsAnyInputActive => isTouching || isMousePressed;
        public Vector2 LastTouchPosition => lastTouchPosition;
        public Vector2 LastMousePosition => lastMousePosition;
        
        // Static convenience methods
        public static bool IsTouchingStatic => Instance.IsTouching;
        public static bool IsMousePressedStatic => Instance.IsMousePressed;
        public static bool IsAnyInputActiveStatic => Instance.IsAnyInputActive;
        public static Vector2 LastTouchPositionStatic => Instance.LastTouchPosition;
        public static Vector2 LastMousePositionStatic => Instance.LastMousePosition;
        
        // Settings
        public void SetTouchRange(float min, float max)
        {
            minTouchRangeY = min;
            maxTouchRangeY = max;
        }
        
        public void SetTouchCooldown(float cooldown)
        {
            touchCooldown = cooldown;
        }
        
        public void SetZoomSensitivity(float touchSensitivity, float mouseSensitivity)
        {
            touchZoomSensitivity = touchSensitivity;
            mouseWheelSensitivity = mouseSensitivity;
        }
        
        public void SetDragSensitivity(float mobileSensitivity, float desktopSensitivity)
        {
            dragSensitivityMobile = mobileSensitivity;
            dragSensitivityDesktop = desktopSensitivity;
        }
        
        private void OnDestroy()
        {
            // Clean up input actions
            if (touchAction != null)
            {
                touchAction.performed -= OnTouchPerformed;
                touchAction.canceled -= OnTouchCanceled;
                touchAction.Disable();
                touchAction.Dispose();
            }
            
            if (mousePositionAction != null)
            {
                mousePositionAction.Disable();
                mousePositionAction.Dispose();
            }
            
            if (mouseScrollAction != null)
            {
                mouseScrollAction.performed -= OnMouseScrollPerformed;
                mouseScrollAction.Disable();
                mouseScrollAction.Dispose();
            }
        }
    }
}
