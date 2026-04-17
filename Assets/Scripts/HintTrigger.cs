using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Oyuncu bu trigger bölgesine girince ekranda ipucu mesajı gösterir.
/// </summary>
public class HintTrigger : MonoBehaviour
{
    [Header("Mesaj")]
    [TextArea(1, 3)]
    public string hintLine1 = "Aşağı atla,";
    [TextArea(1, 3)]
    public string hintLine2 = "bana güven.";

    [Header("Zamanlama")]
    public float displayDuration = 4f;
    public float fadeInTime      = 0.5f;
    public float fadeOutTime     = 1f;
    public bool  oneTimeOnly     = true;

    [Header("Görünüm")]
    public Color textColor  = new Color(1f, 0.92f, 0.55f, 1f);
    public int   fontSize   = 52;

    private bool triggered  = false;

    private void Start()
    {
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var bc = gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(4f, 4f);
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (oneTimeOnly && triggered) return;
        triggered = true;

        HintDisplay.Instance.Show(hintLine1, hintLine2, textColor, fontSize,
                                   displayDuration, fadeInTime, fadeOutTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
        else
            Gizmos.DrawWireCube(transform.position, new Vector3(4f, 4f, 0f));
    }
}
