using UnityEngine;

public class SecretWall : MonoBehaviour
{
    public Transform teleportDestination;
    public GameObject impactVfx;
    public AudioClip shatterSound;
    
    public void OnHitByMagic()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && teleportDestination != null)
        {
            // Play effects if any
            if (impactVfx != null) Instantiate(impactVfx, transform.position, Quaternion.identity);
            if (shatterSound != null) AudioSource.PlayClipAtPoint(shatterSound, transform.position);
            
            // Işınlanma efekti için minik bir bekleme yapılabilir ama anında yapalım:
            player.transform.position = teleportDestination.position;
            
            // Hızını sıfırla ki havada uçmasın
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}
