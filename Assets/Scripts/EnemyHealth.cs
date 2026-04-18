using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Enemy Health — Spriter2UnityDX uyumlu parçalanma sistemi.
/// Her vuruşta bir uzuv sprite'ı kopyalanıp fiziksel olarak fırlatılır.
/// Ölümde kafa + tüm kalan parçalar sahnede dağılır, sonra fade olur.
/// </summary>
public class EnemyHealth : MonoBehaviour, IHealth
{
    [Header("═══ HEALTH SETTINGS ═══")]
    [SerializeField] private float maxHealth    = 100f;
    [SerializeField] private bool  destroyOnDeath = true;
    [SerializeField] private bool  verboseLogs    = false;

    [Header("═══ VISUAL FEEDBACK ═══")]
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float flashDuration    = 0.08f;

    [Header("═══ VFX & SFX ═══")]
    [SerializeField] private GameObject damageVFXPrefab;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private AudioClip  damageSFX;
    [SerializeField] private AudioClip  deathSFX;
    [SerializeField] private AudioClip  limbLossedSFX;

    [Header("═══ DISMEMBERMENT ═══")]
    [Tooltip("Her vuruşta uzuv kopma şansı (0-1)")]
    [SerializeField] private float dismemberChance  = 0.45f;
    [Tooltip("Kopabilecek maksimum uzuv sayısı")]
    [SerializeField] private int   maxDismemberments = 4;

    // ── Private state ──
    private float          currentHealth;
    private Rigidbody2D    rb;
    private SpriteRenderer rootSR;       // root'taki SR (background sprite)
    private AudioSource    audioSource;
    private Animator       animator;
    private Color          originalColor;
    private bool           isDead = false;
    private EnemyAI        enemyAI;

    // Kopan uzuv takibi: "Left Arm", "Right Arm", "Left Leg", "Right Leg", "Head", "Face 01"
    private int            dismemberCount = 0;
    private HashSet<string> dismemberedNames = new HashSet<string>();

    // Animator hash
    private int hashHurt;

    // ──────────────────────────────────────────────────────────────
    //  Uzuv grubu sırası: önce kol/el, sonra bacak, en son kafa
    //  Her isim => hangi gruba ait (sadece öncelik için)
    // ──────────────────────────────────────────────────────────────
    private static readonly string[][] LimbGroups = {
        new[]{ "Left Arm",   "Left Hand"  },
        new[]{ "Right Arm",  "Right Hand" },
        new[]{ "Left Leg"  },
        new[]{ "Right Leg" },
        new[]{ "Head", "Face 01" },  // kafa grubu — hepsi birden kopuyor
    };

    // ─────────────────────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        rootSR      = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        animator    = GetComponent<Animator>();
        enemyAI     = GetComponent<EnemyAI>();

