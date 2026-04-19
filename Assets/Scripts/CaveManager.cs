using UnityEngine;
using System.Collections;

/// <summary>
/// Ekrana programatik bir vignette (köşe kararması) çizer ve mağara giriş/çıkışında fade in/out yapar.
/// </summary>
public class CaveManager : MonoBehaviour
{
    public static CaveManager Instance { get; private set; }
    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    public float fadeSpeed = 1.5f;
    
    private Texture2D vignetteTexture;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    
    private void Start()
    {
        // Programatik olarak yumuşak bir vignette texture'ı oluştur
        vignetteTexture = new Texture2D(256, 256);
        for(int y = 0; y < 256; y++)
        {
            for(int x = 0; x < 256; x++)
            {
                float nx = (x / 255f) * 2f - 1f; // -1 ile 1 arası
                float ny = (y / 255f) * 2f - 1f; // -1 ile 1 arası
                float d = Mathf.Sqrt(nx * nx + ny * ny); // Merkeze uzaklık
                
                // Merkez (0.4) şeffaf, kenarlar tam siyah (1.0) olacak şekilde SmoothStep uygula
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1.2f, d));
                
                // Çok hafif bir genel karanlık ekleyelim, çok siyah olmasın
                alpha = Mathf.Clamp01(alpha + 0.1f); 
                
                vignetteTexture.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }
        vignetteTexture.Apply();
    }
    
    private void Update()
    {
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetAlpha = 0.65f; // Çok daha yumuşak, hafif bir karanlık
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetAlpha = 0f; // Mağaradan çıkınca aydınlık
        }
    }

    public void ClearDarkness()
    {
        targetAlpha = 0f;
    }
    
    private void OnGUI()
    {
        if (currentAlpha > 0.01f && vignetteTexture != null)
        {
            GUI.color = new Color(1f, 1f, 1f, currentAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture);
        }
    }
}
