using UnityEngine;

public class KredToken : MonoBehaviour
{
    private int tokenValue = 1000; // Valor de cada Kred Token

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Notificar al CoinManager que se recogió una moneda
            KredsManager.Instance.AddTokens(tokenValue);
            Destroy(gameObject); // Destruir la moneda
        }
    }
}