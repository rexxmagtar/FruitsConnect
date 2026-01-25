using UnityEngine;
using DG.Tweening;

public class MoneyFlyEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float floatDistance = 3f;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float fadeDelay = 0.2f;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 360, 0);

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Play()
    {
        // Fly up
        transform.DOMoveY(transform.position.y + floatDistance, duration)
            .SetEase(Ease.OutQuad);

        // Rotate for a bit of flair
        transform.DORotate(transform.eulerAngles + rotationSpeed, duration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear);

        // Fade out
        if (spriteRenderer != null)
        {
            spriteRenderer.DOFade(0, duration - fadeDelay)
                .SetDelay(fadeDelay)
                .OnComplete(() => Destroy(gameObject));
        }
        else
        {
            // Fallback if no sprite renderer
            Destroy(gameObject, duration);
        }
    }
}
