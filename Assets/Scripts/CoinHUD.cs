using UnityEngine;
using System.Collections;

/// <summary>
/// Altin parlayan coin sayaci HUD.
/// OnGUI ile cizilir — Canvas gerektirmez.
/// </summary>
public class CoinHUD : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────
    public static CoinHUD Instance { get; private set; }

    // ── Ayarlar ────────────────────────────────────────────────
    [Header("Konum (ekranin sol-ust kosesi)")]
    public float marginX    = 24f;
    public float marginY    = 24f;
    public float barWidth   = 220f;
    public float barHeight  = 22f;

    [Header("Renkler")]
    public Color barBgColor   = new Color(0.08f, 0.06f, 0.02f, 0.82f);
    public Color barFillColor = new Color(1f, 0.78f, 0.1f, 1f);
    public Color barShimmer   = new Color(1f, 1f,   0.55f, 1f);
    public Color textColor    = new Color(1f, 0.92f, 0.3f, 1f);

    // ── Ic durum ───────────────────────────────────────────────
    private int   displayed   = 0;    // su an gosterilen sayi (lerp icin)
    private float fillLerp    = 0f;   // smooth fill progress
    private float shimmerTime = 0f;   // parlamanin zamani
    private float shimmerAlpha= 0f;   // parlama yogunlugu
    private float pulseScale  = 1f;   // coin simgesinin zoom efekti
    private bool  isShimmering= false;

    // Dokulari bir kez olustur
    private Texture2D texWhite;
    private Texture2D texRound;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        texWhite = MakeTex(1, 1, Color.white);
        texRound = MakeRoundedTex(64, 64, 12f);

        // CoinManager eventine baglan
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinCollected += HandleCoinCollected;
    }

    private void OnEnable()
    {
        // Gecikme olmadan da baglanti saglayabilmek icin tekrar dene
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinCollected += HandleCoinCollected;
    }

    private void OnDisable()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinCollected -= HandleCoinCollected;
    }

    private void HandleCoinCollected(int col, int total)
    {
        displayed = col;
        StartCoroutine(TriggerShimmer());
        StartCoroutine(PulseIcon());
    }

    private void Update()
    {
        float target = CoinManager.Instance != null ? CoinManager.Instance.Progress : 0f;
        fillLerp = Mathf.Lerp(fillLerp, target, Time.deltaTime * 7f);

        shimmerTime  += Time.deltaTime;
        shimmerAlpha  = Mathf.Lerp(shimmerAlpha, 0f, Time.deltaTime * 3f);
    }

    private void OnGUI()
    {
        int total     = CoinManager.Instance != null ? CoinManager.Instance.Total     : 20;
        int collected = CoinManager.Instance != null ? CoinManager.Instance.Collected : 0;

        float sw = Screen.width;
        float sh = Screen.height;

        // Ekran boyutuna gore olcekle
        float scale   = sh / 600f;
        float bw      = barWidth  * scale;
        float bh      = barHeight * scale;
        float mx      = marginX   * scale;
        float my      = marginY   * scale;
        float padding = 6f * scale;

        // Panel arka plani
        float panelW = bw + 70f * scale;
        float panelH = bh + 46f * scale;
        float panelX = mx;
        float panelY = my;

        DrawRoundedBox(panelX - padding, panelY - padding,
                       panelW + padding * 2f, panelH + padding * 2f,
                       new Color(0.05f, 0.04f, 0.01f, 0.78f));

        // ── Baslik ──────────────────────────────────────────────
        var titleStyle = new GUIStyle
        {
            fontStyle = FontStyle.Bold,
            fontSize  = Mathf.RoundToInt(12f * scale),
            alignment = TextAnchor.MiddleLeft,
        };
        titleStyle.normal.textColor = new Color(1f, 0.9f, 0.4f, 0.9f);
        GUI.Label(new Rect(panelX, panelY, panelW, 20f * scale), "  COINLER", titleStyle);

        float barY = panelY + 22f * scale;

        // ── Coin simgesi (kucuk daire + puls) ───────────────────
        float iconSize = bh * pulseScale;
        float iconX    = panelX;
        float iconY    = barY + (bh - iconSize) * 0.5f;
        DrawCoinIcon(iconX, iconY, iconSize);
        float afterIcon = panelX + iconSize + 6f * scale;

        // ── Bar arkaplan ─────────────────────────────────────────
        DrawRoundedBox(afterIcon, barY, bw, bh, barBgColor);

        // ── Bar dolma anim ────────────────────────────────────────
        float fillW = bw * fillLerp;
        if (fillW > 2f)
        {
            // Altin gradient: sol koyu, sag parlak
            DrawBarGradient(afterIcon, barY, fillW, bh, fillLerp);

            // Shimmer surmesi (parlak beyaz sweep)
            if (shimmerAlpha > 0.01f)
            {
                float sweepX = afterIcon + fillW * ((shimmerTime % 0.6f) / 0.6f);
                float sweepW = 18f * scale;
                GUI.color    = new Color(1f, 1f, 1f, shimmerAlpha * 0.55f);
                if (texWhite != null) GUI.DrawTexture(new Rect(sweepX, barY, sweepW, bh), texWhite);
                GUI.color = Color.white;
            }
        }

        // ── Yuzde yazisi bar icinde ───────────────────────────────
        int pct = Mathf.RoundToInt(fillLerp * 100f);
        var pctStyle = new GUIStyle
        {
            fontStyle = FontStyle.Bold,
            fontSize  = Mathf.RoundToInt(10f * scale),
            alignment = TextAnchor.MiddleCenter,
        };
        pctStyle.normal.textColor = Color.white;
        if (fillW > 30f)
            GUI.Label(new Rect(afterIcon, barY, fillW, bh), pct + "%", pctStyle);

        // ── Sayi yazisi (x/y) ─────────────────────────────────────
        float labelY = barY + bh + 4f * scale;
        var countStyle = new GUIStyle
        {
            fontStyle = FontStyle.Bold,
            fontSize  = Mathf.RoundToInt(13f * scale),
            alignment = TextAnchor.MiddleLeft,
        };
        // Golge
        countStyle.normal.textColor = new Color(0.1f, 0.06f, 0f, 0.9f);
        GUI.Label(new Rect(afterIcon + 2f, labelY + 2f, bw, 20f * scale),
                  collected + " / " + total + "  🪙", countStyle);
        // Ana
        countStyle.normal.textColor = textColor;
        GUI.Label(new Rect(afterIcon, labelY, bw, 20f * scale),
                  collected + " / " + total + "  🪙", countStyle);
    }

    // ── Yardimci cizimleri ─────────────────────────────────────

    private void DrawCoinIcon(float x, float y, float size)
    {
        // Altin daire
        float t      = Mathf.Abs(Mathf.Sin(Time.time * 2.2f));
        Color inner  = Color.Lerp(barFillColor, barShimmer, t * 0.6f);
        GUI.color    = new Color(inner.r, inner.g, inner.b, 1f);
        if (texWhite != null) GUI.DrawTexture(new Rect(x, y, size, size), texWhite);

        // Ic parlak halka
        float rim = size * 0.14f;
        GUI.color = new Color(1f, 1f, 0.8f, 0.45f);
        if (texWhite != null) GUI.DrawTexture(new Rect(x + rim, y + rim, size - rim*2f, size - rim*2f), texWhite);

        GUI.color = Color.white;
    }

    private void DrawRoundedBox(float x, float y, float w, float h, Color col)
    {
        GUI.color = col;
        if (texWhite != null) GUI.DrawTexture(new Rect(x, y, w, h), texWhite);
        GUI.color = Color.white;
    }

    private void DrawBarGradient(float x, float y, float w, float h, float progress)
    {
        // Koyu soldan parlak saga gecis simule et
        int steps = 8;
        for (int i = 0; i < steps; i++)
        {
            float t0    = (float)i / steps;
            float t1    = (float)(i + 1) / steps;
            float segX  = x + w * t0;
            float segW  = w * (t1 - t0) + 1f;

            float shimA = Mathf.Abs(Mathf.Sin(Time.time * 2.5f + t0 * 3.14f)) * 0.35f;
            Color left  = Color.Lerp(new Color(0.7f, 0.5f, 0.05f), barFillColor, t0);
            Color right = Color.Lerp(barFillColor, barShimmer, Mathf.Clamp01(t1 + shimA));
            Color seg   = Color.Lerp(left, right, (t0 + t1) * 0.5f);

            GUI.color = seg;
            if (texWhite != null) GUI.DrawTexture(new Rect(segX, y, segW, h), texWhite);
        }
        GUI.color = Color.white;
    }

    // ── Coroutines ─────────────────────────────────────────────

    private IEnumerator TriggerShimmer()
    {
        isShimmering = true;
        shimmerTime  = 0f;
        float t = 0f;
        while (t < 0.5f) { t += Time.deltaTime; shimmerAlpha = Mathf.Lerp(1f, 0f, t / 0.5f); yield return null; }
        shimmerAlpha = 0f;
        isShimmering = false;
    }

    private IEnumerator PulseIcon()
    {
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            pulseScale = Mathf.Lerp(1.4f, 1f, t / 0.3f);
            yield return null;
        }
        pulseScale = 1f;
    }

    // ── Texture helpers ────────────────────────────────────────

    private Texture2D MakeTex(int w, int h, Color c)
    {
        var t = new Texture2D(w, h);
        for (int i = 0; i < w; i++) for (int j = 0; j < h; j++) t.SetPixel(i, j, c);
        t.Apply();
        return t;
    }

    private Texture2D MakeRoundedTex(int w, int h, float r)
    {
        var t = new Texture2D(w, h);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float cx = x - w * 0.5f; float cy = y - h * 0.5f;
            float d  = Mathf.Sqrt(cx*cx + cy*cy);
            float a  = Mathf.Clamp01(1f - (d - (w * 0.5f - r)));
            t.SetPixel(x, y, new Color(1,1,1, a));
        }
        t.Apply();
        return t;
    }
}
