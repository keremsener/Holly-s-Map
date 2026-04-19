using UnityEngine;

public class Bouncer : MonoBehaviour
{
    [Header("Bouncing Settings")]
    public float bounceForce = 20f;
    public AudioClip bounceSound;
    public GameObject bounceVFX;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Yukarıdan üstüne düşen objeleri zıplat (y hızı negatif = aşağı gidiyor)
        if (collision.relativeVelocity.y <= 0.5f)
        {
            var rb = collision.collider.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                if (bounceSound != null) AudioSource.PlayClipAtPoint(bounceSound, transform.position);
                if (bounceVFX != null) Instantiate(bounceVFX, transform.position, Quaternion.identity);
            }
        }
    }

    // Trigger olarak kurulduysa bunu da kullan
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null && collision.CompareTag("Player"))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

            if (bounceSound != null) AudioSource.PlayClipAtPoint(bounceSound, transform.position);
            if (bounceVFX != null) Instantiate(bounceVFX, transform.position, Quaternion.identity);
        }
    }
}
