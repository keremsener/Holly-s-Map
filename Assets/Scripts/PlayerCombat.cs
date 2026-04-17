using UnityEngine;
using System.Collections;

/// <summary>
/// Oyuncu Dövüş Sistemi — Hızlı, Hissettiren, Kanlı
/// Sol Tık: Kılıç saldırısı (timeout yok, tak tak tak)
/// F: Büyü atışı
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    #region Serialized Fields
    [Header("═══ MELEE ATTACK (Sol Tık) ═══")]
    [SerializeField] private Transform meleePoint;
    [SerializeField] private float meleeRange = 1.5f;
    [SerializeField] private float meleeDamage = 40f;
    [SerializeField] private float meleeKnockback = 8f;
    [Tooltip("Saniyede kaç vuruş yapabilsin (0.15 = çok hızlı)")]
    [SerializeField] private float attackRate = 0.15f;   // cooldown yok hissi
    [SerializeField] private LayerMask enemyLayers;

    [Header("═══ MAGIC ATTACK (F) ═══")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject magicPrefab;
    [SerializeField] private int maxMagicCharges = 3;
    [SerializeField] private float manaRechargeTime = 5f;

    [Header("═══ VFX ═══")]
    [SerializeField] private GameObject slashVFXPrefab;
    [SerializeField] private GameObject bloodBurstPrefab;
    [SerializeField] private GameObject impactVFXPrefab;

    [Header("═══ SFX ═══")]
    [SerializeField] private AudioClip slashSFX;
    [SerializeField] private AudioClip magicSFX;
    [SerializeField] private AudioClip hitSFX;

    [Header("═══ COMBAT FEEL ═══")]
    [Tooltip("Vuruş anındaki time-scale duraklama süresi (saniye, realtime)")]
    [SerializeField] private float hitStopDuration = 0.06f;
    [Tooltip("Hitsstop sırasındaki time-scale (0 = tam dondurma, 0.1 = çok yavaş)")]
    [SerializeField] private float hitStopScale = 0.05f;
    [Tooltip("Vuruş anında kamera sarsıntısı şiddeti")]
    [SerializeField] private float hitShakeMagnitude = 0.12f;
    [Tooltip("Kan efekti sayısı (her vuruşta)")]
    [SerializeField] private int bloodParticlesPerHit = 3;
    #endregion

    #region Private
    private Animator animator;
    private AudioSource audioSource;
    private int currentMagicCharges;
    private bool isRecharging = false;
    private float lastAttackTime = -999f;
    private Camera mainCamera;

    // Animator hash
    private int hashAttack;

    // Hit stop
    private Coroutine hitStopCoroutine;

    // Procedural kan için renkler
    private static readonly Color32[] bloodColors = {
        new Color32(180,  20,  20, 255),
        new Color32(220,  40,  40, 255),
        new Color32(140,   5,   5, 255),
        new Color32(200,  60,  40, 255),
    };
    #endregion

    #region Lifecycle
    private void Awake()
    {
        animator    = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        mainCamera  = Camera.main;
        hashAttack  = Animator.StringToHash("Attack");
    }

    private void Start()
    {
        currentMagicCharges = maxMagicCharges;
        if (meleePoint == null) Debug.LogWarning("⚠️ MeleePoint atanmamış!");
        if (firePoint  == null) Debug.LogWarning("⚠️ FirePoint atanmamış!");
    }

    private void Update()
    {
        HandleMeleeInput();
        HandleMagicInput();
        HandleManaRecharge();
    }
    #endregion

    #region Input
    private void HandleMeleeInput()
    {
        // GetKey ile basılı tutunca sürekli vurur — attackRate ile hız kontrol
        if (Input.GetKey(KeyCode.Mouse0))
        {
            if (Time.time >= lastAttackTime + attackRate)
            {
                lastAttackTime = Time.time;
                MeleeAttack();
            }
        }
    }

    private void HandleMagicInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (currentMagicCharges > 0)
            {
                ShootMagic();
                currentMagicCharges--;
            }
            else
            {
                Debug.Log("❌ Mana bitti!");
            }
        }
    }

    private void HandleManaRecharge()
    {
        if (currentMagicCharges < maxMagicCharges && !isRecharging)
            StartCoroutine(RechargeMana());
    }
    #endregion

    #region Combat
    private void MeleeAttack()
    {
        if (meleePoint == null) return;

        // Animasyon
        if (animator != null) animator.SetTrigger(hashAttack);

        // Slash VFX
        if (slashVFXPrefab != null)
            Instantiate(slashVFXPrefab, meleePoint.position, meleePoint.rotation);

        // Slash SFX
        PlayAudio(slashSFX);

        // Düşman tespiti
        Collider2D[] hits = Physics2D.OverlapCircleAll(meleePoint.position, meleeRange, enemyLayers);
        bool hitAnEnemy = false;

        foreach (Collider2D hit in hits)
        {
            Vector2 kbDir = (hit.transform.position - transform.position).normalized;

            // Hasar ver
            var health = hit.GetComponent<IHealth>();
            if (health != null)
            {
                health.TakeDamage(meleeDamage);
                hitAnEnemy = true;

                // Kamera efekti
                if (CameraEffects.Instance != null)
                    CameraEffects.Instance.MeleeHitEffect();

                // Hit stop (freeze frame hissi)
                if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
                hitStopCoroutine = StartCoroutine(DoHitStop());

                // Kan efekti — prefab varsa kullan, yoksa procedural
                SpawnBlood(hit.transform.position);

                // Impact VFX
                if (impactVFXPrefab != null)
                    Instantiate(impactVFXPrefab, hit.transform.position + Vector3.up * 0.5f, Quaternion.identity);
            }

            // Knockback
            var rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(kbDir * meleeKnockback, ForceMode2D.Impulse);
            }

            // Hit SFX
            PlayAudio(hitSFX);
        }

        // Boşluğa vurursa hafif kamera sarsıntısı yok (sadece hit'te olsun)
    }

    private void ShootMagic()
    {
        if (firePoint == null) return;

        if (animator != null) animator.SetTrigger(hashAttack);

        if (magicPrefab != null)
            Instantiate(magicPrefab, firePoint.position, firePoint.rotation);

        PlayAudio(magicSFX);

        if (CameraEffects.Instance != null)
            CameraEffects.Instance.MagicHitEffect();
    }
    #endregion

    #region Blood VFX
    /// <summary>
    /// Prefab varsa prefab, yoksa procedural particle-benzeri sprite'larla kan fışkırtır.
    /// </summary>
    private void SpawnBlood(Vector3 enemyPos)
    {
        if (bloodBurstPrefab != null)
        {
            // Birden fazla kan fışkırması
            for (int i = 0; i < bloodParticlesPerHit; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-0.4f, 0.4f),
                    Random.Range(0.5f, 1.6f),
                    0f);
                var vfx = Instantiate(bloodBurstPrefab, enemyPos + offset, Quaternion.identity);
                // Her VFX'i hafif farklı scale'de spawnla
                vfx.transform.localScale = Vector3.one * Random.Range(0.6f, 1.4f);
            }
        }
        else
        {
            // Procedural kan damlacıkları (sprite gerektirmez)
            StartCoroutine(SpawnProceduralBlood(enemyPos));
        }
    }

    private IEnumerator SpawnProceduralBlood(Vector3 origin)
    {
        int count = Random.Range(bloodParticlesPerHit, bloodParticlesPerHit + 3);
        for (int i = 0; i < count; i++)
        {
            var drop = new GameObject("BloodDrop");
            drop.transform.position = origin + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.3f, 1.2f), 0f);

            var sr = drop.AddComponent<SpriteRenderer>();
            sr.sprite = GetCircleSprite();
            sr.color  = bloodColors[Random.Range(0, bloodColors.Length)];
            sr.sortingOrder = 15;
            float s = Random.Range(0.05f, 0.18f);
            drop.transform.localScale = new Vector3(s, s, 1f);

            var rb = drop.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.5f;
            Vector2 vel = new Vector2(Random.Range(-5f, 5f), Random.Range(4f, 9f));
            rb.linearVelocity = vel;

            Destroy(drop, 1.2f);
        }
        yield return null;
    }

    // Tek renk kare sprite oluşturur (1x1 pixel)
    private static Sprite _circleSprite;
    private static Sprite GetCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;
        var tex = new Texture2D(4, 4);
        Color32[] pixels = new Color32[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels32(pixels);
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);
        return _circleSprite;
    }
    #endregion

    #region Coroutines
    private IEnumerator DoHitStop()
    {
        float orig = Time.timeScale;
        Time.timeScale = hitStopScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = orig;
        hitStopCoroutine = null;
    }

    private IEnumerator RechargeMana()
    {
        isRecharging = true;
        yield return new WaitForSeconds(manaRechargeTime);
        currentMagicCharges = Mathf.Min(maxMagicCharges, currentMagicCharges + 1);
        isRecharging = false;
    }
    #endregion

    #region Utility
    private void PlayAudio(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    private void OnDrawGizmosSelected()
    {
        if (meleePoint == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(meleePoint.position, meleeRange);
    }
    #endregion
}