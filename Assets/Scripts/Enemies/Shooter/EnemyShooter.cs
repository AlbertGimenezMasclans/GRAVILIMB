using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    [Tooltip("Prefab del proyectil que dispara el enemigo")]
    public GameObject projectilePrefab;
    [Tooltip("Punto desde donde se dispara el proyectil (debe ser hijo del enemigo)")]
    public Transform firePoint;
    [Tooltip("Velocidad del proyectil")]
    public float projectileSpeed = 8f;
    [Tooltip("Frecuencia de disparo (segundos entre disparos)")]
    public float fireRate = 2f;
    [Tooltip("Daño causado al jugador por cada disparo")]
    public float projectileDamage = 5f;

    [Header("Detection and Shooting Range")]
    [Tooltip("Distancia máxima a la que el enemigo detecta al jugador")]
    public float detectionRange = 10f;
    [Tooltip("Distancia máxima del rango de disparo triangular")]
    public float shootingRange = 5f;
    [Tooltip("Ángulo del cono de disparo (en grados, la mitad del cono total)")]
    public float shootingAngle = 30f;

    [Header("Contact Damage Settings")]
    [Tooltip("Daño causado al jugador al tocar al enemigo")]
    public float contactDamage = 4f;
    [Tooltip("Tiempo de enfriamiento después de hacer daño por contacto (segundos)")]
    public float contactCooldown = 1f;

    [Header("Health Settings")]
    [Tooltip("Vida máxima del enemigo")]
    public float maxHealth = 10f;

    [Header("Death Effect Settings")]
    [Tooltip("Prefab for the effect to play when the enemy dies")]
    public GameObject deathEffectPrefab; // Efecto al morir
    [Tooltip("Duration of the death effect")]
    public float deathEffectDuration = 2f; // Duración del efecto (como en KredToken)

    private Transform player; // Referencia al transform del jugador
    private SpriteRenderer spriteRenderer; // Para voltear el sprite horizontalmente
    private float nextFireTime; // Control del tiempo para el próximo disparo
    private float contactCooldownEnd; // Tiempo en que termina el enfriamiento por contacto
    private bool isOnCooldown; // Indica si el enemigo está en enfriamiento
    private float currentHealth; // Vida actual del enemigo
    private bool facingRight; // Dirección en la que mira el sprite
    private bool isDead; // Indica si el enemigo está muerto

    void Start()
    {
        // Buscar al jugador en la escena usando el tag "Player"
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("No se encontró al jugador con el tag 'Player'. Asegúrate de que el jugador tenga el tag asignado.");
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No se encontró SpriteRenderer en el enemigo. El sprite no podrá voltearse.");
        }

        if (firePoint == null)
        {
            Debug.LogError("FirePoint no está asignado en el Inspector.");
        }

        nextFireTime = Time.time; // Inicializar el tiempo de disparo
        contactCooldownEnd = Time.time; // Inicializar el enfriamiento
        currentHealth = maxHealth; // Inicializar la vida
        facingRight = !spriteRenderer.flipX; // Dirección inicial basada en el sprite
        isDead = false;
    }

    void Update()
    {
        if (player == null || isDead) return;

        // Calcular la distancia al jugador
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Si el jugador está dentro del rango de detección
        if (distanceToPlayer <= detectionRange)
        {
            // Voltear el sprite horizontalmente según la posición del jugador
            if (spriteRenderer != null)
            {
                facingRight = player.position.x > transform.position.x;
                spriteRenderer.flipX = !facingRight;
            }

            // Ajustar la posición del firePoint según la dirección del sprite (sin rotarlo)
            if (firePoint != null)
            {
                Vector3 localPos = firePoint.localPosition;
                localPos.x = Mathf.Abs(localPos.x) * (facingRight ? 1f : -1f); // Ajustar posición en X
                firePoint.localPosition = localPos;
            }

            // Verificar si el enfriamiento por contacto ha terminado
            if (Time.time >= contactCooldownEnd)
            {
                isOnCooldown = false;
            }

            // Verificar si el jugador está dentro del rango de disparo triangular
            if (IsPlayerInShootingCone(distanceToPlayer) && Time.time >= nextFireTime && !isOnCooldown)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("ProjectilePrefab o FirePoint no están asignados en el Inspector.");
            return;
        }

        // Crear el proyectil
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        if (projectileRb == null)
        {
            projectileRb = projectile.AddComponent<Rigidbody2D>();
        }

        // Configurar el proyectil
        projectileRb.gravityScale = 0f; // Sin gravedad para un disparo recto
        Vector2 direction = (player.position - firePoint.position).normalized; // Dirección directa al jugador
        projectileRb.velocity = direction * projectileSpeed;

        // Rotar el proyectil para que apunte al jugador
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Pasar el daño al proyectil
        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            enemyProjectile.SetDamage(projectileDamage);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Infligir daño por contacto al jugador
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(contactDamage);
            }

            // Activar el enfriamiento
            isOnCooldown = true;
            contactCooldownEnd = Time.time + contactCooldown;
        }
    }

    bool IsPlayerInShootingCone(float distance)
    {
        if (distance > shootingRange) return false;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        Vector2 forward = facingRight ? Vector2.right : Vector2.left; // Dirección basada en el sprite
        float angleToPlayer = Vector2.Angle(forward, directionToPlayer);

        return angleToPlayer <= shootingAngle;
    }

    // Visualizar los rangos de detección y disparo en el Editor
    void OnDrawGizmosSelected()
    {
        // Rango de detección (círculo rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Rango de disparo (cono amarillo), ajustado según la dirección del sprite
        Gizmos.color = Color.yellow;
        Vector3 forward = facingRight ? Vector3.right : Vector3.left;
        Vector3 leftEdge = Quaternion.Euler(0, 0, shootingAngle) * forward * shootingRange;
        Vector3 rightEdge = Quaternion.Euler(0, 0, -shootingAngle) * forward * shootingRange;

        Gizmos.DrawLine(transform.position, transform.position + leftEdge);
        Gizmos.DrawLine(transform.position, transform.position + rightEdge);
        Gizmos.DrawLine(transform.position + leftEdge, transform.position + rightEdge);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        // Desactivar el comportamiento del enemigo
        enabled = false; // Desactiva el Update y otras funciones del MonoBehaviour

        // Instanciar el efecto de muerte
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            SpriteRenderer effectRenderer = effect.GetComponent<SpriteRenderer>();
            if (effectRenderer != null)
            {
                effectRenderer.flipX = Random.value > 0.5f; // Voltear aleatoriamente como en KredToken
            }
            Destroy(effect, deathEffectDuration); // Destruir el efecto después de su duración
        }
        else
        {
            Debug.LogWarning("DeathEffectPrefab no está asignado en el Inspector. No se mostrará ningún efecto al morir.");
        }

        // Destruir el enemigo inmediatamente, como en KredToken
        Destroy(gameObject);
    }
}