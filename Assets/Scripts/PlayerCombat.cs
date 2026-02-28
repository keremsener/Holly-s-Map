using UnityEngine;
using System.Collections;

/// <summary>
/// Professional Player Combat System
/// Handles melee attacks, magic spells, cooldowns, and damage application
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    #region Serialized Fields
    [Header("═══ MELEE ATTACK (Left Click) ═══")]
    [SerializeField] private Transform meleePoint;
    [SerializeField] private float meleeRange = 1.5f;
    [SerializeField] private float meleeDamage = 40f;
    [SerializeField] private float meleeKnockback = 10f;
    [SerializeField] private float meleeCooldown = 0.8f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("═══ MAGIC ATTACK (F Key) ═══")]
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
    #endregion

    #region Private Variables
    private Animator animator;
    private AudioSource audioSource;
    private int currentMagicCharges;
    private bool isRecharging = false;
    private float lastMeleeTime = -1f;
    private Camera mainCamera;
    
    private int hashAttackTrigger;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        mainCamera = Camera.main;

        // Cache animator parameter hashes
        hashAttackTrigger = Animator.StringToHash("Attack");
    }

    private void Start()
    {
        currentMagicCharges = maxMagicCharges;
        ValidateReferences();
    }

    private void Update()
    {
        HandleMeleeInput();
        HandleMagicInput();
        HandleManaRecharge();
    }
    #endregion

    #region Input Handling
    private void HandleMeleeInput()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            // Cooldown check
            if (Time.time >= lastMeleeTime + meleeCooldown)
            {
                MeleeAttack();
                lastMeleeTime = Time.time;
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
                Debug.Log($"🔮 Büyü Kullanıldı! Kalan: {currentMagicCharges}/{maxMagicCharges}");
            }
            else
            {
                Debug.Log("❌ Mana bitti! Yenilenmesini bekle...");
            }
        }
    }

    private void HandleManaRecharge()
    {
        if (currentMagicCharges < maxMagicCharges && !isRecharging)
        {
            StartCoroutine(RechargeMana());
        }
    }
    #endregion

    #region Combat Methods
    private void MeleeAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger(hashAttackTrigger);
        }

        // Slash VFX
        if (slashVFXPrefab != null)
        {
            Instantiate(slashVFXPrefab, meleePoint.position, meleePoint.rotation);
        }

        // Slash SFX
        PlayAudio(slashSFX);

        // Find all enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(meleePoint.position, meleeRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Calculate knockback direction
            Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;

            // Deal damage
            var enemyHealth = enemy.GetComponent<IHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(meleeDamage);
                
                // Camera shake & red flash (Kılıç vuruşu efekti)
                if (CameraEffects.Instance != null)
                {
                    CameraEffects.Instance.MeleeHitEffect();
                }
            }

            // Apply knockback
            var rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(knockbackDirection * meleeKnockback, ForceMode2D.Impulse);
            }

            // Hit SFX
            PlayAudio(hitSFX);

            // Blood VFX with random height
            if (bloodBurstPrefab != null)
            {
                float randomY = Random.Range(0.8f, 1.4f);
                Vector3 bloodPosition = enemy.transform.position + new Vector3(0, randomY, 0);
                Instantiate(bloodBurstPrefab, bloodPosition, Quaternion.identity);
            }
        }

        Debug.Log($"⚔️ Kılıç Saldırısı! {hitEnemies.Length} düşmana çarptı");
    }

    private void ShootMagic()
    {
        if (animator != null)
        {
            animator.SetTrigger(hashAttackTrigger);
        }

        if (magicPrefab != null)
        {
            Instantiate(magicPrefab, firePoint.position, firePoint.rotation);
        }

        PlayAudio(magicSFX);
    }
    #endregion

    #region Cooldown System
    private IEnumerator RechargeMana()
    {
        isRecharging = true;
        yield return new WaitForSeconds(manaRechargeTime);
        currentMagicCharges++;
        Debug.Log($"🔄 Mana Yenilendi! {currentMagicCharges}/{maxMagicCharges}");
        isRecharging = false;
    }
    #endregion

    #region Utility
    private void PlayAudio(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void ValidateReferences()
    {
        if (meleePoint == null) Debug.LogWarning("⚠️ Melee Point not assigned!");
        if (firePoint == null) Debug.LogWarning("⚠️ Fire Point not assigned!");
        if (magicPrefab == null) Debug.LogWarning("⚠️ Magic Prefab not assigned!");
    }

    private void OnDrawGizmosSelected()
    {
        if (meleePoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(meleePoint.position, meleeRange);
    }
    #endregion
}