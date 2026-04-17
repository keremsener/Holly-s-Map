using UnityEngine;
using System.Collections;

/// <summary>
/// Professional Enemy Health System with damage, knockback, and visual feedback
/// Integrates with EnemyAI state machine
/// </summary>
public class EnemyHealth : MonoBehaviour, IHealth
{
    [Header("â•â•â• HEALTH SETTINGS â•â•â•")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathDelay = 0.5f;
    [SerializeField] private bool verboseLogs = false;

    [Header("â•â•â• VISUAL FEEDBACK â•â•â•")]
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color deathColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("â•â•â• KNOCKBACK SETTINGS â•â•â•")]
    [SerializeField] private float knockbackDecay = 0.95f;

    [Header("â•â•â• VFX & SFX â•â•â•")]
    [SerializeField] private GameObject damageVFXPrefab;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private AudioClip damageSFX;
    [SerializeField] private AudioClip deathSFX;

    [Header("â•â•â• LIMB DISMEMBERMENT â•â•â•")]
    [SerializeField] private bool enableLimbDismemberment = true;
    [SerializeField] private float dismembermentChancePerHit = 0.3f; // 30% ÅŸansÄ± her hit'te
    [SerializeField] private int maxLimbsToLose = 4; // Maksimum kaÃ§ limb kaybedilebilir (2 arm + 2 leg)
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
    private int legsLost = 0; // Bacak sayÄ±sÄ±
    private bool rightArmLost = false; // SaÄŸ kol kopma durumu
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
            // EÄŸer manuel olarak assign edildiyse, onlarÄ± kullan
            if (manualLimbParts != null && manualLimbParts.Length > 0)
            {
                limbParts = manualLimbParts;
                LogVerbose($"ğŸ“‹ {gameObject.name} iÃ§in {limbParts.Length} limb manuel olarak assign edildi");
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
        LogVerbose($"âš¡ {gameObject.name} hasar aldÄ±! -{damage} HP | Kalan: {currentHealth:F0}/{maxHealth}");

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
        // Bulunan tÃ¼m sprite'larÄ± kÄ±zart (Skeletal animation'lar iÃ§in root'ta deÄŸil child'larda olabilir)
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] origColors = new Color[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            origColors[i] = renderers[i].color;
            renderers[i].color = damageFlashColor;
        }

        // VuruÅŸ hissi (Squash and Stretch) iÃ§in anlÄ±k esneme
        Vector3 origScale = transform.localScale;
        transform.localScale = new Vector3(origScale.x * 1.1f, origScale.y * 0.9f, origScale.z);

        yield return new WaitForSeconds(flashDuration);

