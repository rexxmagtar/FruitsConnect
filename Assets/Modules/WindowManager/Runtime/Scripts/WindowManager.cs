using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;

namespace WindowManager
{
    /// <summary>
    /// Generic window manager for handling screen transitions and basic UI management.
    /// This is a reusable version without project-specific dependencies.
    /// </summary>
    public class WindowManager : MonoBehaviour
    {
        private static WindowManager _instance;
        public static WindowManager Instance => _instance;
        
        [Header("Transition Settings")]
        [SerializeField] private FadeTransition fadeTransition;
        [SerializeField] private float defaultTransitionDuration = 0.3f;
        
        private BaseUI currentScreen;
        private bool isTransitioning = false;
        
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
        
        /// <summary>
        /// Shows a screen with optional transition
        /// </summary>
        /// <param name="screen">The screen to show</param>
        /// <param name="useTransition">Whether to use fade transition</param>
        public async Task ShowScreen(BaseUI screen, bool useTransition = false)
        {
            if (isTransitioning) return;
            
            if (useTransition && fadeTransition != null)
            {
                await ShowScreenWithTransition(screen);
            }
            else
            {
                HideCurrentScreen();
                ShowScreenDirect(screen);
            }
        }
        
        /// <summary>
        /// Hides the current screen
        /// </summary>
        public void HideCurrentScreen()
        {
            if (currentScreen != null)
            {
                currentScreen.Hide();
                currentScreen = null;
            }
        }
        
        /// <summary>
        /// Shows a screen directly without transition
        /// </summary>
        /// <param name="screen">The screen to show</param>
        public void ShowScreenDirect(BaseUI screen)
        {
            if (screen != null)
            {
                currentScreen = screen;
                screen.Show();
            }
        }
        
        /// <summary>
        /// Shows a screen with fade transition
        /// </summary>
        /// <param name="screen">The screen to show</param>
        private async Task ShowScreenWithTransition(BaseUI screen)
        {
            if (isTransitioning) return;
            
            isTransitioning = true;
            
            try
            {
                // Fade in
                await CoroutineToTask(fadeTransition.FadeIn());
                
                // Hide current screen and show new one
                HideCurrentScreen();
                ShowScreenDirect(screen);
                
                // Fade out
                await CoroutineToTask(fadeTransition.FadeOut());
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error showing screen with transition: " + e.Message);
                HideCurrentScreen();
                ShowScreenDirect(screen);
            }
            finally
            {
                isTransitioning = false;
            }
        }
        
        /// <summary>
        /// Converts a coroutine to a task
        /// </summary>
        /// <param name="coroutine">The coroutine to convert</param>
        /// <returns>A task representing the coroutine</returns>
        private Task CoroutineToTask(IEnumerator coroutine)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(RunCoroutine(coroutine, tcs));
            return tcs.Task;
        }
        
        /// <summary>
        /// Runs a coroutine and completes the task when done
        /// </summary>
        /// <param name="coroutine">The coroutine to run</param>
        /// <param name="tcs">The task completion source</param>
        private IEnumerator RunCoroutine(IEnumerator coroutine, TaskCompletionSource<bool> tcs)
        {
            yield return coroutine;
            tcs.SetResult(true);
        }
        
        /// <summary>
        /// Gets the currently active screen
        /// </summary>
        /// <returns>The current screen or null if none</returns>
        public BaseUI GetCurrentScreen()
        {
            return currentScreen;
        }
        
        /// <summary>
        /// Checks if a transition is currently in progress
        /// </summary>
        /// <returns>True if transitioning, false otherwise</returns>
        public bool IsTransitioning()
        {
            return isTransitioning;
        }
        
        /// <summary>
        /// Sets the fade transition component
        /// </summary>
        /// <param name="transition">The fade transition to use</param>
        public void SetFadeTransition(FadeTransition transition)
        {
            fadeTransition = transition;
        }
        
        /// <summary>
        /// Sets the default transition duration
        /// </summary>
        /// <param name="duration">The duration in seconds</param>
        public void SetDefaultTransitionDuration(float duration)
        {
            defaultTransitionDuration = duration;
        }
    }
}

