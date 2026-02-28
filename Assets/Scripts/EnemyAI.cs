using UnityEngine;
using System.Collections;

/// <summary>
/// Professional Enemy AI System with State Machine
/// Implements patrol, detection, chase, combat, and return-to-home mechanics
/// </summary>
public class EnemyAI : MonoBehaviour
{
    #region State Enum
    private enum EnemyState
    {
        Idle,           // Görev yerinde beklemek
        Chasing,        // Oyuncuyu kovalaşmak
        Combat,         // Oyuncuyla savaşmak
        Returning,      // Eve dönmek
        Stunned         // Dönerken hasar alırsa
    }
    #endregion

    #region Inspector Settings
    [Header("═══ DETECTION & RANGE ═══")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float maxChaseDistance = 15f;
    [SerializeField] private float combatRange = 1.5f;
    [SerializeField] private float stoppingDistance = 0.3f;

    [Header("═══ MOVEMENT SPEEDS ═══")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float returnSpeed = 2f;

    [Header("═══ COMBAT SETTINGS ═══")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackWindupTime = 0.3f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float knockbackForce = 5f;

    [Header("═══ PATROL SETTINGS ═══")]
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("═══ VISUAL & AUDIO ═══")]
    [SerializeField] private Color alertColor = Color.red;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private bool showDebugInfo = true;
    #endregion

    #region Private Variables
    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;

    // Positions
    private Vector2 homePosition;
    private Vector2 currentPatrolTarget;

    // State Management
    private EnemyState currentState = EnemyState.Idle;
    private EnemyState previousState;
    private float stateTimer = 0f;

    // Combat
    private float lastAttackTime = 0f;
    private float currentHealth;
    private bool isAttacking = false;

    // Movement
    private bool isFacingRight = true;
    private Vector2 moveDirection = Vector2.zero;
    private float speedMultiplier = 1f; // Bacak kopunca hız düşürülür

    // Animator Hash
    private int hashWalking;
    private int hashAttack;
    private int hashTakeDamage;
    private int hashDying;
    private int hashTaunt;
    private int hashJumpStart;
    private int hashJumpLoop;
    #endregion

    #region Lifecycle Methods
    private void Awake()
    {
        InitializeComponents();
        CacheAnimatorHashes();
    }

    private void Start()
    {
        homePosition = transform.position;
        currentHealth = maxHealth;
        SetPatrolTarget();

        // Player bulma
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerTransform = playerGO.transform;
        }
        else
        {
            Debug.LogError("❌ Player GameObject 'Player' tag ile bulunamamıştır!");
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        float distanceToHome = Vector2.Distance(transform.position, homePosition);

        // State Machine
        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle(distanceToPlayer);
                break;

            case EnemyState.Chasing:
                UpdateChasing(distanceToPlayer, distanceToHome);
                break;

            case EnemyState.Combat:
                UpdateCombat(distanceToPlayer, distanceToHome);
                break;

            case EnemyState.Returning:
                UpdateReturning(distanceToHome, distanceToPlayer);
                break;

            case EnemyState.Stunned:
                UpdateStunned();
                break;
        }

        UpdateAnimation();
        DebugInfo(distanceToPlayer, distanceToHome);
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }
    #endregion

    #region State Update Methods
    private void UpdateIdle(float distanceToPlayer)
    {
        // Oyuncu menzile girdi mi?
        if (distanceToPlayer < detectionRange)
        {
            ChangeState(EnemyState.Chasing);
            return;
        }

        // Patrol hareket
        stateTimer += Time.deltaTime;
        if (stateTimer > patrolWaitTime)
        {
            MoveTowards(currentPatrolTarget, patrolSpeed);
            
            // Hedefine ulaştı mı?
            if (Vector2.Distance(transform.position, currentPatrolTarget) < stoppingDistance)
            {
                SetPatrolTarget();
                stateTimer = 0f;
            }
        }
        else
        {
            StopMovement();
            // Beklerken random göz kırpma veya taunt yap
            if (stateTimer > patrolWaitTime * 0.7f && Random.value > 0.8f)
            {
                animator.SetTrigger(hashTaunt);
            }
        }
    }

    private void UpdateChasing(float distanceToPlayer, float distanceToHome)
    {
        // Oyuncu çok uzaklaştı veya çok ileri gitti mi? → Dön
        if (distanceToPlayer > maxChaseDistance || distanceToHome > maxChaseDistance)
        {
            ChangeState(EnemyState.Returning);
            return;
        }

        // Saldırı menzilinde mi? → Combat'e geç
        if (distanceToPlayer <= combatRange && CanSeePlayer())
        {
            ChangeState(EnemyState.Combat);
            return;
        }

        // Oyuncu kaybedildi mi? → Eve dön
        if (!CanSeePlayer() && stateTimer > 3f)
        {
            ChangeState(EnemyState.Returning);
            return;
        }

        stateTimer += Time.deltaTime;
        MoveTowards(playerTransform.position, chaseSpeed);
        spriteRenderer.color = alertColor;
    }

    private void UpdateCombat(float distanceToPlayer, float distanceToHome)
    {
        // Oyuncu kaçtı mi?
        if (distanceToPlayer > combatRange || !CanSeePlayer())
        {
            // Evden çok uzaksa eve dön
            if (distanceToHome > maxChaseDistance)
            {
                ChangeState(EnemyState.Returning);
            }
            else
            {
                ChangeState(EnemyState.Chasing);
            }
            return;
        }

        // Oyuncuya dönük kal
        FaceTarget(playerTransform.position);

        // Saldırıya hazırlan
        if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
        {
            StartCoroutine(ExecuteAttack());
        }
    }

    private void UpdateReturning(float distanceToHome, float distanceToPlayer)
    {
        // Eve varıp ve oyuncu uzak mı?
        if (distanceToHome < stoppingDistance && distanceToPlayer > detectionRange)
        {
            ChangeState(EnemyState.Idle);
            spriteRenderer.color = idleColor;
            return;
        }

        // Oyuncu menzile girdi?
        if (distanceToPlayer < detectionRange && CanSeePlayer())
        {
            ChangeState(EnemyState.Chasing);
            return;
        }

        // Eve doğru yürü
        MoveTowards(homePosition, returnSpeed);
    }

    private void UpdateStunned()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer > 1.5f)
        {
            ChangeState(EnemyState.Returning);
        }
    }
    #endregion

    #region Combat System
    private IEnumerator ExecuteAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Attacking animasyonunu başlat
        animator.SetTrigger(hashAttack);
        
        // Saldırı öncesi küçük sıçrama efekti
        animator.SetTrigger(hashJumpStart);
        
        yield return new WaitForSeconds(attackWindupTime);

        // Hasar ver (oyuncu kolayca damage almak için collision kontrol et)
        if (Vector2.Distance(transform.position, playerTransform.position) <= combatRange * 1.2f)
        {
            // IHealth interface'i varsa kullan
            var playerHealth = playerTransform.GetComponent<IHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(attackCooldown - attackWindupTime);
        isAttacking = false;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        animator.SetTrigger(hashTakeDamage);

        // Küçük knockback
        rb.linearVelocity = (transform.position - playerTransform.position).normalized * knockbackForce;

        // Hasar alırken returning'e geç
        if (currentState != EnemyState.Returning)
        {
            ChangeState(EnemyState.Stunned);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        animator.SetBool(hashWalking, false);
        animator.SetTrigger(hashDying);
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        enabled = false;
        
        // Ölüm animasyonu
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
    #endregion

    #region Movement & Facing
    private void MoveTowards(Vector2 target, float speed)
    {
        moveDirection = (target - (Vector2)transform.position).normalized;
        
        if (moveDirection.magnitude > 0.01f)
        {
            FaceTarget(target);
        }

        // Bacak kopunca hız düşsün
        float finalSpeed = speed * speedMultiplier;
        rb.linearVelocity = new Vector2(moveDirection.x * finalSpeed, rb.linearVelocity.y);
    }

    private void StopMovement()
    {
        animator.SetBool(hashWalking, false);
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        moveDirection = Vector2.zero;
    }

    private void FaceTarget(Vector2 target)
    {
        float directionX = target.x - transform.position.x;

        if (directionX > 0.01f && !isFacingRight)
        {
            Flip();
        }
        else if (directionX < -0.01f && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void ApplyMovement()
    {
        // Gravity halen uygulanıyor
        if (currentState != EnemyState.Stunned)
        {
            // Velocity'yi koru, gravity otomatik uygulanıyor
        }
    }
    #endregion

    #region Utility Methods
    private bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Raycast ile oyuncuya doğru görüş kontrolü
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer);

        if (hit.collider != null && hit.transform == playerTransform)
        {
            return true;
        }

        return distanceToPlayer < detectionRange * 0.5f; // Çok yakınsa direkt görülebilir
    }

    private void SetPatrolTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        currentPatrolTarget = homePosition + randomDirection * patrolRadius;
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        OnStateChanged(newState);
    }

    private void OnStateChanged(EnemyState newState)
    {
        switch (newState)
        {
            case EnemyState.Idle:
                animator.SetBool(hashWalking, false);
                spriteRenderer.color = idleColor;
                break;

            case EnemyState.Chasing:
                animator.SetBool(hashWalking, true);
                spriteRenderer.color = alertColor;
                // Taunt trigger'ını random şekilde çalıştır (ezici görünüş için)
                if (Random.value > 0.7f)
                {
                    animator.SetTrigger(hashTaunt);
                }
                break;

            case EnemyState.Combat:
                animator.SetBool(hashWalking, false);
                break;

            case EnemyState.Returning:
                animator.SetBool(hashWalking, true);
                break;

            case EnemyState.Stunned:
                StopMovement();
                animator.SetBool(hashWalking, false);
                animator.SetTrigger(hashTakeDamage);
                break;
        }
    }

    private void UpdateAnimation()
    {
        // Animator parametreleri state'de güncellenmiş
    }

    #region Limb Damage Effects
    /// <summary>
    /// Bacak kaybı etkileri - hız düşürülür ve sendeleme başlar
    /// </summary>
    public void ApplyLimbDamageEffect(float newSpeedMultiplier, bool applyStagger)
    {
        speedMultiplier = newSpeedMultiplier;
        
        if (applyStagger)
        {
            StartCoroutine(StaggerMovement());
        }
        
        Debug.Log($"🦵 {gameObject.name} bacak kaybı efekti: Hız {newSpeedMultiplier}x oldu!");
    }

    /// <summary>
    /// Kol kopunca saldırı devre dışı bırak
    /// </summary>
    public void DisableAttacks()
    {
        // Saldırıyı blokla
        isAttacking = true;
        StopAllCoroutines(); // Devam eden ExecuteAttack'i durdur
        
        // Kalıcı olarak disabled tut
        StartCoroutine(KeepAttacksDisabled());
    }

    /// <summary>
    /// Bacaklar kopunca sendeleme animasyonu
    /// </summary>
    private IEnumerator StaggerMovement()
    {
        Debug.Log($"🤪 {gameObject.name} sendeliyor!");
        
        for (int i = 0; i < 8; i++) // 8 frame sendeleme
        {
            if (rb != null && moveDirection.magnitude > 0)
            {
                // Random hareket sapması
                float staggerX = Random.Range(-0.2f, 0.2f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x + staggerX, rb.linearVelocity.y);
            }
            yield return null;
        }
    }

    /// <summary>
    /// Kollar kopunca saldırıyı kalıcı disable et
    /// </summary>
    private IEnumerator KeepAttacksDisabled()
    {
        Debug.Log($"🚫 {gameObject.name} saldırılamıyor - kollar yok!");
        
        while (true)
        {
            isAttacking = true; // Hep attacking durumunda tut saldırı yapmasın
            yield return new WaitForSeconds(0.1f);
        }
    }
    #endregion

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb == null) Debug.LogError($"❌ {gameObject.name}: Rigidbody2D bulunamadı!");
        if (animator == null) Debug.LogError($"❌ {gameObject.name}: Animator bulunamadı!");
        if (spriteRenderer == null) Debug.LogError($"❌ {gameObject.name}: SpriteRenderer bulunamadı!");
    }

    private void CacheAnimatorHashes()
    {
        hashWalking = Animator.StringToHash("Walking");
        hashAttack = Animator.StringToHash("Attacking");
        hashTakeDamage = Animator.StringToHash("Hurt");
        hashDying = Animator.StringToHash("Dying");
        hashTaunt = Animator.StringToHash("Taunt");
        hashJumpStart = Animator.StringToHash("Jump Start");
        hashJumpLoop = Animator.StringToHash("Jump Loop");
    }
    #endregion

    #region Debug
    private void DebugInfo(float distanceToPlayer, float distanceToHome)
    {
        if (!showDebugInfo) return;

        Debug.Log($"🤖 {gameObject.name} | State: {currentState} | HP: {currentHealth:F0}/{maxHealth} | " +
                  $"Dist to Player: {distanceToPlayer:F1} | Dist to Home: {distanceToHome:F1}");
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Detection range
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Combat range
        Gizmos.color = new Color(1, 1, 0, 0.5f);
        Gizmos.DrawWireSphere(transform.position, combatRange);

        // Home position
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(homePosition, homePosition + Vector2.up * 0.5f);
    }
    #endregion
}

/// <summary>
/// Interface for health system - Player'a implement et
/// </summary>
public interface IHealth
{
    void TakeDamage(float damage);
}
