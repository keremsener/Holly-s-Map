using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Tüm coin toplama mantığını yönetir.
/// Singleton — sahneler arasında geçişte de kullanılabilir.
/// </summary>
public class CoinManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────
    public static CoinManager Instance { get; private set; }

    // ── Ayarlar ────────────────────────────────────────────────
    [Header("Coin Hedefi")]
    [Tooltip("Bölümü tamamlamak için gereken toplam coin sayısı")]
    public int totalCoins = 20;

    [Header("Sahneler")]
    [Tooltip("Oyun bitince hangi sahne yüklensin? (Build Settings'e ekli olmalı)")]
    public string mainMenuScene = "MainMenu";
    public string levelScene    = "Level_01";

    // ── Durum ──────────────────────────────────────────────────
    private int collected = 0;
    public  int Collected => collected;
    public  int Total     => totalCoins;
    public  float Progress => totalCoins > 0 ? (float)collected / totalCoins : 0f;

    // ── Events ─────────────────────────────────────────────────
    public event System.Action<int, int> OnCoinCollected;   // (collected, total)
    public event System.Action OnLevelComplete;
    public event System.Action OnLevelFailed;

    // ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ─────────────────────────────────────────────

    /// <summary>Bir coin toplandığında çağrılır.</summary>
    public void CollectCoin()
    {
        if (collected >= totalCoins) return;
        collected++;
        Debug.Log($"🪙 Coin toplandı: {collected}/{totalCoins}");
        OnCoinCollected?.Invoke(collected, totalCoins);
    }

    /// <summary>
    /// Bölüm sonu tetikleyicisi bu metodu çağırır.
    /// Tüm coinler toplandıysa → Main Menu
    /// Toplandıysa → Sahneyi yeniden yükle (sıfırdan)
    /// </summary>
    public void TryCompleteLevel()
    {
        if (collected >= totalCoins)
        {
            Debug.Log("🏆 Tüm coinler toplandı! Bölüm tamamlandı!");
            OnLevelComplete?.Invoke();
            StartCoroutine(GoToMainMenu());
        }
        else
        {
            int remaining = totalCoins - collected;
            string msg = $"{remaining} coin daha topla!";
            HintDisplay.Instance?.Show("Bölüm Tamamlanamadı!", msg, Color.red, 22, 2f, 0.3f, 0.8f);
            Debug.Log($"❌ Bölüm başarısız — Eksik coin: {remaining}");
            OnLevelFailed?.Invoke();
            StartCoroutine(RestartLevel());
        }
    }

    /// <summary>Coin sayacını sıfırlar (sahne yeniden yüklendiğinde Awake'de çalışır).</summary>
    public void ResetCoins()
    {
        collected = 0;
    }

    // ── Coroutines ─────────────────────────────────────────────
    private IEnumerator GoToMainMenu()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(mainMenuScene);
    }

    private IEnumerator RestartLevel()
    {
        yield return new WaitForSeconds(2.0f);
        collected = 0;
        SceneManager.LoadScene(levelScene);
    }
}
