using UnityEngine;

public class KredsCrates : MonoBehaviour
{
    [SerializeField] private GameObject kredTokenPrefab; // Prefab del KredToken (1000 puntos)
    [SerializeField] private GameObject kredBitPrefab;   // Prefab del KredBit (100 puntos)
    [SerializeField] private float spawnForce = 2f;      // Fuerza inicial para dispersar los objetos
    [SerializeField] private float spawnRadius = 0.5f;   // Radio para dispersión inicial de posición
    [SerializeField] [Range(0f, 1f)] private float tokenProbability = 0.25f; // Probabilidad de que sea un Token (25%)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            SpawnKredItems();
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

    private void SpawnKredItems()
    {
        if (kredTokenPrefab == null || kredBitPrefab == null)
        {
            Debug.LogWarning("Falta asignar KredTokenPrefab o KredBitPrefab en el Inspector de Crate.");
            return;
        }

        int itemCount = GetWeightedRandomItemCount();
        for (int i = 0; i < itemCount; i++)
        {
            // Decidir si es un Token (25%) o un Bit (75%)
            bool isToken = Random.value < tokenProbability;
            GameObject prefabToSpawn = isToken ? kredTokenPrefab : kredBitPrefab;

            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
            GameObject item = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            Rigidbody2D rb = item.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                rb.velocity = randomDirection * spawnForce;
            }
        }
    }

    private int GetWeightedRandomItemCount()
    {
        float randomValue = Random.value;
        if (randomValue < 0.4f) return 6;  // 40% - 6 items
        if (randomValue < 0.7f) return 7;  // 30% - 7 items
        if (randomValue < 0.85f) return 8; // 15% - 8 items
        if (randomValue < 0.95f) return 9; // 10% - 9 items
        return 10;                         // 5%  - 10 items
    }
}