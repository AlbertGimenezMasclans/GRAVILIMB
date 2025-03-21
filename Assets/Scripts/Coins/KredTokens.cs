using UnityEngine;

public class KredToken : MonoBehaviour
{
    private int tokenValue = 1000; // Valor de cada KredToken
    [SerializeField] private GameObject collectionEffectPrefab; // Efecto de recolección

    void Start()
    {
        // Ignorar colisiones con otros objetos que tengan el tag "KredToken"
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider != null)
        {
            GameObject[] allTokens = GameObject.FindGameObjectsWithTag("KredToken");
            foreach (GameObject token in allTokens)
            {
                if (token != gameObject) // No ignorar colisión consigo mismo
                {
                    Collider2D otherCollider = token.GetComponent<Collider2D>();
                    if (otherCollider != null && token.CompareTag("KredToken"))
                    {
                        Physics2D.IgnoreCollision(myCollider, otherCollider);
                    }
                }
            }
        }
    }

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
            }
            KredsManager.Instance.AddTokens(tokenValue);
            Destroy(gameObject);
        }
    }
}