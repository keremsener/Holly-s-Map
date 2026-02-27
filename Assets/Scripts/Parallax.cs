using UnityEngine;

public class Parallax : MonoBehaviour
{
    private float length, startpos;
    public GameObject cam;
    public float parallaxEffect; // Katmanın kayma hızı

    void Start()
    {
        startpos = transform.position.x;
        // Resmin genişliğini ölçüyoruz ki bittiğinde kendini başa sarsın
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        // Kameranın ne kadar ilerlediğini hesapla
        float temp = (cam.transform.position.x * (1 - parallaxEffect));
        float dist = (cam.transform.position.x * parallaxEffect);

        // Katmanı kameranın hızına göre hareket ettir
        transform.position = new Vector3(startpos + dist, transform.position.y, transform.position.z);

        // SONSUZ ÇÖL HİLESİ: Resim bittiği an, çaktırmadan kendini ileriye kopyalar
        if (temp > startpos + length) startpos += length;
        else if (temp < startpos - length) startpos -= length;
    }
}