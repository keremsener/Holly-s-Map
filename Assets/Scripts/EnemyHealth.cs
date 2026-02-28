using UnityEngine;
using System.Collections;

/// <summary>
/// Professional Enemy Health System with damage, knockback, and visual feedback
/// Integrates with EnemyAI state machine
/// </summary>
public class EnemyHealth : MonoBehaviour, IHealth
{
    [Header("═══ HEALTH SETTINGS ═══")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathDelay = 0.5f;

    [Header("═══ VISUAL FEEDBACK ═══")]
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color deathColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("═══ KNOCKBACK SETTINGS ═══")]
    [SerializeField] private float knockbackDecay = 0.95f;

    [Header("═══ VFX & SFX ═══")]
    [SerializeField] private GameObject damageVFXPrefab;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private AudioClip damageSFX;
    [SerializeField] private AudioClip deathSFX;

    private float currentHealth;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Animator animator;
    private Color originalColor;
    private bool isDead = false;
    private EnemyAI enemyAI;

    // Animator hash
    private int hashHurt;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        enemyAI = GetComponent<EnemyAI>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Cache animator hash
        if (animator != null)
        {
            hashHurt = Animator.StringToHash("Hurt");
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Takes damage and applies knockback (IHealth implementation)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"⚡ {gameObject.name} hasar aldı! -{damage} HP | Kalan: {currentHealth:F0}/{maxHealth}");

        // Play hurt animation
        if (animator != null)
        {
            animator.SetTrigger(hashHurt);
        }

        // Visual feedback
        StartCoroutine(DamageFlash());

        // Play sound
        PlaySound(damageSFX);

        // Spawn VFX
        if (damageVFXPrefab != null)
        {
            Instantiate(damageVFXPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        // Check death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Takes damage with custom knockback direction and force
    /// </summary>
    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
    {
        TakeDamage(damage);

        if (!isDead && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        }
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"💀 {gameObject.name} öldü!");

        // Disable AI behavior
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        // Play death sound
        PlaySound(deathSFX);

        // Death VFX
        if (deathVFXPrefab != null)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        }

        // Visual change (fade/color)
        if (spriteRenderer != null)
        {
            StartCoroutine(FadeOut());
        }

        // Destroy or disable
        if (destroyOnDeath)
        {
            Destroy(gameObject, deathDelay);
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < deathDelay && spriteRenderer != null)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / deathDelay);
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
            yield return null;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public float GetHealthPercent() => currentHealth / maxHealth;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
}