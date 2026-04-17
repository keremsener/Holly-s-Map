using UnityEngine;
using System.Reflection;

[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlatformGuardian : MonoBehaviour
{
    [Header("Guardian Settings")]
    [SerializeField] private float guardDetectionRange = 30f;
    [SerializeField] private float maxLeashDistance = 1.5f;
    [SerializeField] private float guardAttackCooldown = 0.4f;
    [SerializeField] private float guardDamage = 9999f;
    [SerializeField] private float guardCombatRange = 2f;
    [SerializeField] private bool lockPosition = true;

    private Rigidbody2D rb;
    private EnemyAI ai;
    private Vector3 lockedPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ai = GetComponent<EnemyAI>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        ApplyGuardianConfig();
        lockedPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (lockPosition)
            transform.position = lockedPosition;
    }

    private void ApplyGuardianConfig()
    {
        if (ai == null) return;
        var aiType = ai.GetType();
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        PatchFloat(aiType, flags, "detectionRange",   guardDetectionRange);
        PatchFloat(aiType, flags, "maxChaseDistance",  maxLeashDistance);
        PatchFloat(aiType, flags, "patrolRadius",      0f);
        PatchFloat(aiType, flags, "patrolSpeed",       0f);
        PatchFloat(aiType, flags, "attackDamage",      guardDamage);
        PatchFloat(aiType, flags, "attackCooldown",    guardAttackCooldown);
        PatchFloat(aiType, flags, "combatRange",       guardCombatRange);
        PatchFloat(aiType, flags, "chaseSpeed",        0f);
        PatchFloat(aiType, flags, "returnSpeed",       0f);
        PatchBool(aiType,  flags, "isInstaKillOnContact", true);
    }

    private void PatchFloat(System.Type type, BindingFlags flags, string fieldName, float value)
    {
        var field = type.GetField(fieldName, flags);
        if (field != null) field.SetValue(ai, value);
    }

    private void PatchBool(System.Type type, BindingFlags flags, string fieldName, bool value)
    {
        var field = type.GetField(fieldName, flags);
        if (field != null) field.SetValue(ai, value);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, guardCombatRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, guardDetectionRange);
    }
}
