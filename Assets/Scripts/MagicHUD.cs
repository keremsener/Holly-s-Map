using UnityEngine;

/// <summary>
/// Sağ üst köşede büyü şarj durumunu gösteren premium Mısır temalı HUD.
/// GameManager üzerinde durur, Player'ı otomatik bulur.
/// </summary>
public class MagicHUD : MonoBehaviour
{
    [Header("Referans")]
    [SerializeField] private PlayerCombat playerCombat;

    [Header("Pozisyon & Boyut")]
    [SerializeField] private float panelWidth  = 210f;
    [SerializeField] private float panelHeight = 108f;
    [SerializeField] private float marginRight = 16f;
    [SerializeField] private float marginTop   = 16f;

    [Header("Renkler")]
    [SerializeField] private Color colCharged    = new Color(0.55f, 0.10f, 1.00f, 1f);   // parlak mor
    [SerializeField] private Color colChargedGlow= new Color(0.80f, 0.50f, 1.00f, 0.35f); // orb aura
    [SerializeField] private Color colEmpty      = new Color(0.12f, 0.04f, 0.22f, 1f);   // koyu mor
    [SerializeField] private Color colRecharge   = new Color(0.70f, 0.35f, 1.00f, 1f);   // dolmakta
    [SerializeField] private Color colBar        = new Color(0.60f, 0.20f, 1.00f, 1f);   // progress bar
    [SerializeField] private Color colGold       = new Color(1.00f, 0.82f, 0.25f, 1f);   // altın
    [SerializeField] private Color colGoldDim    = new Color(1.00f, 0.82f, 0.25f, 0.40f);// soluk altın
    [SerializeField] private Color colBgTop      = new Color(0.10f, 0.02f, 0.20f, 0.94f);// üst - koyu mor
    [SerializeField] private Color colBgBot      = new Color(0.20f, 0.05f, 0.35f, 0.94f);// alt - orta mor

    // Iç
    private Texture2D _px;   // 1×1 beyaz
    private Texture2D _grad; // dikey gradient arka plan

    private void Awake()
    {
        _px = new Texture2D(1, 1);
        _px.SetPixel(0, 0, Color.white);
        _px.Apply();
    }

    private void Start()
    {
        if (playerCombat == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerCombat = p.GetComponent<PlayerCombat>();
        }
        if (playerCombat == null)
            Debug.LogWarning("[MagicHUD] PlayerCombat bulunamadi!");

        BuildGradient((int)panelWidth, (int)panelHeight);
    }

