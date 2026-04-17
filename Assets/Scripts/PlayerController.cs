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

    [Header("Efekt Ayarları")]
    public ParticleSystem dustParticles; // Müfettişten (Inspector) sürükleyip bırak
    public float walkEmission = 10f;     // Yürürken çıkan toz miktarı
    public float runEmission = 30f;      // Koşarken çıkan toz miktarı

    private Rigidbody2D rb;
    private CapsuleCollider2D col;
    private float moveInput;
    private float currentSpeed; 
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<CapsuleCollider2D>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // --- HAREKET VE ANİMASYON MANTIĞI ---
        if (Mathf.Abs(moveInput) > 0.1f) 
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed = runSpeed;
                anim.SetFloat("MoveType", 2.0f);
            }
            else
            {
                currentSpeed = walkSpeed;
                anim.SetFloat("MoveType", 1.0f);
            }
        }
        else
        {
            currentSpeed = 0; // Durduğumuzda hız sıfırlansın
            anim.SetFloat("MoveType", 0.0f);
        }

        // Yön Döndürme
        if (moveInput > 0f) transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); 
        else if (moveInput < 0f) transform.localScale = new Vector3(-0.3f, 0.3f, 0.3f); 

        // Zıplama
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // --- TOZ BULUTU KONTROLÜ ---
        HandleParticles();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);
    }

    private void HandleParticles()
    {
        if (dustParticles == null) return;

        var emission = dustParticles.emission; // Parçacık sisteminin yayılma ayarını al

        // Karakter yerde ve hareket halindeyse toz çıkart
        if (Mathf.Abs(moveInput) > 0.1f && IsGrounded())
        {
            // Koşarken yoğun, yürürken hafif toz
            emission.rateOverTime = Input.GetKey(KeyCode.LeftShift) ? runEmission : walkEmission;
            
            if (!dustParticles.isPlaying) dustParticles.Play();
        }
        else
        {
            // Durduğunda veya havadayken toz çıkışını kes
            emission.rateOverTime = 0;
        }
    }

    private bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.CapsuleCast(col.bounds.center, col.bounds.size, col.direction, 0f, Vector2.down, groundCheckExtraHeight, whatIsGround);
        return hit.collider != null;
    }
}