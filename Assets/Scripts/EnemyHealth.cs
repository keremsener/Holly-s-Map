using UnityEngine;
using System.Collections;

/// <summary>
/// Professional Enemy Health System with damage, knockback, limb dismemberment, and head-pop death.
/// </summary>
public class EnemyHealth : MonoBehaviour, IHealth
{
    [Header("═══ HEALTH SETTINGS ═══")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool  destroyOnDeath = true;
    [SerializeField] private bool  verboseLogs    = false;

    [Header("═══ VISUAL FEEDBACK ═══")]
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float flashDuration    = 0.1f;

    [Header("═══ VFX & SFX ═══")]
    [SerializeField] private GameObject damageVFXPrefab;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private AudioClip  damageSFX;
    [SerializeField] private AudioClip  deathSFX;

    [Header("═══ LIMB DISMEMBERMENT ═══")]
    [SerializeField] private bool        enableLimbDismemberment  = true;
    [SerializeField] private float       dismembermentChancePerHit = 0.3f;
    [SerializeField] private int         maxLimbsToLose            = 4;
    [SerializeField] private AudioClip   limbLossedSFX;
    [SerializeField] private Transform[] manualLimbParts;

    // ── Private state ──
    private float          currentHealth;
    private Rigidbody2D    rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource    audioSource;
    private Animator       animator;
    private Color          originalColor;
    private bool           isDead = false;
    private EnemyAI        enemyAI;

    private Transform[] limbParts;
    private int         limbsLost    = 0;
    private int         legsLost     = 0;
    private bool        rightArmLost = false;
    private bool        leftArmLost  = false;

    private int hashHurt;
    private int hashDying;

    // ─────────────────────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource    = GetComponent<AudioSource>();
        animator       = GetComponent<Animator>();
        enemyAI        = GetComponent<EnemyAI>();

        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (animator != null)
        {
            hashHurt  = Animator.StringToHash("Hurt");
            hashDying = Animator.StringToHash("Dying");
        }

        if (enableLimbDismemberment)
        {
            if (manualLimbParts != null && manualLimbParts.Length > 0)
                limbParts = manualLimbParts;
            else
                CollectLimbParts();
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    // ─────────────────────────────────────────────────────────────
    //  IHealth IMPLEMENTATION
    // ─────────────────────────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        LogVerbose($"⚡ {gameObject.name} -{damage} HP | Kalan: {currentHealth:F0}/{maxHealth}");

        if (animator != null) animator.SetTrigger(hashHurt);
        StartCoroutine(DamageFlash());
        PlaySound(damageSFX);

        if (CameraEffects.Instance != null) CameraEffects.Instance.EnemyHitEffect();
        if (damageVFXPrefab != null)
            Instantiate(damageVFXPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        if (enableLimbDismemberment && limbsLost < maxLimbsToLose && Random.value < dismembermentChancePerHit)
            DismemberRandomLimb();

        if (currentHealth <= 0) Die();
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
    {
        TakeDamage(damage);
        if (!isDead && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  VISUAL FEEDBACK
    // ─────────────────────────────────────────────────────────────
    private IEnumerator DamageFlash()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] origColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            origColors[i]       = renderers[i].color;
            renderers[i].color  = damageFlashColor;
        }

        Vector3 origScale    = transform.localScale;
        transform.localScale = new Vector3(origScale.x * 1.1f, origScale.y * 0.9f, origScale.z);

        yield return new WaitForSeconds(flashDuration);

        transform.localScale = origScale;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = origColors[i];
    }

    // ─────────────────────────────────────────────────────────────
    //  LIMB DISMEMBERMENT
    // ─────────────────────────────────────────────────────────────
    private void CollectLimbParts()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        var limbs = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child == transform) continue;
            string n = child.name.ToLower();
            if (n.Contains("arm") || n.Contains("leg") || n.Contains("hand"))
                limbs.Add(child);
        }
        limbParts = limbs.ToArray();
        LogVerbose($"🦵 {gameObject.name}: {limbParts.Length} limb bulundu");
    }

    private void DismemberRandomLimb()
    {
        if (limbParts == null || limbParts.Length == 0) return;

        var activeLimbs = new System.Collections.Generic.List<Transform>();
        foreach (Transform limb in limbParts)
        {
            if (limb == null || !limb.gameObject.activeSelf) continue;
            if (limb.name.ToLower().Contains("leg") && legsLost >= 1) continue;
            activeLimbs.Add(limb);
        }
        if (activeLimbs.Count == 0) return;

        Transform limbToRemove = activeLimbs[Random.Range(0, activeLimbs.Count)];
        string    limbType     = limbToRemove.name.ToLower();

        // Kopan limb fiziği
        SpriteRenderer limbSR = limbToRemove.GetComponent<SpriteRenderer>();
        if (limbSR != null && limbSR.sprite != null)
        {
            GameObject detached = new GameObject("Detached_" + limbToRemove.name);
            detached.transform.position   = limbToRemove.position;
            detached.transform.rotation   = limbToRemove.rotation;
            detached.transform.localScale = limbToRemove.lossyScale;

            var sr            = detached.AddComponent<SpriteRenderer>();
            sr.sprite         = limbSR.sprite;
            sr.sortingLayerID = limbSR.sortingLayerID;
            sr.sortingOrder   = limbSR.sortingOrder;
            sr.color          = limbSR.color;

            var limbRb = detached.AddComponent<Rigidbody2D>();
            limbRb.mass = 0.5f;
            limbRb.AddForce(new Vector2(Random.Range(-5f, 5f), Random.Range(3f, 8f)), ForceMode2D.Impulse);
            limbRb.AddTorque(Random.Range(-100f, 100f));
            detached.AddComponent<PolygonCollider2D>();
            Destroy(detached, 5f);
        }

        limbToRemove.gameObject.SetActive(false);
        limbsLost++;

        if (limbType.Contains("leg"))
        {
            legsLost++;
            if (enemyAI != null) enemyAI.ApplyLimbDamageEffect(0.5f, true);
        }
        else if (limbType.Contains("arm") || limbType.Contains("hand"))
        {
            if (limbType.Contains("right"))      rightArmLost = true;
            else if (limbType.Contains("left"))  leftArmLost  = true;

            if (rightArmLost && leftArmLost && enemyAI != null)
                enemyAI.DisableAttacks();
        }

        PlaySound(limbLossedSFX);
    }

    // ─────────────────────────────────────────────────────────────
    //  DEATH — KAFA KOPMA
    // ─────────────────────────────────────────────────────────────
    private void Die()
    {
        isDead = true;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // AI ve hareketi anında durdur
        if (enemyAI  != null) enemyAI.enabled  = false;
        if (animator != null) animator.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType       = RigidbodyType2D.Kinematic;
        }

        // Efektler
        if (CameraEffects.Instance != null) CameraEffects.Instance.HeavyImpactEffect();
        PlaySound(deathSFX);
        if (deathVFXPrefab != null) Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        // ── KAFA KOPMA ──
        Transform  headBone     = FindDeep(transform, "Head");
        GameObject detachedHead = null;

        if (headBone != null)
        {
            headBone.SetParent(null);

            var headRB = headBone.GetComponent<Rigidbody2D>();
            if (headRB == null) headRB = headBone.gameObject.AddComponent<Rigidbody2D>();
            headRB.bodyType     = RigidbodyType2D.Dynamic;
            headRB.gravityScale = 4f;

            float dirX             = Random.value > 0.5f ? 1f : -1f;
            headRB.linearVelocity  = new Vector2(dirX * Random.Range(3f, 6f), Random.Range(6f, 10f));
            headRB.angularVelocity = dirX * Random.Range(180f, 400f);

            detachedHead = headBone.gameObject;
        }

        // Gövde koyulaşır (kırmızı tonu)
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.25f, 0.1f, 0.1f, 1f);

        // Kısa bekle — kafa düşsün, dramatik an
        yield return new WaitForSeconds(0.5f);

        // Gövdeyi hızlı fade et (0.8s)
        yield return StartCoroutine(FadeRenderers(GetComponentsInChildren<SpriteRenderer>(), 0.8f));

        if (destroyOnDeath) gameObject.SetActive(false);

        // Kafayı yavaşça fade et (1.2s)
        if (detachedHead != null)
        {
            yield return new WaitForSeconds(0.4f);
            if (detachedHead != null)
            {
                yield return StartCoroutine(FadeRenderers(detachedHead.GetComponentsInChildren<SpriteRenderer>(), 1.2f));
                Destroy(detachedHead);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────
    private Transform FindDeep(Transform parent, string searchName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == searchName) return child;
            var r = FindDeep(child, searchName);
            if (r != null) return r;
        }
        return null;
    }

    private IEnumerator FadeRenderers(SpriteRenderer[] renderers, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / duration);
            foreach (var sr in renderers)
                if (sr != null) { var c = sr.color; c.a = a; sr.color = c; }
            yield return null;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    private void LogVerbose(string message)
    {
        if (verboseLogs) Debug.Log(message);
    }

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────
    public float GetHealthPercent() => currentHealth / maxHealth;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth()     => maxHealth;
    public bool  IsDead()           => isDead;

    /// <summary>GameManager tarafından çağrılır: düşmanı tam sağlıkla sıfırlar.</summary>
    public void ResetHealth()
    {
        isDead        = false;
        currentHealth = maxHealth;
        limbsLost     = 0;
        legsLost      = 0;
        rightArmLost  = false;
        leftArmLost   = false;

        if (limbParts != null)
            foreach (var limb in limbParts)
                if (limb != null) limb.gameObject.SetActive(true);

        var renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
            if (sr != null) sr.color = (sr == spriteRenderer) ? originalColor : Color.white;

        if (animator != null) { animator.enabled = true; animator.Rebind(); }

        Debug.Log($"♻️ {gameObject.name} sağlık sıfırlandı.");
    }
}
