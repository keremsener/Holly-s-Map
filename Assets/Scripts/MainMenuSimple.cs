using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Basit bir Ana Menu ekrani.
/// OnGUI ile cizilir — Canvas, Text Mesh Pro veya herhangi ek paket gerekmez.
/// </summary>
public class MainMenuSimple : MonoBehaviour
{
    [Header("Sahne Adi")]
    public string levelScene = "Level_01";

    // ── Animasyon durumu ──────────────────────────────────────────
    private float  fadeAlpha  = 1f;        // Baslangiçta siyah ekran
    private bool   fadingIn   = true;
    private bool   fadingOut  = false;
    private string loadTarget = "";

    // ── Renk paleti ──────────────────────────────────────────────
    private static readonly Color ColorBg      = new Color(0.05f, 0.04f, 0.02f);
    private static readonly Color ColorGold    = new Color(1f,   0.85f, 0.2f);
    private static readonly Color ColorGold2   = new Color(1f,   0.65f, 0.05f);
    private static readonly Color ColorBtn     = new Color(0.18f, 0.13f, 0.03f, 0.92f);
    private static readonly Color ColorBtnHov  = new Color(0.30f, 0.22f, 0.04f, 1f);
    private static readonly Color ColorWhite   = Color.white;

    private Texture2D texBlack;
    private Texture2D texBtn;
    private Texture2D texBtnHov;

    private float titleBob = 0f;
    private float starTimer = 0f;

    private void Awake()
    {
        texBlack  = MakeTex(1, 1, Color.black);
        texBtn    = MakeTex(1, 1, ColorBtn);
        texBtnHov = MakeTex(1, 1, ColorBtnHov);
    }

    private void Update()
    {
        titleBob  = Mathf.Sin(Time.time * 1.6f) * 8f;
        starTimer += Time.deltaTime;

        if (fadingIn)
        {
            fadeAlpha -= Time.deltaTime * 1.4f;
            if (fadeAlpha <= 0f) { fadeAlpha = 0f; fadingIn = false; }
        }

        if (fadingOut)
        {
            fadeAlpha += Time.deltaTime * 1.8f;
            if (fadeAlpha >= 1f)
            {
                fadeAlpha = 1f;
                fadingOut = false;
                SceneManager.LoadScene(loadTarget);
            }
        }
    }

    private void OnGUI()
    {
        int sw = Screen.width;
        int sh = Screen.height;
        float sc = sh / 600f; // Olcek carpani

        // ── Arka plan ─────────────────────────────────────────────
        GUI.color = ColorBg;
        GUI.DrawTexture(new Rect(0, 0, sw, sh), texBlack);
        GUI.color = Color.white;

        // ── Dekoratif altin yatay cizgi ───────────────────────────
        float lineH = 3f * sc;
        GUI.color   = ColorGold;
        GUI.DrawTexture(new Rect(0, sh * 0.22f, sw, lineH), texBlack);
        GUI.DrawTexture(new Rect(0, sh * 0.80f, sw, lineH), texBlack);
        GUI.color   = Color.white;

        // ── Baslik ────────────────────────────────────────────────
        var titleStyle = new GUIStyle
        {
            fontStyle = FontStyle.Bold,
            fontSize  = Mathf.RoundToInt(62f * sc),
            alignment = TextAnchor.MiddleCenter,
        };

        float titleY = sh * 0.10f + titleBob;

        // Golge
        titleStyle.normal.textColor = new Color(0.3f, 0.2f, 0f, 0.9f);
        GUI.Label(new Rect(4f, titleY + 4f, sw, 90f * sc), "HOLLY'S MAP", titleStyle);

        // Parlak altin
        titleStyle.normal.textColor = ColorGold;
        GUI.Label(new Rect(0, titleY, sw, 90f * sc), "HOLLY'S MAP", titleStyle);

        // Alt yazi
        var subStyle = new GUIStyle
        {
            fontStyle = FontStyle.Italic,
            fontSize  = Mathf.RoundToInt(18f * sc),
            alignment = TextAnchor.MiddleCenter,
        };
        subStyle.normal.textColor = new Color(1f, 0.75f, 0.2f, 0.75f);
        GUI.Label(new Rect(0, titleY + 75f * sc, sw, 30f * sc), "— Antik Misir'in Gizli Hazineleri —", subStyle);

        // ── Butonlar ─────────────────────────────────────────────
        float btnW  = 280f * sc;
        float btnH  = 58f  * sc;
        float btnX  = (sw - btnW) * 0.5f;
        float btn1Y = sh * 0.42f;
        float btn2Y = sh * 0.58f;
        float btn3Y = sh * 0.70f;

        if (DrawButton(btnX, btn1Y, btnW, btnH, "OYNA", Mathf.RoundToInt(26f * sc), sc))
        {
            if (!fadingOut) StartFade(levelScene);
        }

        if (DrawButton(btnX, btn2Y, btnW, btnH, "CIKIS", Mathf.RoundToInt(22f * sc), sc))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Coin ipucu ───────────────────────────────────────────
        var tipStyle = new GUIStyle
        {
            fontStyle = FontStyle.Bold,
            fontSize  = Mathf.RoundToInt(13f * sc),
            alignment = TextAnchor.MiddleCenter,
        };
        tipStyle.normal.textColor = new Color(1f, 0.88f, 0.4f, 0.65f);
        GUI.Label(new Rect(0, sh * 0.84f, sw, 30f * sc),
                  "Ipucu: Tum 20 coini topla — ancak o zaman bolumu tamamlayabilirsin!", tipStyle);

        // ── Versiyon ─────────────────────────────────────────────
        var verStyle = new GUIStyle { fontSize = Mathf.RoundToInt(10f * sc), alignment = TextAnchor.LowerRight };
        verStyle.normal.textColor = new Color(1f, 1f, 1f, 0.25f);
        GUI.Label(new Rect(0, sh - 22f * sc, sw - 10f, 20f * sc), "v1.0", verStyle);

        // ── Fade overlay ─────────────────────────────────────────
        if (fadeAlpha > 0.01f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), texBlack);
            GUI.color = Color.white;
        }
    }

    // ── Yardimci: Hover efektli buton ────────────────────────────
    private bool DrawButton(float x, float y, float w, float h, string label, int fontSize, float sc)
    {
        var rect    = new Rect(x, y, w, h);
        bool hover  = rect.Contains(Event.current.mousePosition);
        bool clicked = false;

        // Golge
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(x + 4f, y + 4f, w, h), texBlack);

        // Altin cerceve
        GUI.color = hover ? ColorGold : ColorGold2;
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, w + 4f, h + 4f), texBlack);

        // Arka plan
        GUI.color = hover ? ColorBtnHov : ColorBtn;
        GUI.DrawTexture(rect, texBtn);
        GUI.color = Color.white;

        // Yazi
        var s = new GUIStyle
        {
            fontStyle = FontStyle.Bold,
            fontSize  = fontSize,
            alignment = TextAnchor.MiddleCenter,
        };
        float pulse = hover ? Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.3f : 0f;
        s.normal.textColor = Color.Lerp(ColorGold, Color.white, pulse);

        if (GUI.Button(rect, label, s)) clicked = true;
        return clicked;
    }

    private void StartFade(string scene)
    {
        loadTarget = scene;
        fadingOut  = true;
    }

    private Texture2D MakeTex(int w, int h, Color c)
    {
        var t = new Texture2D(w, h);
        for (int i = 0; i < w; i++)
        for (int j = 0; j < h; j++) t.SetPixel(i, j, c);
        t.Apply();
        return t;
    }
}
