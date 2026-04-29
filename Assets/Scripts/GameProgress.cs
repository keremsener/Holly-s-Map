using UnityEngine;

/// <summary>
/// Hangi bolumun acik/kilitli oldugunu PlayerPrefs'e kaydeder.
/// Singleton — sahneler arasinda yasam surer.
/// </summary>
public class GameProgress : MonoBehaviour
{
    public static GameProgress Instance { get; private set; }

    private const string KEY_UNLOCKED = "UnlockedLevelIndex";
    public const int TOTAL_LEVELS = 4;

    private int _unlockedUpTo = 0;
    public int UnlockedUpTo => _unlockedUpTo;

    // ── Bolum meta verileri ─────────────────────────────────────
    public static readonly string[] LevelNames = { "Misir", "Istanbul", "Roma", "Cin" };
    public static readonly string[] SceneNames = { "Level_01", "Istanbul", "Rome", "China" };
    public static readonly string[] SubTitles  =
    {
        "Antik Firavunlarin Izinde",
        "Iki Kitanin Kesisiminde",
        "Colosseum'un Golgesinde",
        "Ejderhanin Diyarinda"
    };
    public static readonly Color[] LevelColors =
    {
        new Color(1.00f, 0.78f, 0.10f),
        new Color(0.20f, 0.60f, 1.00f),
        new Color(0.85f, 0.30f, 0.10f),
        new Color(0.85f, 0.10f, 0.15f),
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProgress();
    }

    private void LoadProgress()
    {
        _unlockedUpTo = PlayerPrefs.GetInt(KEY_UNLOCKED, 0);
        Debug.Log("[GameProgress] Acik bolum: " + _unlockedUpTo + " (" + LevelNames[_unlockedUpTo] + ")");
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(KEY_UNLOCKED, _unlockedUpTo);
        PlayerPrefs.Save();
    }

    public bool IsUnlocked(int levelIndex)
    {
        return levelIndex <= _unlockedUpTo;
    }

    /// <summary>Bolum tamamlandi — bir sonrakini ac.</summary>
    public void CompleteLevel(int levelIndex)
    {
        if (levelIndex >= _unlockedUpTo)
        {
            _unlockedUpTo = Mathf.Min(levelIndex + 1, TOTAL_LEVELS - 1);
            SaveProgress();
            Debug.Log("[GameProgress] Bolum " + levelIndex + " tamamlandi! Acilan: " + LevelNames[_unlockedUpTo]);
        }
    }

    public void ResetProgress()
    {
        _unlockedUpTo = 0;
        SaveProgress();
    }

    public void UnlockAll()
    {
        _unlockedUpTo = TOTAL_LEVELS - 1;
        SaveProgress();
    }
}
