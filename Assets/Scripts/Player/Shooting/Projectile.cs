using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float lifetime = 2.5f; // Tiempo de vida en segundos
    private float timer = 0f;

    void OnEnable()
    {
        // Reiniciar el temporizador cuando el proyectil se activa
        timer = lifetime;
    }

    void Update()
    {
        // Contar el tiempo hacia abajo
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ReturnToPool(); // Devolver al pool
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Devolver al pool al colisionar
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        ProjectilePool.Instance.ReturnProjectile(gameObject);
    }
}