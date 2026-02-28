using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce)
    {
        currentHealth -= damage;
        Debug.Log("Minotaur Hasar Yedi! Kalan Can: " + currentHealth);

        // --- GERİ SAVRULMA (KNOCKBACK) ---
        rb.linearVelocity = Vector2.zero; // Mevcut hızı sıfırla
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        // --- KIRMIZI PARLAMA EFEKTİ ---
        StartCoroutine(FlashRed());

        if (currentHealth <= 0) Die();
    }

    IEnumerator FlashRed()
    {
        sprite.color = Color.red; // Vurulunca kıpkırmızı olsun
        yield return new WaitForSeconds(0.1f);
        sprite.color = Color.white; // Hemen normale dönsün
    }

    void Die()
    {
        Debug.Log("Minotaur Öldü!");
        Destroy(gameObject); // Şimdilik yok olsun, sonra ölüm animasyonu ekleriz
    }
}