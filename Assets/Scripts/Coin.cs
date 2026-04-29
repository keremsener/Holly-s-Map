using UnityEngine;
using System.Collections;

/// <summary>
/// Toplanabilir altin coin.
/// Bobbing + spin + shimmer animasyonu yapar.
/// </summary>
public class Coin : MonoBehaviour
{
    [Header("Gorsel")]
    public float bobAmplitude  = 0.18f;
    public float bobSpeed      = 2.5f;
    public float spinSpeed     = 180f;

    [Header("Efekt")]
    public GameObject collectVFX;
    public AudioClip  collectSound;
    public float      shimmerInterval = 1.4f;

    // ── ic durum
    private Vector3        startPos;
    private bool           collected = false;
    private float          shimmerTimer;
    private SpriteRenderer sr;
    private CircleCollider2D col;

    private static readonly Color ColorBase    = new Color(1f, 0.82f, 0.1f);
    private static readonly Color ColorShimmer = new Color(1f, 1f,   0.65f);

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr.sprite == null)
        {
            var tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float dx = x - 15.5f; float dy = y - 15.5f;
                float d  = Mathf.Sqrt(dx*dx + dy*dy);
                float a  = Mathf.Clamp01(1f - (d - 14f));
                tex.SetPixel(x, y, new Color(1,1,1, a));
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0,0,32,32), new Vector2(0.5f,0.5f), 32f);
        }

        sr.color        = ColorBase;
        sr.sortingOrder = 5;

        col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.55f;

        startPos     = transform.position;
        shimmerTimer = Random.Range(0f, shimmerInterval);
    }

    private void Update()
    {
        if (collected) return;

        // Bob
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed + startPos.x) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Spin (scaleX ile yassılasma)
        float sx = Mathf.Abs(Mathf.Cos(Time.time * spinSpeed * Mathf.Deg2Rad));
        transform.localScale = new Vector3(Mathf.Max(sx, 0.05f), 1f, 1f);

        // Shimmer zamanlamasi
        shimmerTimer += Time.deltaTime;
        if (shimmerTimer >= shimmerInterval)
        {
            shimmerTimer = 0f;
            StartCoroutine(ShimmerEffect());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag("Player")) return;
        StartCoroutine(DoCollect());
    }

    private IEnumerator DoCollect()
    {
        collected = true;

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position, 0.8f);

        if (collectVFX != null)
            Instantiate(collectVFX, transform.position, Quaternion.identity);

        CoinManager.Instance?.CollectCoin();

        // Pop animasyonu
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1.4f, 0f, t / 0.22f);
            transform.localScale = new Vector3(s, s, 1f);
            if (sr != null) sr.color = new Color(1f, 1f, 0.8f, 1f - t / 0.22f);
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator ShimmerEffect()
    {
        if (sr == null || collected) yield break;
        float dur = 0.4f;
        float t   = 0f;
        while (t < dur && !collected)
        {
            t += Time.deltaTime;
            float p = Mathf.PingPong(t / dur * 2f, 1f);
            sr.color = Color.Lerp(ColorBase, ColorShimmer, p);
            yield return null;
        }
        if (sr != null && !collected) sr.color = ColorBase;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.55f);
    }
}
