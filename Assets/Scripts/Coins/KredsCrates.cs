using UnityEngine;

public class KredsCrates : MonoBehaviour
{
    [SerializeField] private GameObject kredTokenPrefab; // Prefab del KredToken
    [SerializeField] private float spawnForce = 2f;      // Fuerza inicial para dispersar los tokens

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

        int tokenCount = GetWeightedRandomTokenCount();
        GameObject[] spawnedTokens = new GameObject[tokenCount]; // Almacenar los tokens generados

        // Instanciar todos los tokens
        for (int i = 0; i < tokenCount; i++)
        {
            GameObject token = Instantiate(kredTokenPrefab, transform.position, Quaternion.identity);
            spawnedTokens[i] = token;

            Rigidbody2D rb = token.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                rb.velocity = randomDirection * spawnForce;
            }
            else
            {
                Debug.LogWarning("El KredToken instanciado no tiene Rigidbody2D.");
            }
        }

        // Ignorar colisiones entre los tokens generados
        for (int i = 0; i < spawnedTokens.Length; i++)
        {
            Collider2D colliderA = spawnedTokens[i].GetComponent<Collider2D>();
            if (colliderA != null)
            {
                for (int j = i + 1; j < spawnedTokens.Length; j++)
                {
                    Collider2D colliderB = spawnedTokens[j].GetComponent<Collider2D>();
                    if (colliderB != null)
                    {
                        Physics2D.IgnoreCollision(colliderA, colliderB);
                    }
                }
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