    private void BuildGradient(int w, int h)
    {
        _grad = new Texture2D(1, h);
        _grad.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            _grad.SetPixel(0, y, Color.Lerp(colBgTop, colBgBot, t));
        }
        _grad.Apply();
    }

    private void OnGUI()
    {
        if (playerCombat == null) return;

        int   cur     = playerCombat.CurrentMagicCharges;
        int   max     = playerCombat.MaxMagicCharges;
        float cd      = playerCombat.ManaRechargeTime;
        float readyAt = playerCombat.NextChargeReadyTime;

        float px = Screen.width  - panelWidth  - marginRight;
        float py = marginTop;
        Rect  panel = new Rect(px, py, panelWidth, panelHeight);

        // ── Gradient arka plan ───────────────────────────────────────
        GUI.color = Color.white;
        GUI.DrawTexture(panel, _grad, ScaleMode.StretchToFill);

        // ── Dış altın çerçeve (2px) ──────────────────────────────────
        Outline(panel, colGold, 2f);

        // ── İç ince çerçeve (hafif parlaklık) ───────────────────────
        Outline(new Rect(px + 3, py + 3, panelWidth - 6, panelHeight - 6), colGoldDim, 1f);

        // ── Köşe aksan noktaları ─────────────────────────────────────
        float cs = 5f;
        Box(new Rect(px,                    py,                     cs, cs), colGold);
        Box(new Rect(px + panelWidth - cs,  py,                     cs, cs), colGold);
        Box(new Rect(px,                    py + panelHeight - cs,  cs, cs), colGold);
        Box(new Rect(px + panelWidth - cs,  py + panelHeight - cs,  cs, cs), colGold);

        // ── Başlık ───────────────────────────────────────────────────
        GUI.color = colGold;
        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(px, py + 6f, panelWidth, 18f), "✦  BÜYÜ ŞARJI  ✦", titleStyle);

        // İnce ayraç çizgisi
        Box(new Rect(px + 10f, py + 26f, panelWidth - 20f, 1f), colGoldDim);

        // ── Orb ikonları ─────────────────────────────────────────────
        float orbS  = 24f;
        float orbG  = 10f;
        float totalW = max * orbS + (max - 1) * orbG;
        float ox0    = px + (panelWidth - totalW) * 0.5f;
        float oy     = py + 33f;

        float pulse = Mathf.Sin(Time.time * 4f) * 0.12f + 0.88f; // 0.76..1.0

        var starStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        for (int i = 0; i < max; i++)
        {
            float ox   = ox0 + i * (orbS + orbG);
            Rect  orb  = new Rect(ox, oy, orbS, orbS);

            if (i < cur)
            {
                // Dolu — aura + parlak orb
                float auraSize = orbS + 6f;
                Box(new Rect(ox - 3f, oy - 3f, auraSize, auraSize),
                    new Color(colChargedGlow.r, colChargedGlow.g, colChargedGlow.b, colChargedGlow.a * pulse));

                Box(orb, colCharged);
                Outline(orb, colGold, 1.5f);

                GUI.color = new Color(1f, 1f, 1f, 0.90f * pulse);
                GUI.Label(orb, "F", starStyle);
                GUI.color = Color.white;
            }
            else if (i == cur && readyAt > 0f)
            {
                // Dolmakta
                float elapsed  = Mathf.Max(0f, Time.time - (readyAt - cd));
                float progress = Mathf.Clamp01(elapsed / cd);

                Box(orb, colEmpty);
                float fillH = orbS * progress;
                Box(new Rect(ox, oy + orbS - fillH, orbS, fillH),
                    new Color(colRecharge.r, colRecharge.g, colRecharge.b, 0.75f + 0.25f * pulse));
                Outline(orb, colGold, 1.5f);
            }
            else
            {
                // Boş
                Box(orb, colEmpty);
                Outline(orb, colGoldDim, 1.5f);
            }
        }

        // ── Progress bar ─────────────────────────────────────────────
        float bx = px + 12f;
        float by = oy + orbS + 10f;
        float bw = panelWidth - 24f;
        float bh = 8f;

        // Bar arka planı
        Box(new Rect(bx, by, bw, bh), new Color(0.06f, 0.02f, 0.12f, 1f));

        float ratio = 0f;
        if (cur >= max)
            ratio = 1f;
        else if (readyAt > 0f)
        {
            float elapsed = Mathf.Max(0f, Time.time - (readyAt - cd));
            ratio = Mathf.Clamp01(elapsed / cd);
        }

        if (ratio > 0f)
        {
            // Bar dolgusu — renkli
            Box(new Rect(bx, by, bw * ratio, bh), colBar);
            // Parlak üst çizgi (highlight)
            Box(new Rect(bx, by, bw * ratio, 2f),
                new Color(1f, 1f, 1f, 0.25f));
        }

        Outline(new Rect(bx, by, bw, bh), colGoldDim, 1f);

        // ── Bilgi metni ───────────────────────────────────────────────
        float ty = by + bh + 5f;
        var infoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 10,
            alignment = TextAnchor.MiddleCenter
        };

        if (cur >= max)
        {
            GUI.color = colGold;
            GUI.Label(new Rect(px, ty, panelWidth, 16f), "● TAM DOLU ●", infoStyle);
        }
        else if (readyAt > 0f)
        {
            float left = Mathf.Max(0f, readyAt - Time.time);
            GUI.color = colRecharge;
            GUI.Label(new Rect(px, ty, panelWidth, 16f),
                cur + " / " + max + " şarj  —  " + left.ToString("F1") + "s", infoStyle);
        }
        else
        {
            GUI.color = new Color(0.6f, 0.5f, 0.7f, 1f);
            GUI.Label(new Rect(px, ty, panelWidth, 16f),
                cur + " / " + max + " şarj", infoStyle);
        }

        GUI.color = Color.white;
    }

    private void Box(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, _px);
        GUI.color = Color.white;
    }

    private void Outline(Rect r, Color c, float t)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(r.x,        r.y,        r.width, t),        _px);
        GUI.DrawTexture(new Rect(r.x,        r.yMax - t, r.width, t),        _px);
        GUI.DrawTexture(new Rect(r.x,        r.y,        t,       r.height), _px);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y,        t,       r.height), _px);
        GUI.color = Color.white;
    }
}
