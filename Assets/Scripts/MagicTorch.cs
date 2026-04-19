using UnityEngine;

/// <summary>
/// Büyü mermisi ile ateşlenebilir meşale.
/// Ateşlenince bir objeyi aktif eder (kapı, asansör, spawner vb.)
/// </summary>
public class MagicTorch : MonoBehaviour
{
    [Header("State")]
    public bool isLit = false;

    [Header("On Light")]
    public GameObject[] objectsToActivate;
    public ParticleSystem flameParticles;
    public SpriteRenderer torchRenderer;
    public Color litColor = new Color(1f, 0.6f, 0.1f);
    public AudioClip igniteSound;

    [Header("Hint")]
    public string hintLine1 = "Meşale tutuştu!";
    public string hintLine2 = "";

    private void Start()
    {
        if (flameParticles != null) flameParticles.Stop();
    }

    /// <summary>Büyü mermisi çarptığında çağrılır.</summary>
    public void Ignite()
    {
        if (isLit) return;
        isLit = true;

        if (torchRenderer != null) torchRenderer.color = litColor;
        if (flameParticles != null) flameParticles.Play();
        if (igniteSound != null) AudioSource.PlayClipAtPoint(igniteSound, transform.position);

        foreach (var obj in objectsToActivate)
            if (obj != null) obj.SetActive(true);

        if (!string.IsNullOrEmpty(hintLine1))
            HintDisplay.Instance?.Show(hintLine1, hintLine2, Color.white, 20, 3f, 0.5f, 1f);

        // Odayı aydınlat
        CaveManager.Instance?.ClearDarkness();
    }
}
