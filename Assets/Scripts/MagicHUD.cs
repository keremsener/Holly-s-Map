using UnityEngine;

public class MagicHUD : MonoBehaviour
{
    [Header("Referans")]
    [SerializeField] private PlayerCombat playerCombat;

    [Header("Pozisyon")]
    [SerializeField] private float panelWidth  = 220f;
    [SerializeField] private float panelHeight = 110f;
    [SerializeField] private float marginRight = 18f;
    [SerializeField] private float marginTop   = 18f;

    [Header("Renkler")]
    [SerializeField] private Color colorCharged    = new Color(0.45f, 0.15f, 0.90f, 1f);
    [SerializeField] private Color colorEmpty      = new Color(0.18f, 0.06f, 0.35f, 1f);
    [SerializeField] private Color colorRecharging = new Color(0.65f, 0.30f, 0.95f, 1f);
    [SerializeField] private Color colorBar        = new Color(0.55f, 0.20f, 0.95f, 1f);
    [SerializeField] private Color colorGold       = new Color(1.00f, 0.82f, 0.25f, 1f);
    [SerializeField] private Color colorBg         = new Color(0.05f, 0.02f, 0.12f, 0.88f);

    private Texture2D whiteTex;

    private void Awake()
    {
        whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();
    }

    private void Start()
    {
        if (playerCombat == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerCombat = player.GetComponent<PlayerCombat>();
        }
        if (playerCombat == null)
            Debug.LogWarning("[MagicHUD] PlayerCombat bulunamadi! Player tag'li objeye PlayerCombat eklenmis mi?");
    }

    private void OnGUI()
    {
        if (playerCombat == null) return;

        int   current = playerCombat.CurrentMagicCharges;
        int   max     = playerCombat.MaxMagicCharges;
        float totalCD = playerCombat.ManaRechargeTime;
        float readyAt = playerCombat.NextChargeReadyTime; // -1 = sarjda degil

        float px = Screen.width  - panelWidth  - marginRight;
        float py = marginTop;

        // --- Arkaplan + cerceve ---
        DrawBox(new Rect(px, py, panelWidth, panelHeight), colorBg);
        DrawOutline(new Rect(px, py, panelWidth, panelHeight), colorGold, 2f);

        // --- Baslik ---
        GUI.color = colorGold;
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize  = 12;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(px, py + 6f, panelWidth, 20f), "BUYU SARJI", titleStyle);
        GUI.color = Color.white;

        // --- Orb ikonlari ---
        float orbSize = 22f;
        float orbGap  = 8f;
        float totalW  = max * orbSize + (max - 1) * orbGap;
        float ox0     = px + (panelWidth - totalW) * 0.5f;
        float oy      = py + 32f;

        GUIStyle numStyle = new GUIStyle(GUI.skin.label);
        numStyle.fontSize  = 14;
        numStyle.fontStyle = FontStyle.Bold;
        numStyle.alignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < max; i++)
        {
            float ox      = ox0 + i * (orbSize + orbGap);
            Rect  orbRect = new Rect(ox, oy, orbSize, orbSize);

            if (i < current)
            {
                // Dolu
                DrawBox(orbRect, colorCharged);
                DrawOutline(orbRect, colorGold, 1.5f);
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                GUI.Label(orbRect, "F", numStyle);
                GUI.color = Color.white;
            }
            else if (i == current && readyAt > 0f)
            {
                // Dolmakta
                float elapsed  = Mathf.Max(0f, Time.time - (readyAt - totalCD));
                float progress = Mathf.Clamp01(elapsed / totalCD);
                DrawBox(orbRect, colorEmpty);
                float fillH = orbSize * progress;
                DrawBox(new Rect(ox, oy + orbSize - fillH, orbSize, fillH), colorRecharging);
                DrawOutline(orbRect, colorGold, 1.5f);
            }
            else
            {
                // Bos
                DrawBox(orbRect, colorEmpty);
                Color dimGold = new Color(colorGold.r, colorGold.g, colorGold.b, 0.3f);
                DrawOutline(orbRect, dimGold, 1.5f);
            }
        }

        // --- Sarj progress bar ---
        float bx = px + 10f;
        float by = oy + orbSize + 10f;
        float bw = panelWidth - 20f;
        float bh = 10f;

        DrawBox(new Rect(bx, by, bw, bh), new Color(0.1f, 0.04f, 0.2f, 1f));

        float ratio = 0f;
        if (current >= max)
        {
            ratio = 1f;
        }
        else if (readyAt > 0f)
        {
            float elapsed = Mathf.Max(0f, Time.time - (readyAt - totalCD));
            ratio = Mathf.Clamp01(elapsed / totalCD);
        }

        if (ratio > 0f)
            DrawBox(new Rect(bx, by, bw * ratio, bh), colorBar);

        DrawOutline(new Rect(bx, by, bw, bh), colorGold, 1f);

        // --- Alt yazi ---
        float textY = by + bh + 6f;
        GUIStyle infoStyle = new GUIStyle(GUI.skin.label);
        infoStyle.fontSize  = 11;
        infoStyle.alignment = TextAnchor.MiddleCenter;

        if (current >= max)
        {
            GUI.color = colorGold;
            GUI.Label(new Rect(px, textY, panelWidth, 18f), "TAM DOLU", infoStyle);
        }
        else if (readyAt > 0f)
        {
            float secsLeft = Mathf.Max(0f, readyAt - Time.time);
            GUI.color = colorRecharging;
            GUI.Label(new Rect(px, textY, panelWidth, 18f),
                current + "/" + max + " sarj  -  " + secsLeft.ToString("F1") + "s", infoStyle);
        }
        else
        {
            GUI.color = Color.gray;
            GUI.Label(new Rect(px, textY, panelWidth, 18f),
                current + "/" + max + " sarj", infoStyle);
        }

        GUI.color = Color.white;
    }

    // Dolu kutu
    private void DrawBox(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, whiteTex);
        GUI.color = Color.white;
    }

    // Cerceve
    private void DrawOutline(Rect r, Color c, float t)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(r.x,        r.y,        r.width, t),       whiteTex); // ust
        GUI.DrawTexture(new Rect(r.x,        r.yMax - t, r.width, t),       whiteTex); // alt
        GUI.DrawTexture(new Rect(r.x,        r.y,        t,       r.height), whiteTex); // sol
        GUI.DrawTexture(new Rect(r.xMax - t, r.y,        t,       r.height), whiteTex); // sag
        GUI.color = Color.white;
    }
}
