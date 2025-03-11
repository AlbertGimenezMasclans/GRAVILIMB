using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float lifetime = 0.8f; // Tiempo de vida del proyectil (0.5 segundos)
    private float spawnTime;       // Tiempo en que fue creado

    void Start()
    {
        spawnTime = Time.time; // Registrar el momento de creación
    }

    void Update()
    {
        // Destruir el proyectil después de 0.5 segundos
        if (Time.time >= spawnTime + lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject); // Destruir al chocar con algo
    }
}