using UnityEngine;

public class Parallax : MonoBehaviour
{
    private float length, startpos;
    public GameObject cam; // Senin "Main Camera" objen buraya gelecek
    public float parallaxEffect; // 0 ile 1 arası değer (Örn: 0.9 yaparsan seninle gelir)

    void Start()
    {
        startpos = transform.position.x;
        // Resmin genişliğini ölçüyoruz ki bittiğinde çaktırmadan yerini değiştirsin
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate() // FixedUpdate kullanarak sarsıntıyı (jitter) engelliyoruz
    {
        // Ne kadar mesafe kat edildiğini hesapla
        float temp = (cam.transform.position.x * (1 - parallaxEffect));
        float dist = (cam.transform.position.x * parallaxEffect);

        // Arka planı kameranın hızına göre kaydırarak takip ettir
        transform.position = new Vector3(startpos + dist, transform.position.y, transform.position.z);

        // --- SONSUZ DÖNGÜ MANTIĞI ---
        // Eğer kamera resmin sonuna yaklaştıysa, resmi hemen ileriye taşı
        if (temp > startpos + length) startpos += length;
        else if (temp < startpos - length) startpos -= length;
    }
}