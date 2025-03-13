using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Variables públicas para ajustar en el Inspector
    public float moveSpeed = 5f;       // Velocidad de movimiento horizontal
    public float jumpForce = 5f;       // Fuerza del salto
    public LayerMask groundLayer;      // Capa del suelo para detectar si está en tierra
    public GameObject habSelector;     // Panel "Hab-Selector" en la jerarquía
    public GameObject projectilePrefab; // Prefab del proyectil blanco
    public float projectileSpeed = 10f; // Velocidad del proyectil
    public Transform firePoint;        // Punto de disparo (posición desde donde salen los proyectiles)

    // Variables privadas
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer; // Para cambiar el color del jugador
    private bool isGrounded;           // ¿Está el jugador en el suelo o techo?
    private float gravityScale;        // Escala de gravedad inicial
    private bool isGravityNormal = true; // Estado de la gravedad (normal o invertida)
    private float lastGravityChange;   // Tiempo del último cambio de gravedad
    private float gravityChangeDelay = 0.25f; // Cooldown de 0.25 segundos
    private bool hasTouchedGround = false; // ¿Ha tocado el suelo al menos una vez?
    private bool isRed = false;        // ¿Está el jugador en modo rojo (dispara)?
    private float facingDirection = 1f; // 1 = derecha, -1 = izquierda
    private bool isSelectingMode = false; // ¿Está en modo selección (E presionada)?

    void Start()
    {
        // Obtener componentes
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        gravityScale = rb.gravityScale; // Guardar la gravedad inicial
        lastGravityChange = -gravityChangeDelay; // Permitir el primer cambio inmediato

        // Asegurarse de que el panel esté oculto al inicio
        if (habSelector != null) habSelector.SetActive(false);

        // Establecer escala inicial explícitamente para evitar problemas de tamaño
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), 1f);

        // Tiempo normal al inicio
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Movimiento horizontal
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        // Girar al jugador según la dirección del movimiento
        if (moveInput != 0)
        {
            facingDirection = Mathf.Sign(moveInput); // 1 si derecha, -1 si izquierda
            transform.localScale = new Vector3(facingDirection * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);
        }

        // Detectar si está en el suelo o techo según la gravedad
        Vector2 rayDirection = isGravityNormal ? Vector2.down : Vector2.up;
        isGrounded = Physics2D.Raycast(transform.position, rayDirection, 1f, groundLayer);

        // Marcar que ha tocado el suelo al menos una vez
        if (isGrounded)
        {
            hasTouchedGround = true;
        }

        // Salto (tecla Espacio)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            float jumpDirection = isGravityNormal ? 1f : -1f;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce * jumpDirection);
        }

        // Cambiar gravedad y girar (tecla Q) - No permitido si es rojo, permitido en el aire tras tocar suelo
        if (Input.GetKeyDown(KeyCode.Q) && !isRed && hasTouchedGround && Time.time >= lastGravityChange + gravityChangeDelay)
        {
            rb.gravityScale = -rb.gravityScale;
            isGravityNormal = !isGravityNormal;
            // Rotar 180° verticalmente (Z) y 180° horizontalmente (Y)
            transform.Rotate(0f, 180f, 180f);
            lastGravityChange = Time.time; // Actualizar el tiempo del último cambio
        }

        // Mostrar panel "Hab-Selector" al mantener E y manejar estados
        if (Input.GetKey(KeyCode.E))
        {
            if (habSelector != null && !isSelectingMode) habSelector.SetActive(true);

            // Ralentizar el tiempo
            Time.timeScale = 0.3f;
            isSelectingMode = true;

            // Cambiar color y estado del jugador
            if (Input.GetKeyDown(KeyCode.Alpha1)) // Tecla "1" - Blanco, sin disparo
            {
                spriteRenderer.color = Color.white;
                isRed = false; // Desactiva el modo de disparo
                if (habSelector != null) habSelector.SetActive(false); // Ocultar "Hab-Selector"
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2)) // Tecla "2" - Rojo, con disparo
            {
                spriteRenderer.color = Color.red;
                isRed = true; // Activa el modo de disparo
                if (habSelector != null) habSelector.SetActive(false); // Ocultar "Hab-Selector"
            }
        }
        else
        {
            if (habSelector != null) habSelector.SetActive(false);
            Time.timeScale = 1f; // Restaurar tiempo normal al soltar E
            isSelectingMode = false;
        }

        // Disparar proyectiles solo si está en modo rojo
        if (isRed && Input.GetKeyDown(KeyCode.Q)) // ¡Corregido! Volvemos a usar "F" para disparar
        {
            FireProjectile();
        }
    }

    // Función para disparar proyectiles
    void FireProjectile()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            projectileRb.gravityScale = 0f; // Sin gravedad
            Vector2 shootDirection = transform.right * facingDirection; // Dirección ajustada
            projectileRb.velocity = shootDirection.normalized * projectileSpeed;
        }
    }

    // Restaurar Time.timeScale al desactivar el objeto (por si acaso)
    void OnDisable()
    {
        Time.timeScale = 1f;
    }

    // Dibujar el rayo de detección en el editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector2 rayDirection = isGravityNormal ? Vector2.down : Vector2.up;
        Gizmos.DrawRay(transform.position, rayDirection * 1f);
    }
}