using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticServices
{
    public class HapticController : MonoBehaviour
    {
        [Header("Haptic Settings")]
        [SerializeField] private float leftMotorSpeed = 0.5f;
        [SerializeField] private float rightMotorSpeed = 0.5f;
        [SerializeField] private float singleVibrationDuration = 0.1f;
        [SerializeField] private int mobileVibrationAmplitude = 128; // 1-255 range for mobile vibration
        
        private static HapticController instance;
        private Gamepad gamepad;
        private bool isHapticsSupported = false;
        private bool isVibrating = false;
        private bool isMobilePlatform = false;

        public bool IsVibrationEnabled;
        
        public static HapticController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<HapticController>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("HapticController");
                        instance = go.AddComponent<HapticController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeHaptics();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeHaptics()
        {
            // Check platform
            isMobilePlatform = Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
            
            if (isMobilePlatform)
            {
                // For mobile platforms, use Android vibrations (works on both Android and iOS)
                isHapticsSupported = true;
                Debug.Log("Mobile platform detected - Using Android vibration system");
            }
            else
            {
                // For desktop/console platforms, check for gamepad
                gamepad = Gamepad.current;
                isHapticsSupported = gamepad != null;
                
                if (isHapticsSupported)
                {
                    Debug.Log("Haptics supported - Gamepad detected");
                }
                else
                {
                    Debug.Log("Haptics not supported - No gamepad detected");
                }
            }
        }
        
        /// <summary>
        /// Starts continuous vibration
        /// </summary>
        public void StartVibration()
        {
            if (!IsVibrationEnabled || isVibrating)
                return;
                
            isVibrating = true;
            
            if (isMobilePlatform)
            {
                // Use Android vibration for mobile - infinite duration
                Vibration.Vibrate(999999, mobileVibrationAmplitude, true); // Very long duration for "infinite" effect
            }
            else if (isHapticsSupported && gamepad != null)
            {
                // Use Input System haptics with dual motor support for gamepad
                gamepad.SetMotorSpeeds(leftMotorSpeed, rightMotorSpeed);
            }
        }
        
        /// <summary>
        /// Stops continuous vibration
        /// </summary>
        public void StopVibration()
        {
            if (!isVibrating)
                return;
                
            isVibrating = false;
            
            if (isMobilePlatform)
            {
                // Cancel Android vibration
                Vibration.Cancel();
            }
            else if (isHapticsSupported && gamepad != null)
            {
                gamepad.SetMotorSpeeds(0f, 0f);
            }
        }
        
        /// <summary>
        /// Plays a single vibration pulse
        /// </summary>
        public void PlaySingleVibration()
        {
            if (!IsVibrationEnabled)
                return;
                
            if (isMobilePlatform)
            {
                // Use Android vibration for mobile - convert duration to milliseconds
                long durationMs = (long)(singleVibrationDuration * 1000);
                Vibration.Vibrate(durationMs, mobileVibrationAmplitude, true);
            }
            else if (isHapticsSupported && gamepad != null)
            {
                // Use Input System haptics with dual motor support
                gamepad.SetMotorSpeeds(leftMotorSpeed, rightMotorSpeed);
                
                // Stop haptics after duration
                StartCoroutine(StopSingleVibrationAfterDelay(singleVibrationDuration));
            }
        }
        
        /// <summary>
        /// Plays a predefined vibration effect (mobile only)
        /// </summary>
        /// <param name="effectType">Type of predefined effect</param>
        public void PlayPredefinedVibration(PredefinedEffectType effectType)
        {
            if (!IsVibrationEnabled || !isMobilePlatform)
                return;
                
            int effectId = GetPredefinedEffectId(effectType);
            if (effectId != -1)
            {
                Vibration.VibratePredefined(effectId, true);
            }
        }
        
        /// <summary>
        /// Plays a custom vibration pattern (mobile only)
        /// </summary>
        /// <param name="pattern">Pattern of durations in milliseconds (Off-On-Off-On format)</param>
        /// <param name="amplitudes">Optional amplitudes array (1-255 range)</param>
        /// <param name="repeat">Repeat index or -1 to disable repeating</param>
        public void PlayVibrationPattern(long[] pattern, int[] amplitudes = null, int repeat = -1)
        {
            if (!IsVibrationEnabled || !isMobilePlatform)
                return;
                
            Vibration.Vibrate(pattern, amplitudes, repeat, true);
        }
        
        
        /// <summary>
        /// Stops single vibration after a specified delay
        /// </summary>
        /// <param name="delay">Delay in seconds</param>
        private System.Collections.IEnumerator StopSingleVibrationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (isHapticsSupported && gamepad != null)
            {
                gamepad.SetMotorSpeeds(0f, 0f);
            }
        }
        
        /// <summary>
        /// Gets the predefined effect ID based on effect type
        /// </summary>
        /// <param name="effectType">Type of predefined effect</param>
        /// <returns>Effect ID or -1 if not supported</returns>
        private int GetPredefinedEffectId(PredefinedEffectType effectType)
        {
            switch (effectType)
            {
                case PredefinedEffectType.Click:
                    return Vibration.PredefinedEffect.EFFECT_CLICK;
                case PredefinedEffectType.DoubleClick:
                    return Vibration.PredefinedEffect.EFFECT_DOUBLE_CLICK;
                case PredefinedEffectType.HeavyClick:
                    return Vibration.PredefinedEffect.EFFECT_HEAVY_CLICK;
                case PredefinedEffectType.Tick:
                    return Vibration.PredefinedEffect.EFFECT_TICK;
                default:
                    return -1;
            }
        }
        
        /// <summary>
        /// Checks if the device supports vibration
        /// </summary>
        /// <returns>True if vibration is supported</returns>
        public bool IsVibrationSupported()
        {
            if (isMobilePlatform)
            {
                return Vibration.HasVibrator();
            }
            else
            {
                return isHapticsSupported;
            }
        }
        
        /// <summary>
        /// Gets the current platform type
        /// </summary>
        /// <returns>Platform type</returns>
        public PlatformType GetPlatformType()
        {
            return isMobilePlatform ? PlatformType.Mobile : PlatformType.Desktop;
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                StopVibration();
            }
        }
        
        /// <summary>
        /// Predefined effect types for mobile vibration
        /// </summary>
        public enum PredefinedEffectType
        {
            Click,
            DoubleClick,
            HeavyClick,
            Tick
        }
        
        /// <summary>
        /// Platform types
        /// </summary>
        public enum PlatformType
        {
            Mobile,
            Desktop
        }
    }
}