using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 8f;
    public float jumpForce = 15f;

    [Header("Zemin Kontrolü")]
    public LayerMask whatIsGround; 
    [Range(0.01f, 0.5f)] public float groundCheckExtraHeight = 0.1f; // Algılama mesafesi

    private Rigidbody2D rb;
    private CapsuleCollider2D col; // Box yerine Capsule yapıldı!
    private float moveInput;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<CapsuleCollider2D>(); // CapsuleCollider'ı otomatik bulur
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Animasyon Hızı
        anim.SetFloat("Speed", Mathf.Abs(moveInput));

        // Yön Döndürme
        if (moveInput > 0f)
        {
            transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); 
        }
        else if (moveInput < 0f)
        {
            transform.localScale = new Vector3(-0.3f, 0.3f, 0.3f); 
        }

        // Zıplama (CapsuleCast ile otomatik kontrol)
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private bool IsGrounded()
    {
        // Karakterin altından aşağıya hayali bir kapsül fırlatıp zemini kontrol eder
        // Box yerine CapsuleCast kullanarak kapsülün kenar kavislerine uyum sağlar
        RaycastHit2D hit = Physics2D.CapsuleCast(col.bounds.center, col.bounds.size, col.direction, 0f, Vector2.down, groundCheckExtraHeight, whatIsGround);
        return hit.collider != null;
    }
}