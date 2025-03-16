using UnityEngine;

public class Dismember : MonoBehaviour
{
    public float hopForceHorizontal = 3f;    // Fuerza horizontal del salto (ajustable)
    public float hopForceVertical = 4f;      // Fuerza vertical del salto (ajustable)
    public float hopInterval = 0.4f;         // Intervalo entre saltos (ajustable, como la rana)
    public float jumpForce = 5f;             // Fuerza del salto vertical independiente
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private float facingDirection = 1f;
    private bool isGravityNormal = true;
    private float lastGravityChange;
    private float gravityChangeDelay = 1f;
    private float lastHopTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        lastGravityChange = -gravityChangeDelay;
        lastHopTime = -hopInterval; // Permitir el primer salto inmediatamente
    }

    void Update()
    {
        // Movimiento a saltos como la rana
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.RightArrow))
        {
            moveInput = 1f;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveInput = -1f;
        }

        if (moveInput != 0 && Time.time >= lastHopTime + hopInterval && isGrounded)
        {
            facingDirection = Mathf.Sign(moveInput);
            transform.localScale = new Vector3(facingDirection * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);
            
            // Aplicar fuerza de salto con componente vertical y horizontal
            float verticalDirection = isGravityNormal ? 1f : -1f;
            rb.velocity = new Vector2(facingDirection * hopForceHorizontal, verticalDirection * hopForceVertical);
            lastHopTime = Time.time;
        }

        // Salto vertical independiente
        if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded)
        {
            float jumpDirection = isGravityNormal ? 1f : -1f;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce * jumpDirection);
        }

        // Cambio de gravedad con Z
        if (Input.GetKeyDown(KeyCode.Z) && Time.time >= lastGravityChange + gravityChangeDelay)
        {
            ChangeGravity();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            Vector2 normal = collision.contacts[0].normal;
            float dot = Vector2.Dot(normal, isGravityNormal ? Vector2.up : Vector2.down);
            if (dot > 0.5f)
            {
                isGrounded = true;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = false;
        }
    }

    private void ChangeGravity()
    {
        rb.gravityScale = -rb.gravityScale;
        isGravityNormal = !isGravityNormal;

        Vector3 center = transform.position;
        transform.RotateAround(center, Vector3.forward, 180f);

        lastGravityChange = Time.time;
        rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    public void SetGravityState(bool gravityNormal)
    {
        isGravityNormal = gravityNormal;
        rb.gravityScale = gravityNormal ? Mathf.Abs(rb.gravityScale) : -Mathf.Abs(rb.gravityScale);

        float horizontalScale = transform.localScale.x;
        transform.rotation = Quaternion.identity;
        if (!isGravityNormal)
        {
            transform.Rotate(0f, 0f, 180f);
        }
        transform.localScale = new Vector3(horizontalScale, Mathf.Abs(transform.localScale.y), transform.localScale.z);
    }
}