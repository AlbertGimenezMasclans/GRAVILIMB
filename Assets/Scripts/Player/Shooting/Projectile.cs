using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float lifetime = 2.5f; // Tiempo de vida en segundos
    private float timer = 0f;

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
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
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