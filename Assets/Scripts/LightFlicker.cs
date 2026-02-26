using UnityEngine;
using UnityEngine.Rendering.Universal; // URP 2D ışıkları için şart

public class LightFlicker : MonoBehaviour
{
    private Light2D _light;
    public float minIntensity = 1.5f; // En az ne kadar parlasın
    public float maxIntensity = 2.5f; // En fazla ne kadar parlasın
    public float flickerSpeed = 0.1f; // Ne kadar hızlı titresin

    void Start()
    {
        _light = GetComponent<Light2D>();
    }

    void Update()
    {
        if (_light != null)
        {
            // Rasgele bir parlaklık değeri atayarak titreme efekti yapıyoruz
            _light.intensity = Mathf.Lerp(_light.intensity, Random.Range(minIntensity, maxIntensity), flickerSpeed);
        }
    }
}