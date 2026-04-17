using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Oyun durumu yöneticisi.
/// Oyuncu öldüğünde tüm düşmanları orijinal konumlarına respawn eder,
/// checkpoint sistemini koordine eder.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Tüm düşman spawn verilerini tutan liste
    private List<EnemySpawnData> enemySpawnList = new List<EnemySpawnData>();

    [Header("Respawn Efekti")]
    [SerializeField] private float enemyRespawnDelay = 0.5f;  // Oyuncu respawn olduktan bu kadar sonra düşmanlar geri döner

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Sahnedeki tüm düşmanları kayıt et
        RegisterAllEnemies();
    }

    // ─────────────────────────────────────────────────────────────
    //  ENEMY KAYIT
    // ─────────────────────────────────────────────────────────────
    private void RegisterAllEnemies()
    {
        enemySpawnList.Clear();
        var enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var ai in enemies)
        {
            var health = ai.GetComponent<EnemyHealth>();
            enemySpawnList.Add(new EnemySpawnData
            {
                gameObject  = ai.gameObject,
                prefabName  = ai.gameObject.name,
                spawnPosition = ai.transform.position,
                spawnRotation = ai.transform.rotation,
                spawnScale    = ai.transform.localScale,
                originalRbType = ai.GetComponent<Rigidbody2D>()?.bodyType ?? RigidbodyType2D.Dynamic
            });
        }
        Debug.Log($"🗂️ GameManager: {enemySpawnList.Count} düşman kayıt edildi.");
    }

    // ─────────────────────────────────────────────────────────────
    //  OYUNCU ÖLDÜĞÜNde ÇAĞRILIR
    // ─────────────────────────────────────────────────────────────
    public void OnPlayerDied()
    {
        StartCoroutine(ResetEnemiesAfterDelay());
    }

    private IEnumerator ResetEnemiesAfterDelay()
    {
        yield return new WaitForSeconds(enemyRespawnDelay);
        ResetAllEnemies();
    }

    private void ResetAllEnemies()
    {
        foreach (var data in enemySpawnList)
        {
            if (data.gameObject == null)
            {
                // Düşman destroy edilmişse — şu an için log, prefab sistemi sonra eklenebilir
                Debug.LogWarning($"⚠️ '{data.prefabName}' destroy edilmiş, respawn atlanıyor.");
                continue;
            }

            var go = data.gameObject;

            // Pozisyon & rotasyon sıfırla
            go.transform.position   = data.spawnPosition;
            go.transform.rotation   = data.spawnRotation;
            go.transform.localScale = data.spawnScale;

            // Rigidbody sıfırla
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType       = data.originalRbType;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            // EnemyHealth sıfırla
            var health = go.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.ResetHealth();
            }

            // EnemyAI sıfırla (Home pozisyonunu güncelle)
            var ai = go.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.ResetToHome(data.spawnPosition);
            }

            // PlatformGuardian varsa pozisyonu kilitle
            var guardian = go.GetComponent<PlatformGuardian>();
            if (guardian != null)
            {
                guardian.ResetLockPosition(data.spawnPosition);
            }

            // GameObject'i aktif et (ölünce devre dışı kalmışsa)
            go.SetActive(true);

            // Script'i tekrar etkinleştir
            var aiScript = go.GetComponent<EnemyAI>();
            if (aiScript != null) aiScript.enabled = true;

            Debug.Log($"♻️ {go.name} respawn: {data.spawnPosition}");
        }
    }

    // Yeni düşmanlar eklendiğinde (çalışma zamanında) manuel kayıt
    public void RegisterEnemy(GameObject enemy)
    {
        var ai = enemy.GetComponent<EnemyAI>();
        if (ai == null) return;

        enemySpawnList.Add(new EnemySpawnData
        {
            gameObject    = enemy,
            prefabName    = enemy.name,
            spawnPosition = enemy.transform.position,
            spawnRotation = enemy.transform.rotation,
            spawnScale    = enemy.transform.localScale,
            originalRbType = enemy.GetComponent<Rigidbody2D>()?.bodyType ?? RigidbodyType2D.Dynamic
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  VERİ YAPISI
    // ─────────────────────────────────────────────────────────────
    private class EnemySpawnData
    {
        public GameObject      gameObject;
        public string          prefabName;
        public Vector3         spawnPosition;
        public Quaternion      spawnRotation;
        public Vector3         spawnScale;
        public RigidbodyType2D originalRbType;
    }
}
