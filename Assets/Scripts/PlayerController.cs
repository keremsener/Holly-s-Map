using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 8f;
    public float jumpForce = 15f;

    [Header("Zemin Kontrolü")]
    public LayerMask whatIsGround; // Zemin katmanını buradan seçeceğiz
    public Transform groundCheck;  // Karakterin altına koyacağın boş obje
    public float checkRadius = 0.2f; // Kontrol dairesinin büyüklüğü

    private Rigidbody2D rb;
    private float moveInput;
    private Animator anim;
    private bool isGrounded; // Yerde miyiz?

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // 1. Zemin Kontrolü: Ayakların altındaki daire zemine değiyor mu?
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        // 2. Animasyon Hızı
        anim.SetFloat("Speed", Mathf.Abs(moveInput));

        // 3. Yön Döndürme
        if (moveInput > 0f)
        {
            transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); 
        }
        else if (moveInput < 0f)
        {
            transform.localScale = new Vector3(-0.3f, 0.3f, 0.3f); 
        }

        // 4. Zıplama (Sadece yerdeyken!)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }
}