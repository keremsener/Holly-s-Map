using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Professional Light Flicker Effect with Perlin Noise for smooth, natural flickering
/// </summary>
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private float minIntensity = 1.5f;
    [SerializeField] private float maxIntensity = 2.5f;
    [SerializeField] private float flickerSpeed = 0.5f;
    [SerializeField] private bool usePerlinNoise = true;

    private Light2D lightSource;
    private float perlinOffset;

    private void Start()
    {
        lightSource = GetComponent<Light2D>();
        perlinOffset = Random.Range(0f, 100f);

        if (lightSource == null)
        {
            Debug.LogError($"❌ {gameObject.name}: Light2D component not found!");
        }
    }

    private void Update()
    {
        if (lightSource == null) return;

        if (usePerlinNoise)
        {
            // Smooth Perlin noise flickering
            float noiseValue = Mathf.PerlinNoise(Time.time * flickerSpeed, perlinOffset);
            lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, noiseValue);
        }
        else
        {
            // Random flickering (original method)
            lightSource.intensity = Mathf.Lerp(lightSource.intensity, Random.Range(minIntensity, maxIntensity), flickerSpeed);
        }
    }
}