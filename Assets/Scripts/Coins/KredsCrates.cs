using UnityEngine;

public class KredsCrates : MonoBehaviour
{
    [SerializeField] private GameObject kredTokenPrefab; // Prefab del KredToken
    [SerializeField] private float spawnForce = 2f;      // Fuerza inicial para dispersar los tokens
    [SerializeField] private float spawnRadius = 0.5f;   // Radio para dispersión inicial de posición

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            SpawnKredTokens();
            Projectile projectile = collision.gameObject.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.ReturnToPool();
            }
            else
            {
                Destroy(collision.gameObject);
            }
            Destroy(gameObject);
        }
    }

    private void SpawnKredTokens()
    {
        if (kredTokenPrefab == null)
        {
            Debug.LogWarning("KredTokenPrefab no está asignado en el Inspector de Crate.");
            return;
        }

        // Soltar SOLO KredTokens normales
        int tokenCount = GetWeightedRandomTokenCount();
        for (int i = 0; i < tokenCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
            GameObject token = Instantiate(kredTokenPrefab, spawnPosition, Quaternion.identity);
            Rigidbody2D rb = token.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                rb.velocity = randomDirection * spawnForce;
            }
        }
    }

    private int GetWeightedRandomTokenCount()
    {
        float randomValue = Random.value;
        if (randomValue < 0.4f) return 6;  // 40%
        if (randomValue < 0.7f) return 7;  // 30%
        if (randomValue < 0.85f) return 8; // 15%
        if (randomValue < 0.95f) return 9; // 10%
        return 10;                         // 5%
    }
}