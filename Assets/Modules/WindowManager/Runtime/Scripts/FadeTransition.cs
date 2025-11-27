using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace WindowManager
{
    public class FadeTransition : MonoBehaviour
    {
        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip transitionSound;

        private void Awake()
        {
            if (fadeImage == null)
            {
                fadeImage = GetComponent<Image>();
                if (fadeImage == null)
                {
                    Debug.LogError("FadeTransition requires an Image component!");
                }
            }

            // Set initial state
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.gameObject.SetActive(false);
        }

        public IEnumerator FadeIn()
        {
            Debug.Log("FadeIn");
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0);

            if (audioSource != null && transitionSound != null)
            {
                audioSource.PlayOneShot(transitionSound);
            }

            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);
                fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            fadeImage.color = new Color(0, 0, 0, 1);
        }

        public IEnumerator FadeOut()
        {
            Debug.Log("FadeOut");
            fadeImage.color = new Color(0, 0, 0, 1);

            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
                fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.gameObject.SetActive(false);
        }
    }
} 