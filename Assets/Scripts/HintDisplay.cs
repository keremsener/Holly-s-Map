using UnityEngine;
using System.Collections;

/// <summary>
/// Ekrana hint mesajı yazan singleton — OnGUI ile çalışır, Canvas gerektirmez.
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
    private Color  currentColor  = new Color(1f, 0.92f, 0.55f, 1f);
    private int    currentSize   = 52;
    private float  alpha         = 0f;
    private bool   isShowing     = false;
    private Coroutine runningCo;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ──── Public API ────
    public void Show(string l1, string l2, Color col, int size,
                     float duration, float fadeIn, float fadeOut)
    {
        if (runningCo != null) StopCoroutine(runningCo);
        runningCo = StartCoroutine(DoShow(l1, l2, col, size, duration, fadeIn, fadeOut));
    }

    private IEnumerator DoShow(string l1, string l2, Color col, int size,
                                float duration, float fadeIn, float fadeOut)
    {
        currentLine1 = l1;
        currentLine2 = l2;
        currentColor = col;
        currentSize  = size;
        isShowing    = true;
        alpha        = 0f;

        // Fade in
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            alpha = Mathf.SmoothStep(0f, 1f, t / fadeIn);
            yield return null;
        }
        alpha = 1f;

        // Bekle (realtime — hitstop'tan etkilenmez)
        yield return new WaitForSecondsRealtime(duration);

        // Fade out
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
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

        // Arka plan (koyu yarı saydam kutu)
        float boxW = sw * 0.6f;
        float boxH = currentSize * 2.8f;
        float boxX = (sw - boxW) / 2f;
        float boxY = sh * 0.62f;

        var oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha * 0.55f);
        GUI.DrawTexture(new Rect(boxX - 20, boxY - 10, boxW + 40, boxH + 20),
                        Texture2D.whiteTexture);

        // Stil
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle  = FontStyle.Bold,
            fontSize   = currentSize
        };

        // Gölge (okunabilirlik için)
        style.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.8f);
        GUI.Label(new Rect(boxX + 2, boxY + 2, boxW, currentSize * 1.3f), currentLine1, style);
        style.fontSize = Mathf.RoundToInt(currentSize * 0.78f);
        GUI.Label(new Rect(boxX + 2, boxY + currentSize * 1.4f + 2, boxW, currentSize), currentLine2, style);

        // Ana renk
        style.fontSize = currentSize;
        style.normal.textColor = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
        GUI.Label(new Rect(boxX, boxY, boxW, currentSize * 1.3f), currentLine1, style);
        style.fontSize = Mathf.RoundToInt(currentSize * 0.78f);
        style.normal.textColor = new Color(currentColor.r, currentColor.g, currentColor.b, alpha * 0.9f);
        GUI.Label(new Rect(boxX, boxY + currentSize * 1.4f, boxW, currentSize), currentLine2, style);

        GUI.color = oldColor;
    }
}
