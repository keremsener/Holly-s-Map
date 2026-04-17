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
    [SerializeField] private bool verboseLogs = false;

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

    [Header("═══ LIMB DISMEMBERMENT ═══")]
    [SerializeField] private bool enableLimbDismemberment = true;
    [SerializeField] private float dismembermentChancePerHit = 0.3f; // 30% şansı her hit'te
    [SerializeField] private int maxLimbsToLose = 4; // Maksimum kaç limb kaybedilebilir (2 arm + 2 leg)
    [SerializeField] private AudioClip limbLossedSFX;
    [SerializeField] private Transform[] manualLimbParts; // Inspector'da manuel olarak assign et

    private float currentHealth;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Animator animator;
    private Color originalColor;
    private bool isDead = false;
    private EnemyAI enemyAI;

    // Body parts tracking
    private Transform[] limbParts;
    private int limbsLost = 0;
    private int legsLost = 0; // Bacak sayısı
    private bool rightArmLost = false; // Sağ kol kopma durumu
    private bool leftArmLost = false; // Sol kol kopma durumu

    // Animator hash
    private int hashHurt;
    private int hashDying;

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
            hashDying = Animator.StringToHash("Dying");
        }

        // Find all limb parts (Left Arm, Right Arm, Left Leg, Right Leg)
        if (enableLimbDismemberment)
        {
            // Eğer manuel olarak assign edildiyse, onları kullan
            if (manualLimbParts != null && manualLimbParts.Length > 0)
            {
                limbParts = manualLimbParts;
                LogVerbose($"📋 {gameObject.name} için {limbParts.Length} limb manuel olarak assign edildi");
            }
            else
            {
                // Yoksa otomatik bul
                CollectLimbParts();
            }
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
        LogVerbose($"⚡ {gameObject.name} hasar aldı! -{damage} HP | Kalan: {currentHealth:F0}/{maxHealth}");

        // Play hurt animation
        if (animator != null)
        {
            animator.SetTrigger(hashHurt);
        }

        // Visual feedback
        StartCoroutine(DamageFlash());

        // Play sound
        PlaySound(damageSFX);

        // Camera shake effect (Enemy hit)
        if (CameraEffects.Instance != null)
        {
            CameraEffects.Instance.EnemyHitEffect();
        }

        // Spawn VFX
        if (damageVFXPrefab != null)
        {
            Instantiate(damageVFXPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        // Check if limb should be dismembered
        if (enableLimbDismemberment && limbsLost < maxLimbsToLose && Random.value < dismembermentChancePerHit)
        {
            DismemberRandomLimb();
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

    /// <summary>
    /// Find all body parts (limbs) on the enemy
    /// </summary>
    private void CollectLimbParts()
    {
        // Get all child transforms
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        System.Collections.Generic.List<Transform> limbs = new System.Collections.Generic.List<Transform>();

        // Find limb parts by name
        foreach (Transform child in allChildren)
        {
            if (child == transform) continue; // Skip the parent

            string name = child.name.ToLower();
            if (name.Contains("arm") || name.Contains("leg") || name.Contains("hand"))
            {
                limbs.Add(child);
            }
        }

        limbParts = limbs.ToArray();
        LogVerbose($"🦵 {gameObject.name} için {limbParts.Length} limb bulundu");
    }

    /// <summary>
    /// Disable and remove a random limb
    /// </summary>
    private void DismemberRandomLimb()
    {
        if (limbParts == null || limbParts.Length == 0) return;

        // Find all active limbs
        System.Collections.Generic.List<Transform> activeLimbs = new System.Collections.Generic.List<Transform>();
        foreach (Transform limb in limbParts)
        {
            if (limb != null && limb.gameObject.activeSelf)
            {
                // MAX 1 BACAK KOPSIN! 
                string limbName = limb.name.ToLower();
                if ((limbName.Contains("leg")) && legsLost >= 1)
                {
                    continue; // Bu bacağı atla, zaten 1 kopmuş
                }
                
                activeLimbs.Add(limb);
            }
        }

        if (activeLimbs.Count == 0) return;

        // Pick a random limb
        Transform limbToRemove = activeLimbs[Random.Range(0, activeLimbs.Count)];
        string limbType = limbToRemove.name.ToLower();
        
        // Disable it
        limbToRemove.gameObject.SetActive(false);
        limbsLost++;

        // Apply limb-specific effects
        if (limbType.Contains("leg"))
        {
            legsLost++;
            ApplyLegLossEffect();
            LogVerbose($"💥 {gameObject.name} bacak kaybetti! Bacaklar: {legsLost}/1");
        }
        else if (limbType.Contains("arm") || limbType.Contains("hand"))
        {
            ApplyArmLossEffect(limbToRemove);
            LogVerbose($"💥 {gameObject.name} kol kaybetti! '{limbToRemove.name}'");
        }

        // Play sound effect
        PlaySound(limbLossedSFX);
    }

    /// <summary>
    /// Bacak kaybı etkileri - yavaşla ve sendeleme
    /// </summary>
    private void ApplyLegLossEffect()
    {
        if (enemyAI != null)
        {
            // Hızı 0.5x yap
            enemyAI.ApplyLimbDamageEffect(0.5f, true);
        }
    }

    /// <summary>
    /// Kol kaybı etkileri - saldırı devre dışı
    /// </summary>
    private void ApplyArmLossEffect(Transform armLimb)
    {
        string armName = armLimb.name.ToLower();
        
        if (armName.Contains("right"))
        {
            rightArmLost = true;
        }
        else if (armName.Contains("left"))
        {
            leftArmLost = true;
        }

        // Eğer iki kol da kopmuşsa, saldırı yapamaz
        if (rightArmLost && leftArmLost)
        {
            if (enemyAI != null)
            {
                enemyAI.DisableAttacks();
                LogVerbose($"🚫 {gameObject.name} kolları kopmasından saldıramıyor!");
            }
        }
    }

    private void Die()
    {
        isDead = true;
        LogVerbose($"💀 {gameObject.name} öldü!");

        // Ölüm sekansını başlat
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Ölüm animasyonunu oynat
        if (animator != null)
        {
            animator.SetTrigger(hashDying);
            
            // State transition'ın gerçekleşmesi için birkaç frame bekle
            yield return new WaitForSeconds(0.1f);
            
            // Animator state info'yu al
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationDuration = stateInfo.length;
            
            // Eğer 3 saniyeden uzunsa (muhtemelen loop time açık), cap it
            if (animationDuration > 3f)
            {
                animationDuration = 1.5f;
            }
            
            // Animasyon çalışması için animator'u çalıştır bırak
            // (animator'u disable etmeyin, loop time kapalı olduğu için otomatik bitecek)
            // Animasyon süresi kadar bekle
            yield return new WaitForSeconds(animationDuration);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        // Heavy camera impact (Enemy death)
        if (CameraEffects.Instance != null)
        {
            CameraEffects.Instance.HeavyImpactEffect();
        }

        // Disable AI behavior
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Play death sound
        PlaySound(deathSFX);

        // Death VFX
        if (deathVFXPrefab != null)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        }

        // NOW disable animator - animasyon bittiğinden sonra
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Yerde ölü kalsın biraz
        yield return new WaitForSeconds(deathDelay + 1.5f);

        // Fade out
        float fadeDuration = 2f;
        if (spriteRenderer != null)
        {
            yield return StartCoroutine(FadeOut(fadeDuration));
        }

        // Tamamlandı, şimdi destroy et
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator FadeOut(float duration = 0.5f)
    {
        float elapsed = 0f;
        while (elapsed < duration && spriteRenderer != null)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
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

    private void LogVerbose(string message)
    {
        if (verboseLogs)
        {
            Debug.Log(message);
        }
    }

    public float GetHealthPercent() => currentHealth / maxHealth;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
}