using UnityEngine;
using System.Collections;

/// <summary>
/// Oyuncu sağlık, ölüm ve checkpoint geri dönüş sistemi.
/// IHealth interface'ini implement eder — EnemyAI ve diğer düşmanlar bu scripti bulur.
/// </summary>
public class PlayerHealth : MonoBehaviour, IHealth
{
    [Header("Sağlık")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Hasar Geri Bildirimi")]
    [SerializeField] private float invincibilityDuration = 1f;   // Hasar sonrası geçici dokunulmazlık
    [SerializeField] private float deathFadeTime = 0.6f;         // Ölüm fade süresi

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1.5f;          // Ölüm → respawn arası süre

    // Bileşenler
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;
    private PlayerCombat playerCombat;

    // Durum
    private bool isDead = false;
    private bool isInvincible = false;
    private Vector3 checkpointPosition;

    // Animator hash
    private int hashHurt;
    private int hashDie;

    // ─── Singleton erişimi (opsiyonel, ama kolay ulaşmak için) ───
    public static PlayerHealth Instance { get; private set; }

    // ─── Checkpoint kayıt noktaları ───
    private static Vector3 savedCheckpoint;
    private static bool checkpointSaved = false;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();

        hashHurt = Animator.StringToHash("Hurt");
        hashDie  = Animator.StringToHash("Death");
    }

    private void Start()
    {
        currentHealth = maxHealth;

        // Sahnede kayıtlı checkpoint varsa oradan başla
        if (checkpointSaved)
            checkpointPosition = savedCheckpoint;
        else
            checkpointPosition = transform.position;   // Sahne başlangıç noktası = ilk checkpoint
    }

    // ─────────────────────────────────────────────────────────────
    //  IHealth IMPLEMENTATION
    // ─────────────────────────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        if (isDead || isInvincible) return;

        currentHealth -= damage;
        Debug.Log($"💔 Oyuncu {damage} hasar aldı! Kalan HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            StartCoroutine(HurtRoutine());
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  CHECKPOINT
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Herhangi bir checkpoint tetikleyici bu metodu çağırır.
    /// </summary>
    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
        savedCheckpoint    = position;
        checkpointSaved    = true;
        Debug.Log($"📍 Checkpoint kaydedildi: {position}");
    }

    // ─────────────────────────────────────────────────────────────
    //  ÖLÜM & RESPAWN
    // ─────────────────────────────────────────────────────────────
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("💀 Oyuncu öldü! Respawn bekleniyor...");

        // Hareketi durdur
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Kontrolleri devre dışı bırak
        if (playerController != null) playerController.enabled = false;
        if (playerCombat     != null) playerCombat.enabled     = false;

        // Ölüm animasyonu tetikle (yoksa sadece fade)
        if (animator != null)
        {
            bool hasDeath = HasAnimatorParam(hashDie);
            if (hasDeath) animator.SetTrigger(hashDie);
        }

        StartCoroutine(DeathAndRespawn());
    }

    private IEnumerator DeathAndRespawn()
    {
        // Kamerada kırmızı flash efekti (varsa)
        if (CameraEffects.Instance != null)
            CameraEffects.Instance.MeleeHitEffect();

        // Ekran kara  fade
        yield return StartCoroutine(FadeOut());

        yield return new WaitForSeconds(respawnDelay * 0.5f);

        // ── RESPAWN ──
        Respawn();

        // Ekranı açık fade
        yield return StartCoroutine(FadeIn());
    }

    private void Respawn()
    {
        isDead        = false;
        isInvincible  = false;
        currentHealth = maxHealth;

        // Pozisyonu checkpoint'e taşı
        transform.position = checkpointPosition;

        // Fizik ve kontrolleri geri aç
        if (rb != null)
        {
            rb.bodyType        = RigidbodyType2D.Dynamic;
            rb.linearVelocity  = Vector2.zero;
        }

        if (playerController != null) playerController.enabled = true;
        if (playerCombat     != null) playerCombat.enabled     = true;

        // Animatörü sıfırla
        if (animator != null)
            animator.Rebind();

        // Sprite'ı görünür yap
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        Debug.Log($"✅ Oyuncu respawn oldu: {checkpointPosition}");
    }

    // ─────────────────────────────────────────────────────────────
    //  HURT FLASH
    // ─────────────────────────────────────────────────────────────
    private IEnumerator HurtRoutine()
    {
        isInvincible = true;

        if (animator != null && HasAnimatorParam(hashHurt))
            animator.SetTrigger(hashHurt);

        // Kısa süre kırmızı yak
        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = elapsed % 0.2f < 0.1f ? Color.red : Color.white;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        isInvincible = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  FADE HELPERS  (SpriteRenderer alpha yetersizse ScreenFade varsa kullanılır)
    // ─────────────────────────────────────────────────────────────
    private IEnumerator FadeOut()
    {
        if (spriteRenderer == null) yield break;
        float t = 0f;
        Color c = spriteRenderer.color;
        while (t < deathFadeTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, t / deathFadeTime);
            spriteRenderer.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        spriteRenderer.color = new Color(c.r, c.g, c.b, 0f);
    }

    private IEnumerator FadeIn()
    {
        if (spriteRenderer == null) yield break;
        float t = 0f;
        Color c = spriteRenderer.color;
        while (t < deathFadeTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(0f, 1f, t / deathFadeTime);
            spriteRenderer.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        spriteRenderer.color = new Color(c.r, c.g, c.b, 1f);
    }

    // ─────────────────────────────────────────────────────────────
    //  YARDIMCİ
    // ─────────────────────────────────────────────────────────────
    private bool HasAnimatorParam(int hash)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.nameHash == hash) return true;
        return false;
    }

    /// <summary>
    /// Editörde anlık health göstergesi (debug bar).
    /// </summary>
    private void OnGUI()
    {
#if UNITY_EDITOR
        GUI.color = Color.red;
        GUI.Label(new Rect(10, 10, 200, 25), $"❤️ HP: {currentHealth:F0} / {maxHealth:F0}");
        GUI.color = Color.white;
#endif
    }
}
