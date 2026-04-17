using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Professional Impact Camera Effects System
/// Kenarlardan vignette renk efekti + shake = gerçekçi vuruş hissi
/// </summary>
public class CameraEffects : MonoBehaviour
{
    [Header("═══ SHAKE SETTINGS ═══")]
    [SerializeField] private float shakeIntensity = 0.4f;
    [SerializeField] private float shakeDuration = 0.25f;

    [Header("═══ VIGNETTE FLASH SETTINGS ═══")]
    [SerializeField] private float vignetteIntensity = 0.8f;
    [SerializeField] private float vignetteDuration = 0.35f;
    [SerializeField] private Image vignettePanel;
    [SerializeField] private bool verboseLogs = false;

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private bool isShaking = false;

    // Singleton
    public static CameraEffects Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        mainCamera = GetComponent<Camera>();

        if (mainCamera == null)
        {
            Debug.LogError("❌ Camera component not found on CameraEffects!");
        }

        // Vignette panel oluştur (yoksa)
        if (vignettePanel == null)
        {
            CreateVignettePanel();
        }

        originalCameraPosition = transform.position;
    }

    /// <summary>
    /// Kılıç vuruşu — orta güçlü shake + kırmızı vignette.
    /// Her vuruşta yeniden başlar.
    /// </summary>
    public void MeleeHitEffect()
    {
        StopAllCoroutines();
        isShaking = false;
        StartCoroutine(CombinedShakeVignette(
            Color.red,
            vignetteDuration * 0.55f,  // 0.25 * 0.55 = 0.138s
            shakeIntensity   * 0.55f,  // 0.4 * 0.55  = 0.22
            shakeDuration    * 0.45f   // 0.25 * 0.45 = 0.112s
        ));
        if (verboseLogs) Debug.Log("⚔️ === MELEE HIT ===");
    }

    /// <summary>
    /// Büyü vuruşu - Mor vignette + orta shake
    /// </summary>
    public void MagicHitEffect()
    {
        Color magicPurple = new Color(0.8f, 0.2f, 1f);
        StartCoroutine(CombinedShakeVignette(magicPurple, vignetteDuration * 0.9f, shakeIntensity * 0.7f, shakeDuration * 0.8f));
        if (verboseLogs) Debug.Log("🔮 === MAGIC HIT ===");
    }

    /// <summary>
    /// Oyuncu düşmana vurdu — hafif ama tatmin edici shake.
    /// Tak-tak-tak seri vuruşta her seferinde tetiklenir.
    /// </summary>
    public void EnemyHitEffect()
    {
        // Her vuruşta shake'i yeniden başlat
        StopAllCoroutines();
        isShaking = false;

        // Hafif altın vignette + küçük shake
        Color hitGlow = new Color(1f, 0.78f, 0.1f);
        StartCoroutine(CombinedShakeVignette(
            hitGlow,
            vignetteDuration * 0.45f,  // kısa vignette
            shakeIntensity   * 0.20f,  // hafif shake (default 0.4 * 0.20 = 0.08)
            shakeDuration    * 0.30f   // çabuk biter (default 0.25 * 0.30 = 0.075s)
        ));
    }

    /// <summary>
    /// Heavy impact - Derin kırmızı vignette + güçlü shake
    /// </summary>
    public void HeavyImpactEffect()
    {
        Color deepRed = new Color(1f, 0.1f, 0f);
        StartCoroutine(CombinedShakeVignette(deepRed, vignetteDuration * 1.3f, shakeIntensity * 1.3f, shakeDuration * 1.2f));
    }

    /// <summary>
    /// Shake + Vignette combined efekt
    /// </summary>
    private IEnumerator CombinedShakeVignette(Color vignetteColor, float vignetteDur, float shakeIntens, float shakeDur)
    {
        // Her iki coroutine'i aynı anda başlat
        StartCoroutine(ShakeCamera(shakeDur, shakeIntens));
        yield return StartCoroutine(VignetteFlash(vignetteColor, vignetteDur));
    }

    private IEnumerator ShakeCamera(float duration, float intensity)
    {
        if (mainCamera == null) yield break;

        // isShaking guard kaldırıldı — her vuruşta yeniden başlar
        StopCoroutine("ShakeCamera"); // önceki shake'i iptal et
        isShaking = true;
        float elapsed = 0f;

        // Şu anki kamera pozisyonunu origin al (başka bir shake bitmemiş olabilir)
        float ox = Mathf.Round(mainCamera.transform.position.x * 1000f) / 1000f;
        float oy = Mathf.Round(mainCamera.transform.position.y * 1000f) / 1000f;
        float oz = mainCamera.transform.position.z;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // hitstop sırasında da çalışır
            float t    = elapsed / duration;
            float kick = intensity * (1f - t * t); // ease-out: başta güçlü, sonda sıfır

            // Perlin noise — random'dan çok daha smooth ve doğal
            float seed = Time.unscaledTime * 55f;
            float px   = (Mathf.PerlinNoise(seed,        0.3f) - 0.5f) * 2f;
            float py   = (Mathf.PerlinNoise(seed + 17f,  0.7f) - 0.5f) * 2f;

            mainCamera.transform.position = new Vector3(ox + px * kick, oy + py * kick, oz);
            yield return null;
        }

        // Orijinal pozisyona kesin dönüş
        var cur = mainCamera.transform.position;
        mainCamera.transform.position = new Vector3(ox, oy, oz);
        isShaking = false;
    }

    private IEnumerator VignetteFlash(Color flashColor, float duration)
    {
        if (vignettePanel == null) yield break;

        float elapsed = 0f;
        vignettePanel.gameObject.SetActive(true);

        // Başlangıç: çok yoğun
        float peakAlpha = vignetteIntensity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Hızlı başla, yavaş bitir
            float eased = Mathf.Lerp(1f, 0f, progress);
            float currentAlpha = peakAlpha * eased;

            Color vignetteColor = flashColor;
            vignetteColor.a = currentAlpha;
            vignettePanel.color = vignetteColor;

            yield return null;
        }

        vignettePanel.gameObject.SetActive(false);
    }

    private void CreateVignettePanel()
    {
        // Canvas oluştur
        GameObject canvasGO = new GameObject("VignetteCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // Vignette Image
        GameObject panelGO = new GameObject("VignettePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rectTransform = panelGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = panelGO.AddComponent<Image>();
        
        // Vignette texture (radial gradient beyazdan koyu siyaha)
        Texture2D vignetteTexture = CreateVignetteTexture();
        Sprite vignetteSprite = Sprite.Create(vignetteTexture, new Rect(0, 0, vignetteTexture.width, vignetteTexture.height), Vector2.one * 0.5f);
        
        image.sprite = vignetteSprite;
        image.type = Image.Type.Simple;
        image.color = new Color(1, 1, 1, 0);

        vignettePanel = image;
        vignettePanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Radial vignette texture oluştur (kenarlar koyu)
    /// </summary>
    private Texture2D CreateVignetteTexture()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center = Vector2.one * (size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                float maxDistance = size / 2f;

                // Kenarlar karanlık, merkez aydınlık
                float brightness = Mathf.Clamp01(1f - (distance / maxDistance) * 1.5f);
                
                pixels[y * size + x] = new Color(brightness, brightness, brightness, brightness);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}

