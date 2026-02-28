using UnityEngine;

public class MagicBullet : MonoBehaviour
{
    public float speed = 15f;
    private Rigidbody2D rb;

    void Start()
    {
        // Merminin fiziğini (Rigidbody) bul
        rb = GetComponent<Rigidbody2D>();
        
        // Doğar doğmaz karakterin baktığı yöne doğru (sağa) uçmaya başla
        rb.linearVelocity = transform.right * speed;
        
        // Mermi boşa giderse haritadan çıkıp oyunu kastırmasın diye 2 saniye sonra kendini sil
        Destroy(gameObject, 2f); 
    }

    // Mermi (Trigger açık olduğu için) bir şeyin içinden geçerken burası çalışır
    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Eğer çarptığı şeyin adı "Enemy_Minotaur" ise
        if(hitInfo.name == "Enemy_Minotaur")
        {
            Debug.Log("Mor büyü Minotaur'a çarptı!");
            Destroy(hitInfo.gameObject); // Canavarı yok et
            Destroy(gameObject); // Çarptıktan sonra merminin kendisi de patlayıp yok olsun
        }
    }
}