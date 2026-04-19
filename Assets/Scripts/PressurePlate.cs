using UnityEngine;
using System.Collections;

/// <summary>
/// Üstüne basınca bağlı kapıyı/engeli açar.
/// Ağırlık kalkarsa kapanır (opsiyonel).
/// </summary>
public class PressurePlate : MonoBehaviour
{
    [Header("Target")]
    public GameObject[] objectsToToggle;  // Açılacak/kapanacak objeler
    public bool stayOpen = false;          // Bir kez basıldı mı sonsuza açık kalsın?
    public float closeDelay = 2.0f;        // Kapının kapanması için bekleme süresi

    [Header("Visual Feedback")]
    public SpriteRenderer plateRenderer;
    public Color pressedColor = new Color(0.8f, 0.3f, 0.1f);
    public Color releasedColor = new Color(0.6f, 0.5f, 0.2f);
    public AudioClip pressSound;
    public AudioClip releaseSound;

    private bool isPressed = false;
    private bool activated = false;
    private Color originalColor;
    private Coroutine releaseCoroutine;

    private void Start()
    {
        if (plateRenderer == null) plateRenderer = GetComponent<SpriteRenderer>();
        if (plateRenderer != null) {
            originalColor = releasedColor;
            plateRenderer.color = releasedColor;
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (isPressed) return;
        if (col.gameObject.CompareTag("Player") || col.gameObject.CompareTag("Box") || col.gameObject.name.Contains("Box"))
        {
            Press();
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (isPressed) return;
        if (col.CompareTag("Player") || col.CompareTag("Box") || col.name.Contains("Box"))
        {
            Press();
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (stayOpen) return;
        if (col.gameObject.CompareTag("Player") || col.gameObject.CompareTag("Box") || col.gameObject.name.Contains("Box"))
        {
            if (releaseCoroutine != null) StopCoroutine(releaseCoroutine);
            releaseCoroutine = StartCoroutine(DelayedRelease());
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (stayOpen) return;
        if (col.CompareTag("Player") || col.CompareTag("Box") || col.name.Contains("Box"))
        {
            if (releaseCoroutine != null) StopCoroutine(releaseCoroutine);
            releaseCoroutine = StartCoroutine(DelayedRelease());
        }
    }

    private void Press()
    {
        if (releaseCoroutine != null) StopCoroutine(releaseCoroutine);

        isPressed = true;
        if (plateRenderer != null) plateRenderer.color = pressedColor;
        if (pressSound != null) AudioSource.PlayClipAtPoint(pressSound, transform.position);

        // Hedef objeleri aç/kapat
        foreach (var obj in objectsToToggle)
            if (obj != null) obj.SetActive(false); // Kapıyı gizle = geçit açıldı

        if (!activated)
        {
            activated = true;
            HintDisplay.Instance?.Show("Kapı açıldı!", "Hızlı geç — çok sürmeyecek.", Color.white, 20, 3f, 0.5f, 0.8f);
        }
    }

    private IEnumerator DelayedRelease()
    {
        yield return new WaitForSeconds(closeDelay);
        Release();
    }

    private void Release()
    {
        isPressed = false;
        if (plateRenderer != null) plateRenderer.color = releasedColor;
        if (releaseSound != null) AudioSource.PlayClipAtPoint(releaseSound, transform.position);

        foreach (var obj in objectsToToggle)
            if (obj != null) obj.SetActive(true); // Kapıyı geri getir
    }
}
