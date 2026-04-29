using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Bolum secme ekrani. OnGUI ile cizilir.
/// 4 buyuk kart gosterir — kilitli bolumlerde kilit simgesi ve karanlik overlay.
/// Bir bolum tamamlaninca GameProgress.CompleteLevel() cagrilir ve bu ekrana donulur.
/// Yeni acilan bolum icin parlama animasyonu oynar.
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    // ── Animasyon durumu ────────────────────────────────────────
    private float  fadeAlpha   = 1f;
    private bool   fadingIn    = true;
    private bool   fadingOut   = false;
    private string loadTarget  = "";

    // Yeni kilidi kalkan bolum (GameProgress'ten gelir)
    private int  newlyUnlocked = -1;
    private float unlockAnim   = 0f;   // 0..1  kilit acilma animasyonu
    private bool  playingUnlockAnim = false;

    // Hover durumu
    private int hoveredCard = -1;

    // Kart animasyonlari
    private float[] cardScale;
    private float   cardPulse = 0f;

    // Texture
    private Texture2D texBlack;
    private Texture2D texWhite;

    // ── Statik: hangi bolum yeni acildi (sahneler arasi) ────────
    public static int PendingUnlockIndex = -1;

    private void Awake()
    {
        texBlack = MakeTex(Color.black);
        texWhite = MakeTex(Color.white);
        cardScale = new float[GameProgress.TOTAL_LEVELS];
        for (int i = 0; i < cardScale.Length; i++) cardScale[i] = 1f;

        // Yeni acilan bolum var mi?
        if (PendingUnlockIndex >= 0)
        {
            newlyUnlocked    = PendingUnlockIndex;
            PendingUnlockIndex = -1;
            StartCoroutine(PlayUnlockAnim(newlyUnlocked));
        }
    }

    private void Update()
    {
        cardPulse = Mathf.Abs(Mathf.Sin(Time.time * 1.8f));

        // Fade in
        if (fadingIn)
        {
            fadeAlpha -= Time.deltaTime * 1.6f;
            if (fadeAlpha <= 0f) { fadeAlpha = 0f; fadingIn = false; }
        }

        // Fade out + sahne yukle
        if (fadingOut)
        {
            fadeAlpha += Time.deltaTime * 2f;
            if (fadeAlpha >= 1f)
            {
                fadeAlpha = 1f;
                fadingOut = false;
                SceneManager.LoadScene(loadTarget);
            }
        }

        // Kart scale lerp
        for (int i = 0; i < cardScale.Length; i++)
        {
            float target = (i == hoveredCard) ? 1.04f : 1f;
            cardScale[i] = Mathf.Lerp(cardScale[i], target, Time.deltaTime * 10f);
        }
    }

    private void OnGUI()
    {
        int sw = Screen.width;
        int sh = Screen.height;
        float sc = sh / 600f;

        // ── Arka plan ────────────────────────────────────────────
        GUI.color = new Color(0.04f, 0.03f, 0.01f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), texBlack);
        GUI.color = Color.white;

        // ── Baslik ───────────────────────────────────────────────
        float titleY = 28f * sc;
        DrawTextShadow("BOLUM SEC", new Rect(0, titleY, sw, 70f * sc),
            Mathf.RoundToInt(48f * sc), TextAnchor.MiddleCenter,
            new Color(1f, 0.82f, 0.1f));

        // Altin yatay cizgi
        GUI.color = new Color(1f, 0.75f, 0.1f, 0.5f);
        GUI.DrawTexture(new Rect(sw * 0.1f, titleY + 65f * sc, sw * 0.8f, 2f * sc), texWhite);
        GUI.color = Color.white;

        // ── Kartlar ─────────────────────────────────────────────
        int totalLevels = GameProgress.TOTAL_LEVELS;
        float cardW  = Mathf.Min(sw * 0.20f, 200f * sc);
        float cardH  = cardW * 1.45f;
        float gapX   = sw * 0.04f;
        float totalW = cardW * totalLevels + gapX * (totalLevels - 1);
        float startX = (sw - totalW) * 0.5f;
        float cardY  = sh * 0.30f;

        hoveredCard = -1;
        var mousePos = Event.current.mousePosition;

        for (int i = 0; i < totalLevels; i++)
        {
            bool unlocked = GameProgress.Instance != null && GameProgress.Instance.IsUnlocked(i);
            float cx = startX + i * (cardW + gapX);
            float cy = cardY;

            // Scale pivot (merkez)
            float s    = cardScale[i];
            float offX = (cardW - cardW * s) * 0.5f;
            float offY = (cardH - cardH * s) * 0.5f;
            var cardRect = new Rect(cx + offX, cy + offY, cardW * s, cardH * s);

            // Hover tespiti
            if (cardRect.Contains(mousePos) && unlocked)
                hoveredCard = i;

            DrawLevelCard(cardRect, i, unlocked, i == newlyUnlocked, mousePos);
        }

        // ── Geri butonu ──────────────────────────────────────────
        float backW = 140f * sc;
        float backH = 44f  * sc;
        float backX = 28f  * sc;
        float backY = sh - backH - 24f * sc;
        if (DrawSimpleButton(new Rect(backX, backY, backW, backH), "← ANA MENU",
            Mathf.RoundToInt(14f * sc), new Color(0.7f, 0.6f, 0.2f)))
        {
            if (!fadingOut) StartFade("MainMenu");
        }

        // ── Fade overlay ─────────────────────────────────────────
        if (fadeAlpha > 0.01f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), texBlack);
            GUI.color = Color.white;
        }
    }

    // ── Kart cizim ──────────────────────────────────────────────
    private void DrawLevelCard(Rect r, int index, bool unlocked, bool justUnlocked, Vector2 mouse)
    {
        Color themeCol = GameProgress.LevelColors[index];
        bool hovered   = r.Contains(mouse) && unlocked;

        // Kart golge
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(r.x + 5, r.y + 7, r.width, r.height), texBlack);

        // Kart arka plani
        Color bgCol = unlocked
            ? (hovered ? new Color(themeCol.r * 0.35f, themeCol.g * 0.35f, themeCol.b * 0.35f, 1f)
                       : new Color(0.10f, 0.08f, 0.04f, 1f))
            : new Color(0.06f, 0.05f, 0.05f, 1f);
        GUI.color = bgCol;
        GUI.DrawTexture(r, texBlack);

        // Cerceve
        float pulseMul = justUnlocked ? (0.6f + unlockAnim * 0.4f) : (hovered ? 1f : 0.45f);
        GUI.color = new Color(themeCol.r, themeCol.g, themeCol.b, pulseMul);
        DrawBorder(r, 3f);

        GUI.color = Color.white;

        float sc = r.height / 290f;

        if (unlocked)
        {
            // ── Bolum numarasi (buyuk) ──────────────────────────
            Color numCol = justUnlocked
                ? Color.Lerp(themeCol, Color.white, unlockAnim * cardPulse * 0.6f)
                : themeCol;
            DrawTextShadow((index + 1).ToString(),
                new Rect(r.x, r.y + 18f * sc, r.width, 80f * sc),
                Mathf.RoundToInt(56f * sc), TextAnchor.MiddleCenter, numCol);

            // ── Bolum adi ────────────────────────────────────────
            DrawTextShadow(GameProgress.LevelNames[index],
                new Rect(r.x, r.y + r.height * 0.43f, r.width, 36f * sc),
                Mathf.RoundToInt(18f * sc), TextAnchor.MiddleCenter,
                new Color(1f, 0.92f, 0.5f));

            // ── Alt baslik ───────────────────────────────────────
            var subStyle = new GUIStyle
            {
                fontStyle = FontStyle.Italic,
                fontSize  = Mathf.RoundToInt(10f * sc),
                alignment = TextAnchor.MiddleCenter,
                wordWrap  = true,
            };
            subStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(r.x + 6, r.y + r.height * 0.60f, r.width - 12, 40f * sc),
                GameProgress.SubTitles[index], subStyle);

            // ── OYNA butonu (alt) ─────────────────────────────────
            float btnW = r.width * 0.70f;
            float btnH = 30f * sc;
            float btnX = r.x + (r.width - btnW) * 0.5f;
            float btnY = r.y + r.height - btnH - 14f * sc;
            if (DrawSimpleButton(new Rect(btnX, btnY, btnW, btnH), "OYNA",
                Mathf.RoundToInt(13f * sc), themeCol))
            {
                if (!fadingOut) StartFade(GameProgress.SceneNames[index]);
            }

            // ── Yeni acilma animasyonu overlay ───────────────────
            if (justUnlocked && unlockAnim < 1f)
            {
                GUI.color = new Color(themeCol.r, themeCol.g, themeCol.b, (1f - unlockAnim) * 0.45f);
                GUI.DrawTexture(r, texWhite);
                GUI.color = Color.white;
            }
        }
        else
        {
            // ── Kilitli kart ─────────────────────────────────────
            // Kilitli overlay
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(r, texBlack);
            GUI.color = Color.white;

            // Kilit simgesi (metin)
            DrawTextShadow("🔒",
                new Rect(r.x, r.y + r.height * 0.25f, r.width, r.height * 0.4f),
                Mathf.RoundToInt(36f * sc), TextAnchor.MiddleCenter,
                new Color(0.6f, 0.6f, 0.6f, 0.8f));

            // Bolum adi soluk
            DrawTextShadow(GameProgress.LevelNames[index],
                new Rect(r.x, r.y + r.height * 0.62f, r.width, 28f * sc),
                Mathf.RoundToInt(14f * sc), TextAnchor.MiddleCenter,
                new Color(0.6f, 0.6f, 0.6f, 0.55f));

            DrawTextShadow("Kilitli",
                new Rect(r.x, r.y + r.height * 0.75f, r.width, 24f * sc),
                Mathf.RoundToInt(11f * sc), TextAnchor.MiddleCenter,
                new Color(0.5f, 0.4f, 0.3f, 0.7f));
        }
    }

    // ── Animasyon ───────────────────────────────────────────────
    private IEnumerator PlayUnlockAnim(int idx)
    {
        playingUnlockAnim = true;
        unlockAnim = 0f;

        yield return new WaitForSeconds(0.6f); // Sahne acilinca bekle

        float dur = 1.6f;
        float t   = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            unlockAnim = Mathf.SmoothStep(0f, 1f, t / dur);
            yield return null;
        }
        unlockAnim = 1f;
        playingUnlockAnim = false;
        newlyUnlocked     = -1;
    }

    // ── Yardimcilar ─────────────────────────────────────────────
    private void StartFade(string scene)
    {
        if (fadingOut) return;
        loadTarget = scene;
        fadingOut  = true;
    }

    private bool DrawSimpleButton(Rect r, string label, int fontSize, Color accentCol)
    {
        bool hov = r.Contains(Event.current.mousePosition);

        // Golge
        GUI.color = new Color(0f, 0f, 0f, 0.4f);
        GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, r.width, r.height), texBlack);

        // Cerceve
        GUI.color = hov ? accentCol : new Color(accentCol.r, accentCol.g, accentCol.b, 0.55f);
        DrawBorder(r, 2f);

        // Arkaplan
        GUI.color = hov ? new Color(accentCol.r * 0.28f, accentCol.g * 0.28f, accentCol.b * 0.1f, 1f)
                        : new Color(0.08f, 0.06f, 0.02f, 0.9f);
        GUI.DrawTexture(r, texBlack);
        GUI.color = Color.white;

        var style = new GUIStyle
        {
            fontStyle = FontStyle.Bold,
            fontSize  = fontSize,
            alignment = TextAnchor.MiddleCenter,
        };
        float p = hov ? Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 0.25f : 0f;
        style.normal.textColor = Color.Lerp(accentCol, Color.white, p);

        return GUI.Button(r, label, style);
    }

    private void DrawTextShadow(string text, Rect r, int fontSize, TextAnchor align, Color col)
    {
        var style = new GUIStyle { fontStyle = FontStyle.Bold, fontSize = fontSize, alignment = align, wordWrap = true };
        style.normal.textColor = new Color(0.1f, 0.06f, 0f, col.a * 0.85f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = col;
        GUI.Label(r, text, style);
    }

    private void DrawBorder(Rect r, float t)
    {
        GUI.DrawTexture(new Rect(r.x,           r.y,            r.width, t),       texWhite);
        GUI.DrawTexture(new Rect(r.x,           r.y + r.height - t, r.width, t),  texWhite);
        GUI.DrawTexture(new Rect(r.x,           r.y,            t, r.height),      texWhite);
        GUI.DrawTexture(new Rect(r.x + r.width - t, r.y,       t, r.height),      texWhite);
    }

    private Texture2D MakeTex(Color c)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, c);
        tex.Apply();
        return tex;
    }
}