        // Eski haline geri dÃ¶ndÃ¼r
        transform.localScale = origScale;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = origColors[i];
        }
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
        LogVerbose($"ğŸ¦µ {gameObject.name} iÃ§in {limbParts.Length} limb bulundu");
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
                    continue; // Bu bacaÄŸÄ± atla, zaten 1 kopmuÅŸ
                }
                
                activeLimbs.Add(limb);
            }
        }

        if (activeLimbs.Count == 0) return;

        // Pick a random limb
        Transform limbToRemove = activeLimbs[Random.Range(0, activeLimbs.Count)];
        string limbType = limbToRemove.name.ToLower();
        
        // --- Spawn detached physics limb ---
        SpriteRenderer limbSpriteRenderer = limbToRemove.GetComponent<SpriteRenderer>();
        if (limbSpriteRenderer != null && limbSpriteRenderer.sprite != null)
        {
            GameObject detachedLimb = new GameObject("Detached_" + limbToRemove.name);
            detachedLimb.transform.position = limbToRemove.position;
            detachedLimb.transform.rotation = limbToRemove.rotation;
            detachedLimb.transform.localScale = limbToRemove.lossyScale;

            SpriteRenderer sr = detachedLimb.AddComponent<SpriteRenderer>();
            sr.sprite = limbSpriteRenderer.sprite;
            sr.sortingLayerID = limbSpriteRenderer.sortingLayerID;
            sr.sortingOrder = limbSpriteRenderer.sortingOrder;
            sr.color = limbSpriteRenderer.color;

            Rigidbody2D limbRb = detachedLimb.AddComponent<Rigidbody2D>();
            limbRb.mass = 0.5f;
            Vector2 dropForce = new Vector2(Random.Range(-5f, 5f), Random.Range(3f, 8f));
            limbRb.AddForce(dropForce, ForceMode2D.Impulse);
            limbRb.AddTorque(Random.Range(-100f, 100f));

            detachedLimb.AddComponent<PolygonCollider2D>();
            Destroy(detachedLimb, 5f); // 5 saniye sonra yok et
        }
        // -----------------------------------

        // Disable it
        limbToRemove.gameObject.SetActive(false);
        limbsLost++;

        // Apply limb-specific effects
        if (limbType.Contains("leg"))
        {
            legsLost++;
            ApplyLegLossEffect();
            LogVerbose($"ğŸ’¥ {gameObject.name} bacak kaybetti! Bacaklar: {legsLost}/1");
        }
        else if (limbType.Contains("arm") || limbType.Contains("hand"))
        {
            ApplyArmLossEffect(limbToRemove);
            LogVerbose($"ğŸ’¥ {gameObject.name} kol kaybetti! '{limbToRemove.name}'");
        }

        // Play sound effect
        PlaySound(limbLossedSFX);
    }

    /// <summary>
    /// Bacak kaybÄ± etkileri - yavaÅŸla ve sendeleme
    /// </summary>
    private void ApplyLegLossEffect()
    {
        if (enemyAI != null)
        {
            // HÄ±zÄ± 0.5x yap
            enemyAI.ApplyLimbDamageEffect(0.5f, true);
        }
    }

    /// <summary>
    /// Kol kaybÄ± etkileri - saldÄ±rÄ± devre dÄ±ÅŸÄ±
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

        // EÄŸer iki kol da kopmuÅŸsa, saldÄ±rÄ± yapamaz
        if (rightArmLost && leftArmLost)
        {
            if (enemyAI != null)
            {
                enemyAI.DisableAttacks();
                LogVerbose($"ğŸš« {gameObject.name} kollarÄ± kopmasÄ±ndan saldÄ±ramÄ±yor!");
            }
        }
    }

    private void Die()
    {
        isDead = true;
        LogVerbose($"ğŸ’€ {gameObject.name} Ã¶ldÃ¼!");

        // Ã–lÃ¼m sekansÄ±nÄ± baÅŸlat
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Ã–lÃ¼m animasyonunu oynat
        if (animator != null)
        {
            animator.SetTrigger(hashDying);
            
            // State transition'Ä±n gerÃ§ekleÅŸmesi iÃ§in birkaÃ§ frame bekle
            yield return new WaitForSeconds(0.1f);
            
            // Animator state info'yu al
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationDuration = stateInfo.length;
            
            // EÄŸer 3 saniyeden uzunsa (muhtemelen loop time aÃ§Ä±k), cap it
            if (animationDuration > 3f)
            {
                animationDuration = 1.5f;
            }
            
            // Animasyon Ã§alÄ±ÅŸmasÄ± iÃ§in animator'u Ã§alÄ±ÅŸtÄ±r bÄ±rak
            // (animator'u disable etmeyin, loop time kapalÄ± olduÄŸu iÃ§in otomatik bitecek)
            // Animasyon sÃ¼resi kadar bekle
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

        // NOW disable animator - animasyon bittiÄŸinden sonra
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Yerde Ã¶lÃ¼ kalsÄ±n biraz
        yield return new WaitForSeconds(deathDelay + 1.5f);

        // Fade out
        float fadeDuration = 2f;
        if (spriteRenderer != null)
        {
            yield return StartCoroutine(FadeOut(fadeDuration));
        }

        // TamamlandÄ±, ÅŸimdi destroy et
        if (destroyOnDeath)
        {
            gameObject.SetActive(false);
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

    /// <summary>GameManager tarafindan cagrilir: dusmani tam saglikla sifirlar.</summary>
    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        limbsLost = 0;
        legsLost = 0;
        rightArmLost = false;
        leftArmLost = false;

        // Limbleri geri ac
        if (limbParts != null)
            foreach (var limb in limbParts)
                if (limb != null) limb.gameObject.SetActive(true);

        // Sprite rengini sifirla
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
            if (sr != null) sr.color = sr == spriteRenderer ? originalColor : Color.white;

        // Animator'u yeniden baslat
        if (animator != null) { animator.enabled = true; animator.Rebind(); }

        Debug.Log($"â™»ï¸ {gameObject.name} saglik sifirlandi.");
    }
}
