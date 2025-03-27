using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Tooltip("Tiempo de vida del proyectil antes de destruirse")]
    public float lifetime = 5f;

    private float damage; // Daño que inflige, establecido por el enemigo

    void Start()
    {
        // Destruir el proyectil después de su tiempo de vida
        Destroy(gameObject, lifetime);
    }

    public void SetDamage(float damageValue)
    {
        damage = damageValue;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Obtener el componente PlayerHealth del jugador
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject); // Destruir el proyectil al impactar al jugador
        }
    }
}