using UnityEngine;
using System.Collections;

/// <summary>
/// Professional Magic Projectile System with damage, knockback, and VFX
/// </summary>
public class MagicBullet : MonoBehaviour
{
    [Header("═══ MOVEMENT ═══")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 3f;

    [Header("═══ DAMAGE ═══")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float knockbackForce = 8f;

    [Header("═══ VFX & SFX ═══")]
    [SerializeField] private GameObject impactVFXPrefab;
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private bool destroyOnHit = true;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private bool hasHit = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        if (rb != null)
        {
            rb.linearVelocity = transform.right * speed;
        }

        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        // Check if hit an enemy
        if (collision.CompareTag("Enemy"))
        {
            OnEnemyHit(collision);
        }
    }

    private void OnEnemyHit(Collider2D enemyCollider)
    {
        hasHit = true;
        Debug.Log($"💥 Büyü {enemyCollider.gameObject.name} ile çarpıştı!");

        // Deal damage
        var enemyHealth = enemyCollider.GetComponent<IHealth>();
        if (enemyHealth != null)
        {
            // Calculate knockback direction
            Vector2 knockbackDirection = (enemyCollider.transform.position - transform.position).normalized;
            enemyHealth.TakeDamage(damage);
            
            // Apply knockback
            var rb = enemyCollider.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            }
        }

        // Play impact VFX
        if (impactVFXPrefab != null)
        {
            Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
        }

        // Play impact sound
        if (hitSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSFX);
        }

        // Destroy projectile
        if (destroyOnHit)
        {
            Destroy(gameObject, 0.1f);
        }
    }
}