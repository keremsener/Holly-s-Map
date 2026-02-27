using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float walkSpeed = 4f;    
    public float runSpeed = 8f;     
    public float jumpForce = 15f;

    [Header("Zemin Kontrolü")]
    public LayerMask whatIsGround; 
    [Range(0.01f, 0.5f)] public float groundCheckExtraHeight = 0.1f;

    private Rigidbody2D rb;
    private CapsuleCollider2D col; 
    private float moveInput;
    private float currentSpeed; 
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<CapsuleCollider2D>(); //
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // --- SHIFT KONTROLÜ VE ANİMASYON GEÇİŞİ ---
        if (Mathf.Abs(moveInput) > 0.1f) // Hareket ediyorsak
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed = runSpeed;
                anim.SetFloat("MoveType", 2.0f); // Koşma (1.5'ten büyük)
            }
            else
            {
                currentSpeed = walkSpeed;
                anim.SetFloat("MoveType", 1.0f); // Yürüme (0.1'den büyük, 1.5'ten küçük)
            }
        }
        else
        {
            anim.SetFloat("MoveType", 0.0f); // Durma (0.1'den küçük)
        }

        // --- SPEED SATIRI SİLİNDİ! ---

        // Yön Döndürme
        if (moveInput > 0f) transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); 
        else if (moveInput < 0f) transform.localScale = new Vector3(-0.3f, 0.3f, 0.3f); 

        // Zıplama
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);
    }

    private bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.CapsuleCast(col.bounds.center, col.bounds.size, col.direction, 0f, Vector2.down, groundCheckExtraHeight, whatIsGround);
        return hit.collider != null;
    }
}