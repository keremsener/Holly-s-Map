using UnityEngine;
using System.Collections;

/// <summary>
/// Ekrana dünya parçasıymış gibi hint mesajı yazar.
/// Sağ tarafta, arka plan yok, aşağı ok ile.
/// OnGUI singleton — Canvas gerektirmez.
/// </summary>
public class HintDisplay : MonoBehaviour
{
    // ──── Singleton ────
    private static HintDisplay _instance;
    public static HintDisplay Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("HintDisplay_AUTO");
            _instance = go.AddComponent<HintDisplay>();
            DontDestroyOnLoad(go);
            return _instance;
        }
    }

    // ──── State ────
    private string currentLine1 = "";
    private string currentLine2 = "";
    private float  alpha        = 0f;
    private bool   isShowing    = false;
    private Coroutine runningCo;

    // Ok animasyonu
    private float arrowBob = 0f;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (isShowing)
            arrowBob = Mathf.Sin(Time.unscaledTime * 3.5f) * 5f; // yukarı-aşağı zıplama
    }

    // ──── Public API ────
    public void Show(string l1, string l2, Color col, int size,
                     float duration, float fadeIn, float fadeOut)
    {
        if (runningCo != null) StopCoroutine(runningCo);
        runningCo = StartCoroutine(DoShow(l1, l2, duration, fadeIn, fadeOut));
    }

    private IEnumerator DoShow(string l1, string l2,
                                float duration, float fadeIn, float fadeOut)
    {
        currentLine1 = l1;
        currentLine2 = l2;
        isShowing    = true;
        alpha        = 0f;

        // Fade in
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            alpha = Mathf.SmoothStep(0f, 1f, t / fadeIn);
            yield return null;
        }
        alpha = 1f;

        yield return new WaitForSecondsRealtime(duration);

        // Fade out
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            alpha = 1f - Mathf.SmoothStep(0f, 1f, t / fadeOut);
            yield return null;
        }

        alpha     = 0f;
        isShowing = false;
        runningCo = null;
    }

    // ──── Rendering ────
    private void OnGUI()
    {
        if (!isShowing || alpha <= 0.01f) return;

        int sw = Screen.width;
        int sh = Screen.height;

        // ── FONT boyutları — ekrana göre ölçekli ──
        int fontSize1 = Mathf.RoundToInt(sh * 0.045f);   // ~büyük satır
        int fontSize2 = Mathf.RoundToInt(sh * 0.034f);   // ~küçük satır
        int arrowSize = Mathf.RoundToInt(sh * 0.065f);   // ok

        // ── Konum: sağ taraf, dikey orta-alt ──
        float panelW = sw * 0.30f;                        // genişlik %30
        float panelX = sw - panelW - sw * 0.025f;        // sağdan %2.5 boşluk
        float panelY = sh * 0.38f;                        // dikey %38

        // ── Stil: drop-shadow ile arka plansız metin ──
        var style = new GUIStyle
        {
            fontStyle  = FontStyle.Bold,
            alignment  = TextAnchor.UpperLeft,
            wordWrap   = true,
        };

        // Sarı altın renk — mısır teması
        Color goldColor  = new Color(1f, 0.88f, 0.35f, alpha);
        Color shadowCol  = new Color(0.15f, 0.08f, 0f, alpha * 0.9f);

        // ── Satır 1 ──
        style.fontSize = fontSize1;
        float line1H   = fontSize1 * 1.35f;

        // Gölge (sağ-alt)
        style.normal.textColor = shadowCol;
        GUI.Label(new Rect(panelX + 2, panelY + 2, panelW, line1H), currentLine1, style);
        // Ana
        style.normal.textColor = goldColor;
        GUI.Label(new Rect(panelX, panelY, panelW, line1H), currentLine1, style);

        // ── Satır 2 ──
        float line2Y = panelY + line1H + 4f;
        style.fontSize = fontSize2;
        float line2H   = fontSize2 * 1.35f;

        Color gold2     = new Color(1f, 0.82f, 0.25f, alpha * 0.88f);
        Color shadow2   = new Color(0.15f, 0.08f, 0f, alpha * 0.75f);

        style.normal.textColor = shadow2;
        GUI.Label(new Rect(panelX + 2, line2Y + 2, panelW, line2H), currentLine2, style);
        style.normal.textColor = gold2;
        GUI.Label(new Rect(panelX, line2Y, panelW, line2H), currentLine2, style);

        // ── Aşağı Ok (↓) — zıplayan animasyon ──
        float arrowY = line2Y + line2H + 6f + arrowBob;
        style.fontSize = arrowSize;
        style.alignment = TextAnchor.UpperLeft;

        style.normal.textColor = new Color(0.15f, 0.08f, 0f, alpha * 0.85f);
        GUI.Label(new Rect(panelX + 2, arrowY + 2, panelW, arrowSize * 1.5f), "↓", style);
        style.normal.textColor = new Color(1f, 0.78f, 0.1f, alpha);
        GUI.Label(new Rect(panelX, arrowY, panelW, arrowSize * 1.5f), "↓", style);
    }
}