        if (rootSR != null) originalColor = rootSR.color;
        if (animator != null) hashHurt = Animator.StringToHash("Hurt");
    }

    private void Start() { currentHealth = maxHealth; }

    // ─────────────────────────────────────────────────────────────
    //  IHealth
    // ─────────────────────────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        LogVerbose($"⚡ {gameObject.name} -{damage} HP | {currentHealth:F0}/{maxHealth}");

        if (animator != null) animator.SetTrigger(hashHurt);
        StartCoroutine(DamageFlash());
        PlaySound(damageSFX);

        if (CameraEffects.Instance != null) CameraEffects.Instance.EnemyHitEffect();
        if (damageVFXPrefab != null)
            Instantiate(damageVFXPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);

        // Her vuruşta uzuv kopma şansı
        if (dismemberCount < maxDismemberments && Random.value < dismemberChance)
            TryDismemberLimb();

        if (currentHealth <= 0) Die();
    }

    public void TakeDamage(float damage, Vector2 knockDir, float knockForce)
    {
        TakeDamage(damage);
        if (!isDead && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockDir.normalized * knockForce, ForceMode2D.Impulse);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  HASAR FLASH
    // ─────────────────────────────────────────────────────────────
    private IEnumerator DamageFlash()
    {
        var srs    = GetComponentsInChildren<SpriteRenderer>();
        var colors = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++)
        {
            colors[i]   = srs[i].color;
            srs[i].color = damageFlashColor;
        }

        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(s.x * 1.08f, s.y * 0.93f, s.z);
        yield return new WaitForSeconds(flashDuration);
        transform.localScale = s;

        for (int i = 0; i < srs.Length; i++)
            if (srs[i] != null) srs[i].color = colors[i];
    }

    // ─────────────────────────────────────────────────────────────
    //  UZUV KOPARMA
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Henüz kopmamış bir uzuv grubunu seç, sprite kopyasını fırlat,
    /// orijinali görünmez yap (SetActive yerine — Spriter2UnityDX uyumlu).
    /// </summary>
    private void TryDismemberLimb()
    {
        // Kopmamış grupları bul
        var available = new List<string[]>();
        foreach (var group in LimbGroups)
        {
            // Bu grubun ana ismi (ilk eleman) zaten kopmuş mu?
            if (!dismemberedNames.Contains(group[0]))
                available.Add(group);
        }
        if (available.Count == 0) return;

        // Rastgele bir grup seç
        var chosen = available[Random.Range(0, available.Count)];

        // Gruptaki tüm sprite'ları bul ve kopyala
        bool anyFound = false;
        foreach (var partName in chosen)
        {
            var sr = FindSpriteRenderer(partName);
            if (sr == null) continue;
            anyFound = true;
            SpawnFlyingLimb(sr);
            HideSR(sr);
        }

        if (!anyFound) return;

        // Takibe ekle
        dismemberedNames.Add(chosen[0]);
        dismemberCount++;
        PlaySound(limbLossedSFX);

        // Kol koptu mu? → AI saldırı kısıtla
        string g0 = chosen[0].ToLower();
        if ((g0.Contains("arm") || g0.Contains("hand")) && enemyAI != null)
            enemyAI.ApplyLimbDamageEffect(0.85f, false);

        // Bacak koptu mu? → yavaşla
        if (g0.Contains("leg") && enemyAI != null)
            enemyAI.ApplyLimbDamageEffect(0.5f, true);

        LogVerbose($"💥 {chosen[0]} koptu!");
    }

    /// <summary>Sprite'ı kopyalayıp fiziksel olarak sahneye fırlat.</summary>
    private void SpawnFlyingLimb(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return;

        var go = new GameObject("Detached_" + sr.gameObject.name);
        go.transform.position   = sr.transform.position;
        go.transform.rotation   = sr.transform.rotation;
        go.transform.localScale = sr.transform.lossyScale;

        var copy          = go.AddComponent<SpriteRenderer>();
        copy.sprite       = sr.sprite;
        copy.color        = sr.color;
        copy.sortingOrder = sr.sortingOrder + 10;
        copy.sortingLayerID = sr.sortingLayerID;
        copy.flipX        = sr.flipX;

        var limRb            = go.AddComponent<Rigidbody2D>();
        limRb.gravityScale   = 3.5f;
        limRb.mass           = 0.4f;
        float dx             = Random.value > 0.5f ? 1f : -1f;
        limRb.linearVelocity = new Vector2(dx * Random.Range(2f, 6f), Random.Range(3f, 8f));
        limRb.angularVelocity= dx * Random.Range(80f, 300f);

        // 4 saniye sonra fade + yok et
        StartCoroutine(FadeAndDestroy(go, 3f, 1.5f));
    }

    /// <summary>Spriter2UnityDX uyumlu gizleme — color alpha=0 yap, aktifliği değiştirme.</summary>
    private void HideSR(SpriteRenderer sr)
    {
        if (sr == null) return;
        // Sprite'ı tamamen şeffaf yap — SetActive yapmıyoruz
        var c = sr.color; c.a = 0f; sr.color = c;
        // Ek güvenlik: sprite'ı null yap
        sr.sprite = null;
    }

    // ─────────────────────────────────────────────────────────────
    //  ÖLÜM
    // ─────────────────────────────────────────────────────────────
    private void Die()
    {
        isDead = true;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // AI + animator durdur
        if (enemyAI  != null) enemyAI.enabled  = false;
        if (animator != null) animator.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity  = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Kamera + ses
        if (CameraEffects.Instance != null) CameraEffects.Instance.HeavyImpactEffect();
        PlaySound(deathSFX);
        if (deathVFXPrefab != null) Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        // ── Kalan tüm uzuvları + kafayı aynı anda fırlat ──
        var allSRs = GetComponentsInChildren<SpriteRenderer>();
        var flying = new List<GameObject>();

        foreach (var sr in allSRs)
        {
            if (sr == null || sr.sprite == null) continue;
            // Zaten kopmuş olanları atla (sprite=null veya alpha=0)
            if (sr.color.a < 0.05f) continue;

            var go = new GameObject("Death_" + sr.gameObject.name);
            go.transform.position   = sr.transform.position;
            go.transform.rotation   = sr.transform.rotation;
            go.transform.localScale = sr.transform.lossyScale;

            var copy          = go.AddComponent<SpriteRenderer>();
            copy.sprite       = sr.sprite;
            copy.color        = sr.color;
            copy.sortingOrder = sr.sortingOrder + 15;
            copy.sortingLayerID = sr.sortingLayerID;
            copy.flipX        = sr.flipX;

            var prb            = go.AddComponent<Rigidbody2D>();
            prb.gravityScale   = 4f;
            prb.mass           = 0.3f;
            float dx           = Random.value > 0.5f ? 1f : -1f;
            // Kafa mı? → daha güçlü fırlat
            bool isHead = sr.gameObject.name.ToLower().Contains("head")
                       || sr.gameObject.name.ToLower().Contains("face");
            float forceUp = isHead ? Random.Range(8f, 13f) : Random.Range(2f, 6f);
            float forceSide = isHead ? Random.Range(3f, 7f) : Random.Range(1f, 5f);
            prb.linearVelocity  = new Vector2(dx * forceSide, forceUp);
            prb.angularVelocity = dx * Random.Range(100f, 500f);

            flying.Add(go);
        }

        // Orijinal gövdeyi hemen gizle
        foreach (var sr in allSRs)
        {
            if (sr != null) { var c = sr.color; c.a = 0f; sr.color = c; sr.sprite = null; }
        }

        if (destroyOnDeath) gameObject.SetActive(false);

        // Uçan parçalar yere çarpsın (0.6s bekle) sonra fade
        yield return new WaitForSeconds(0.6f);
        foreach (var go in flying)
        {
            if (go != null)
                StartCoroutine(FadeAndDestroy(go, 0f, 1.8f));
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────
    /// <summary>İsme göre alt hiyerarşide SpriteRenderer bul.</summary>
    private SpriteRenderer FindSpriteRenderer(string partName)
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr != null && sr.gameObject.name == partName && sr.color.a > 0.05f)
                return sr;
        }
        return null;
    }

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

    /// <summary>Belirtilen süreden sonra fade olup destroy et.</summary>
    private IEnumerator FadeAndDestroy(GameObject go, float delay, float fadeDuration)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (go == null) yield break;

        var srs = go.GetComponentsInChildren<SpriteRenderer>();
        float elapsed = 0f;
        while (elapsed < fadeDuration && go != null)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            foreach (var sr in srs)
                if (sr != null) { var c = sr.color; c.a = a; sr.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private IEnumerator FadeRenderers(SpriteRenderer[] srs, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / duration);
            foreach (var sr in srs)
                if (sr != null) { var c = sr.color; c.a = a; sr.color = c; }
            yield return null;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    private void LogVerbose(string msg)
    {
        if (verboseLogs) Debug.Log(msg);
    }

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────
    public float GetHealthPercent() => currentHealth / maxHealth;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth()     => maxHealth;
    public bool  IsDead()           => isDead;

    /// <summary>GameManager tarafından çağrılır — tam reset.</summary>
    public void ResetHealth()
    {
        isDead          = false;
        currentHealth   = maxHealth;
        dismemberCount  = 0;
        dismemberedNames.Clear();

        // Tüm sprite'ları geri getir
        if (animator != null) { animator.enabled = true; animator.Rebind(); }

        // Root sprite rengini sıfırla
        var srs = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
            if (sr != null) { var c = sr.color; c.a = 1f; sr.color = c; }

        Debug.Log($"♻️ {gameObject.name} sıfırlandı.");
    }
}
