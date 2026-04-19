using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

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
    private Light2D magicLight; // Mor ışık efekti
    private float lightIntensity = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        magicLight = GetComponent<Light2D>();

        // Light yoksa oluştur
        if (magicLight == null)
        {
            magicLight = gameObject.AddComponent<Light2D>();
        }

        // Light ayarları
        magicLight.color = new Color(1f, 0f, 1f); // Mor
        magicLight.intensity = 0f; // Başta kapalı
        magicLight.pointLightInnerRadius = 0.5f;
        magicLight.pointLightOuterRadius = 3f;

        if (rb != null)
        {
            // ÖNEMLI: Hızlı mermiler için Collision Detection ayarla
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.gravityScale = 0f; // Yerçekimine direnç
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Dönüş engelle
            rb.linearVelocity = transform.right * speed;
        }

        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Light fade out (çarpışma sonrası)
        if (hasHit && magicLight != null)
        {
            lightIntensity -= Time.deltaTime * 3f; // Hızlı fade
            magicLight.intensity = Mathf.Max(0f, lightIntensity);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        // Düşman mı kontrol et
        if (collision.CompareTag("Enemy") || collision.GetComponent<IHealth>() != null)
        {
            OnEnemyHit(collision);
        }
        // Gizli duvar mı kontrol et
        else if (collision.GetComponent<SecretWall>() != null)
        {
            collision.GetComponent<SecretWall>().OnHitByMagic();
            
            // Çarpışma efekti ve yok olma
            hasHit = true;
            if (impactVFXPrefab != null) Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
            if (hitSFX != null && audioSource != null) audioSource.PlayOneShot(hitSFX);
            Destroy(gameObject, 0.1f);
        }
    }

    private void OnEnemyHit(Collider2D enemyCollider)
    {
        hasHit = true;
        Debug.Log($"💥 Büyü {enemyCollider.gameObject.name} ile çarpıştı!");

        Vector2 knockbackDirection = (enemyCollider.transform.position - transform.position).normalized;
        if (knockbackDirection.sqrMagnitude < 0.0001f)
        {
            knockbackDirection = transform.right;
        }

        // Düşmanın konumuna ışık yerleştir
        transform.position = enemyCollider.transform.position;

        // Mor ışık patlaması
        if (magicLight != null)
        {
            lightIntensity = 2f; // Başlangıç yoğunluğu (parlak)
            magicLight.intensity = lightIntensity;
        }

        // Deal damage
        var enemyHealth = enemyCollider.GetComponent<IHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            
            // Ekran vignette yapmıyoruz - düşmanın etrafında zaten mor ışık var
            
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

        // Destroy projectile after light fades
        Destroy(gameObject, 0.5f);
    }
}