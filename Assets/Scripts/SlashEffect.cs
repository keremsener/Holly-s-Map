using UnityEngine;
using System.Collections;

/// <summary>
/// Professional Slash Effect with fade animation and optional sound
/// </summary>
public class SlashEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.25f;
    [SerializeField] private bool animateFade = true;
    [SerializeField] private bool animateScale = true;
    [SerializeField] private float scaleMultiplier = 0.8f;

    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        if (animateFade || animateScale)
        {
            StartCoroutine(AnimateAndDestroy());
        }
        else
        {
            Destroy(gameObject, lifetime);
        }
    }

    private IEnumerator AnimateAndDestroy()
    {
        float elapsed = 0f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / lifetime;

            // Fade out
            if (animateFade && spriteRenderer != null)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(1f, 0f, progress);
                spriteRenderer.color = color;
            }

            // Scale animation
            if (animateScale)
            {
                float scale = Mathf.Lerp(1f, scaleMultiplier, progress);
                transform.localScale = originalScale * scale;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}