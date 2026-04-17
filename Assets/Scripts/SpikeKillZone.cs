using UnityEngine;

/// <summary>
/// Diken (spike/trap) ölüm bölgesi.
/// Bu component'i sahneye konan spike GameObject'lerine ekle.
/// Oyuncu temas edince anında öldürür.
/// </summary>
public class SpikeKillZone : MonoBehaviour
{
    [Header("Spike Ayarlari")]
    [Tooltip("Tek vurusta mi oldursun? False ise hasar verir.")]
    [SerializeField] private bool instakill = true;

    [SerializeField] private float damage = 9999f;

    [Header("Geri Bildirim")]
    [SerializeField] private bool shakeCamera = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        if (shakeCamera && CameraEffects.Instance != null)
            CameraEffects.Instance.MeleeHitEffect();

        float dmg = instakill ? 9999f : damage;
        health.TakeDamage(dmg);

        Debug.Log($"🌵 Oyuncu dikene carpti! {dmg} hasar.");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        var health = collision.gameObject.GetComponent<PlayerHealth>();
        if (health == null) return;

        if (shakeCamera && CameraEffects.Instance != null)
            CameraEffects.Instance.MeleeHitEffect();

        float dmg = instakill ? 9999f : damage;
        health.TakeDamage(dmg);

        Debug.Log($"🌵 Oyuncu dikene carpti (collision)! {dmg} hasar.");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}
