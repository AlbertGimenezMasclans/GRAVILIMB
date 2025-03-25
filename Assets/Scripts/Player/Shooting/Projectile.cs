using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float lifetime = 2.5f; // Tiempo de vida en segundos
    private float timer = 0f;
    private Rigidbody2D rb; // Referencia al Rigidbody2D para obtener la velocidad
    private SpriteRenderer spriteRenderer; // Referencia al SpriteRenderer para voltear el sprite

    [Header("Damage Settings")]
    [Tooltip("Daño causado a los enemigos al impactar")]
    public float damageToEnemy = 5f; // Daño al enemigo, configurable en el Inspector

    void Awake()
    {
        // Obtener los componentes necesarios
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (rb == null)
        {
            Debug.LogError("Projectile necesita un Rigidbody2D para determinar la dirección.");
        }
        if (spriteRenderer == null)
        {
            Debug.LogError("Projectile necesita un SpriteRenderer para voltear el sprite.");
        }
    }

    void OnEnable()
    {
        timer = lifetime;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ReturnToPool();
        }

        // Voltear el sprite según la dirección en el eje X
        if (rb != null && spriteRenderer != null)
        {
            float velocityX = rb.velocity.x;
            if (Mathf.Abs(velocityX) > 0.1f) // Solo voltear si hay movimiento significativo
            {
                spriteRenderer.flipX = velocityX > 0f; // Voltear si va a la izquierda
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Aplicar daño si colisiona con un enemigo
        if (collision.gameObject.CompareTag("Enemy")) // Asegúrate de que el enemigo tenga el tag "Enemy"
        {
            EnemyShooter enemy = collision.gameObject.GetComponent<EnemyShooter>();
            if (enemy != null)
            {
                // Aquí podrías llamar a un método TakeDamage en EnemyShooter, pero como movimos la lógica, el enemigo necesitará un método público
                collision.gameObject.SendMessage("TakeDamage", damageToEnemy, SendMessageOptions.DontRequireReceiver);
            }
        }

        // No devolver al pool si colisiona con "Crate"
        if (!collision.gameObject.CompareTag("Crate"))
        {
            ReturnToPool();
        }
    }

    public void ReturnToPool() // Cambiado a público para que Crate lo llame
    {
        ProjectilePool.Instance.ReturnProjectile(gameObject);
    }
}