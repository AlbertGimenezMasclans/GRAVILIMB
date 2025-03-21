using UnityEngine;

public class KredToken : MonoBehaviour
{
    private int tokenValue = 1000; // Valor de cada Kred Token
    [SerializeField] private GameObject collectionEffectPrefab; // Prefab del efecto de recolección

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collectionEffectPrefab != null)
            {
                GameObject effect = Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
                // Activar Flip X aleatoriamente (50% de probabilidad)
                SpriteRenderer effectRenderer = effect.GetComponent<SpriteRenderer>();
                if (effectRenderer != null)
                {
                    effectRenderer.flipX = Random.value > 0.5f; // True o False aleatorio
                }
                else
                {
                    Debug.LogWarning("El prefab KredCollect no tiene SpriteRenderer.");
                }
            }
            else
            {
                Debug.LogWarning("CollectionEffectPrefab no está asignado en el Inspector del KredToken.");
            }

            KredsManager.Instance.AddTokens(tokenValue);
            Destroy(gameObject);
        }
    }
}