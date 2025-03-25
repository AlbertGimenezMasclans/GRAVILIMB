using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    [Tooltip("Prefab del proyectil que dispara el enemigo")]
    public GameObject projectilePrefab;
    [Tooltip("Punto desde donde se dispara el proyectil")]
    public Transform firePoint;
    [Tooltip("Velocidad del proyectil")]
    public float projectileSpeed = 8f;
    [Tooltip("Frecuencia de disparo (segundos entre disparos)")]
    public float fireRate = 2f;
    [Tooltip("Daño causado al jugador por cada disparo")]
    public float damage = 5f;

    [Header("Detection Settings")]
    [Tooltip("Distancia máxima a la que el enemigo detecta al jugador")]
    public float detectionRange = 10f;

    private Transform player; // Referencia al transform del jugador
    private float nextFireTime; // Control del tiempo para el próximo disparo
    private SpriteRenderer spriteRenderer; // Para voltear el sprite según la dirección

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
            Debug.LogWarning("No se encontró SpriteRenderer en el enemigo. El volteo del sprite no funcionará.");
        }

        nextFireTime = Time.time; // Inicializar el tiempo de disparo
    }

    void Update()
    {
        if (player == null) return;

        // Calcular la distancia al jugador
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Si el jugador está dentro del rango de detección
        if (distanceToPlayer <= detectionRange)
        {
            // Voltear el sprite según la posición del jugador
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = player.position.x < transform.position.x;
            }

            // Disparar si ha pasado suficiente tiempo
            if (Time.time >= nextFireTime)
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
        Vector2 direction = (player.position - firePoint.position).normalized;
        projectileRb.velocity = direction * projectileSpeed;

        // Pasar el daño al proyectil
        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            enemyProjectile.SetDamage(damage);
        }

        // Opcional: Rotar el proyectil para que apunte en la dirección de disparo
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // Visualizar el rango de detección en el Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}