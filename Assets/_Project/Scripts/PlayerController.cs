using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 14f;

    [Header("Game Feel - Curva de salto")]
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [SerializeField] private float lowJumpGravityMultiplier = 2f;

    [Header("Detección de suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float horizontalInput;
    private bool isGrounded;
    private bool facingRight = true;
    private float baseGravityScale;

    // Expuesto para PlayerAnimator y otros sistemas (UI, cámara, etc.) sin acoplar lógica extra aquí.
    public bool IsGrounded => isGrounded;
    public float HorizontalInput => horizontalInput;
    public Rigidbody2D Rigidbody => rb;
    public bool FacingRight => facingRight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseGravityScale = rb.gravityScale;
    }

    private void Update()
    {
        // GetAxisRaw evita el suavizado interno de Unity: respuesta instantánea, sin inercia flotante.
        horizontalInput = Input.GetAxisRaw("Horizontal");

        isGrounded = groundCheck != null &&
                     Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            AudioManager.Instance?.PlaySfxJump();
        }

        ApplyVariableJumpGravity();
        UpdateFacing();
    }

    // Cambio de orientación del personaje: voltea el sprite (no el collider) según hacia dónde se mueve.
    private void UpdateFacing()
    {
        if (horizontalInput > 0f && !facingRight) Flip();
        else if (horizontalInput < 0f && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    // Salto "pesado y responsivo": cae más rápido de lo que sube, y si se suelta el
    // botón mientras sube, el salto se corta antes (salto de altura variable).
    private void ApplyVariableJumpGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.gravityScale = baseGravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0f && !Input.GetButton("Jump"))
        {
            rb.gravityScale = baseGravityScale * lowJumpGravityMultiplier;
        }
        else
        {
            rb.gravityScale = baseGravityScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
