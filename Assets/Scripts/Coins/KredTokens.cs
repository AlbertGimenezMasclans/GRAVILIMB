using UnityEngine;

public class KredToken : MonoBehaviour
{
    private int tokenValue = 1000; // Valor de cada KredToken
    [SerializeField] private GameObject collectionEffectPrefab; // Efecto de recolección

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collectionEffectPrefab != null)
            {
                GameObject effect = Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
                SpriteRenderer effectRenderer = effect.GetComponent<SpriteRenderer>();
                if (effectRenderer != null)
                {
                    effectRenderer.flipX = Random.value > 0.5f;
                }
                Destroy(effect, 2f); // Destruir el efecto después de 2 segundos
            }
            KredsManager.Instance.AddTokens(tokenValue);
            Destroy(gameObject); // Destruir el KredToken inmediatamente
        }
    }
}