using UnityEngine;

/// <summary>
/// Sahneye koyulan checkpoint (kayıt noktası) tetikleyici.
/// Oyuncu bu collider'a girince respawn noktası burası olur.
/// Inspector'dan Trigger Collider2D'yi aktif etmeyi unutma.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Görsel")]
    [SerializeField] private SpriteRenderer flagSprite;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color activeColor   = Color.yellow;
    
    [Header("Ses")]
    [SerializeField] private AudioClip successSound;

    private bool activated = false;

    private void Start()
    {
        // Collider'ı trigger'a çevir
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        if (flagSprite != null)
            flagSprite.color = inactiveColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;
        if (!other.CompareTag("Player")) return;

        activated = true;

        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.SetCheckpoint(transform.position);

        if (flagSprite != null)
            flagSprite.color = activeColor;

        if (successSound != null)
            AudioSource.PlayClipAtPoint(successSound, transform.position);

        Debug.Log($"📍 Checkpoint aktif: {transform.position}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = activated ? Color.yellow : new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 1.5f, 0f));
        Gizmos.DrawIcon(transform.position + Vector3.up * 1f, "sv_icon_dot0_pix16_gizmo", true);
    }
}
