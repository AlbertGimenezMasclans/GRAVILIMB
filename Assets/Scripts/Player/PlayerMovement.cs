using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Variables públicas existentes
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public LayerMask groundLayer;
    public GameObject habSelector;
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public Transform firePoint;

    // Variables para el desmembramiento
    public GameObject headObject;    // Referencia al GameObject de la cabeza
    public GameObject bodyObject;    // Referencia al GameObject del cuerpo
    private bool isDismembered = false;

    // Variables privadas existentes
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Animator animator;
    private bool isGrounded;
    private float gravityScale;
    private bool isGravityNormal = true;
    private float lastGravityChange;
    private float gravityChangeDelay = 1f;
    private bool hasTouchedGround = false;
    private bool isShoot = false;
    private float facingDirection = 1f;
    private bool isSelectingMode = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        gravityScale = rb.gravityScale;
        lastGravityChange = -gravityChangeDelay;

        if (habSelector != null) habSelector.SetActive(false);
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), 1f);
        Time.timeScale = 1f;

        if (headObject != null) headObject.SetActive(false);
        if (bodyObject != null) bodyObject.SetActive(false);
    }

    void Update()
{
    if (!isDismembered)
    {
        // Movimiento horizontal (sin cambios)
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.RightArrow))
        {
            moveInput = 1f;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveInput = -1f;
        }
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        animator.SetFloat("Speed", Mathf.Abs(moveInput));

        if (moveInput != 0)
        {
            facingDirection = Mathf.Sign(moveInput);
            transform.localScale = new Vector3(facingDirection * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded)
        {
            float jumpDirection = isGravityNormal ? 1f : -1f;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce * jumpDirection);
        }

        if (Input.GetKeyDown(KeyCode.Z) && !isShoot && hasTouchedGround && Time.time >= lastGravityChange + gravityChangeDelay)
        {
            ChangeGravity();
        }

        // Modo de selección
        if (Input.GetKey(KeyCode.X))
        {
            if (habSelector != null && !isSelectingMode)
            {
                habSelector.SetActive(true);
                Debug.Log("Selector activado");
            }
            Time.timeScale = 0.3f;
            isSelectingMode = true;

            if (Input.GetKeyDown(KeyCode.A))
            {
                spriteRenderer.color = Color.white;
                isShoot = false;
                if (habSelector != null) habSelector.SetActive(false);
                Time.timeScale = 1f;
                isSelectingMode = false;
                Debug.Log("Estado cambiado a blanco");
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                spriteRenderer.color = Color.red;
                isShoot = true;
                if (habSelector != null) habSelector.SetActive(false);
                Time.timeScale = 1f;
                isSelectingMode = false;
                Debug.Log("Estado cambiado a rojo");
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("Intentando desmembrar");
                DismemberHead();
            }
        }
        else
        {
            if (habSelector != null) habSelector.SetActive(false);
            Time.timeScale = 1f;
            isSelectingMode = false;
        }

        if (isShoot && Input.GetKeyDown(KeyCode.Z))
        {
            FireProjectile();
        }
    }
    else
    {
        rb.velocity = Vector2.zero;

        // Volver al estado normal con Z
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ReturnToNormal();
        }
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
                hasTouchedGround = true;
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

        Vector3 center = boxCollider.bounds.center;
        transform.RotateAround(center, Vector3.forward, 180f);
        transform.RotateAround(center, Vector3.up, 180f);

        lastGravityChange = Time.time;
        rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    void FireProjectile()
{
    if (projectilePrefab != null && firePoint != null && ProjectilePool.Instance.CanShoot())
    {
        // Obtener un proyectil del pool
        GameObject projectile = ProjectilePool.Instance.GetProjectile();
        if (projectile != null) // Verificar que se obtuvo un proyectil
        {
            projectile.transform.position = firePoint.position; // Posicionar en el punto de disparo
            projectile.transform.rotation = Quaternion.identity; // Reiniciar rotación
            projectile.SetActive(true); // Activar el proyectil

            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            projectileRb.gravityScale = 0f;
            Vector2 shootDirection = transform.right * facingDirection;
            projectileRb.velocity = shootDirection.normalized * projectileSpeed;
        }
    }
}

    void OnDisable()
    {
        Time.timeScale = 1f;
    }

private void DismemberHead()
{
    if (headObject != null && bodyObject != null)
    {
        Debug.Log("Desmembramiento iniciado");
        isDismembered = true;

        // Hacer invisible al jugador y desactivar su colisión
        spriteRenderer.enabled = false;
        if (boxCollider != null) boxCollider.enabled = false;
        rb.velocity = Vector2.zero;

        if (habSelector != null) habSelector.SetActive(false);
        Time.timeScale = 1f;
        isSelectingMode = false;

        bodyObject.SetActive(true);
        headObject.SetActive(true);

        bodyObject.transform.position = transform.position;
        bodyObject.transform.rotation = transform.rotation;
        bodyObject.transform.localScale = transform.localScale;

        Vector3 headOffset = new Vector3(0f, boxCollider.size.y * 0.5f, 0f);
        headObject.transform.position = transform.position + (isGravityNormal ? headOffset : -headOffset);
        headObject.transform.localScale = transform.localScale;

        Rigidbody2D bodyRb = bodyObject.GetComponent<Rigidbody2D>();
        if (bodyRb != null)
        {
            bodyRb.bodyType = RigidbodyType2D.Static;
            bodyRb.velocity = Vector2.zero;
        }
    }
    else
    {
        Debug.LogError("headObject o bodyObject no están asignados en el Inspector");
    }
}

    private void ReturnToNormal()
    {
        if (headObject != null && bodyObject != null)
        {
            isDismembered = false;

            // Hacer visible al jugador y reactivar su colisión
            spriteRenderer.enabled = true;
            if (boxCollider != null) boxCollider.enabled = true;

            // Desactivar cabeza y cuerpo
            headObject.SetActive(false);
            bodyObject.SetActive(false);

            // Mover al jugador a la posición del cuerpo
            transform.position = bodyObject.transform.position;

            // Cambiar al estado de cambio de gravedad (blanco)
            isShoot = false;

            // Restaurar la escala y dirección
            transform.localScale = bodyObject.transform.localScale;
        }
    }
}