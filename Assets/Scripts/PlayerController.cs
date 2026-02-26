using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 8f;   // Koşma hızı
    public float jumpForce = 15f;  // Zıplama gücü

    private Rigidbody2D rb;
    private float moveInput;

    void Start()
    {
        // Karakterin fizik motorunu (Rigidbody2D) kodumuza bağlıyoruz
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Klavyeden sağ-sol (A-D veya Yön Tuşları) girdisini alıyoruz (-1, 0 veya 1 döner)
        moveInput = Input.GetAxisRaw("Horizontal");

        // Boşluk (Space) tuşuna basıldığında zıplama komutu veriyoruz
        if (Input.GetButtonDown("Jump"))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        // Karakteri sağa sola fiziksel olarak itiyoruz
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }
}