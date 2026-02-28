using UnityEngine;

public class SlashEffect : MonoBehaviour
{
    void Start()
    {
        // Kılıç izi doğduktan sadece 0.15 saniye sonra yok olsun!
        Destroy(gameObject, 0.15f);
    }
}