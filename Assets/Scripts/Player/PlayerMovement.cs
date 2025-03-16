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
    public bool isDismembered = false;

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
    private bool isRed = false;
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
            float moveInput = 0f;
            if (Input.GetKey(KeyCode.RightArrow)) moveInput = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow)) moveInput = -1f;
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

            if (Input.GetKeyDown(KeyCode.Z) && !isRed && hasTouchedGround && Time.time >= lastGravityChange + gravityChangeDelay)
            {
                ChangeGravity();
            }

            if (Input.GetKey(KeyCode.X))
            {
                if (habSelector != null && !isSelectingMode)
                {
                    habSelector.SetActive(true);
                }
                Time.timeScale = 0.3f;
                isSelectingMode = true;

                if (Input.GetKeyDown(KeyCode.A))
                {
                    spriteRenderer.color = Color.white;
                    isRed = false;
                    if (habSelector != null) habSelector.SetActive(false);
                    Time.timeScale = 1f;
                    isSelectingMode = false;
                }
                if (Input.GetKeyDown(KeyCode.D))
                {
                    spriteRenderer.color = Color.red;
                    isRed = true;
                    if (habSelector != null) habSelector.SetActive(false);
                    Time.timeScale = 1f;
                    isSelectingMode = false;
                }
                if (Input.GetKeyDown(KeyCode.S) && isGrounded)
                {
                    DismemberHead();
                }
            }
            else
            {
                if (habSelector != null) habSelector.SetActive(false);
                Time.timeScale = 1f;
                isSelectingMode = false;
            }

            if (isRed && Input.GetKeyDown(KeyCode.Z))
            {
                FireProjectile();
            }
        }
        else
        {
            rb.velocity = Vector2.zero;

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
            GameObject projectile = ProjectilePool.Instance.GetProjectile();
            if (projectile != null)
            {
                projectile.transform.position = firePoint.position;
                projectile.transform.rotation = Quaternion.identity;
                projectile.SetActive(true);

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
            isDismembered = true;

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

            Rigidbody2D headRb = headObject.GetComponent<Rigidbody2D>();
            if (headRb != null)
            {
                headRb.bodyType = RigidbodyType2D.Dynamic;
                headRb.gravityScale = 1f; // Siempre gravedad normal (hacia abajo)
                headRb.velocity = Vector2.zero;
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

            spriteRenderer.enabled = true;
            if (boxCollider != null) boxCollider.enabled = true;

            headObject.SetActive(false);
            bodyObject.SetActive(false);

            transform.position = bodyObject.transform.position;

            spriteRenderer.color = Color.white;
            isRed = false;

            transform.localScale = bodyObject.transform.localScale;

            Time.timeScale = 1f;
        }
    }

    // Método público para que "recompose" lo llame
    public void RecomposePlayer()
    {
        if (isDismembered)
        {
            ReturnToNormal();
        }
    }
}