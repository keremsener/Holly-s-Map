using UnityEngine;

/// <summary>
/// Oyuncu ilk kez bir trigger bölgesine girince HintDisplay üzerinden ipucu gösterir.
/// Bir kere gösterildikten sonra bir daha tetiklenmez.
/// </summary>
public class HintZoneTrigger : MonoBehaviour
{
    [Header("Hint Content")]
    public string line1 = "";
    public string line2 = "";
    public float duration = 5f;

    [Header("Optional: only after enemy dies")]
    public GameObject requiredEnemyDead;

    private bool shown = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (shown) return;
        if (!other.CompareTag("Player")) return;
        if (requiredEnemyDead != null && requiredEnemyDead.activeInHierarchy) return;

        shown = true;
        HintDisplay.Instance?.Show(line1, line2, Color.white, 20, duration, 0.6f, 1f);
        // Kendini devre dışı bırak
        enabled = false;
    }
}
