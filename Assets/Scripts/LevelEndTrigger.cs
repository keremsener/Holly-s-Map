using UnityEngine;
using System.Collections;

/// <summary>
/// Bolumun son noktasi. Oyuncu buraya girince CoinManager.TryCompleteLevel() cagirilir.
/// Tum coinler toplandiysa Main Menu, degilse sahne basadan baslar.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelEndTrigger : MonoBehaviour
{
    [Header("Gorunum")]
    public Color gizmoColor = new Color(0.1f, 1f, 0.4f, 0.35f);

    [Header("Efekt")]
    public AudioClip winSound;
    public AudioClip failSound;

    private bool triggered = false;
    private SpriteRenderer sr;

    private static readonly Color ColorIdle   = new Color(0.2f, 0.9f, 0.3f, 0.7f);
    private static readonly Color ColorPulse  = new Color(0.6f, 1f,   0.7f, 1f);

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Gorsel kutu
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr.sprite == null)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite    = Sprite.Create(tex, new Rect(0,0,1,1), new Vector2(0.5f,0.5f), 1f);
            sr.drawMode  = SpriteDrawMode.Tiled;
            var bc = GetComponent<BoxCollider2D>();
            if (bc != null) sr.size = bc.size;
        }

        sr.color        = ColorIdle;
        sr.sortingOrder = 4;

        StartCoroutine(PulseLoop());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        int collected = CoinManager.Instance != null ? CoinManager.Instance.Collected : 0;
        int total     = CoinManager.Instance != null ? CoinManager.Instance.Total     : 20;

        if (collected >= total)
        {
            if (winSound  != null) AudioSource.PlayClipAtPoint(winSound,  transform.position, 1f);
            HintDisplay.Instance?.Show("Tebrikler!", "Tum coinleri topladın — Bolum Tamamlandi! 🏆", Color.yellow, 24, 2f, 0.3f, 0.5f);
        }
        else
        {
            if (failSound != null) AudioSource.PlayClipAtPoint(failSound, transform.position, 1f);
            int rem = total - collected;
            HintDisplay.Instance?.Show("Bolum Tamamlanamadi!", $"{rem} coin daha toplamaliydin!", Color.red, 22, 2f, 0.3f, 0.8f);
        }

        CoinManager.Instance?.TryCompleteLevel();
    }

    private IEnumerator PulseLoop()
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.4f;
                if (sr != null)
                    sr.color = Color.Lerp(ColorIdle, ColorPulse, Mathf.PingPong(t * 2f, 1f));
                yield return null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, new Vector3(2f, 3f, 0f));
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(2f, 3f, 0f));
    }
}
