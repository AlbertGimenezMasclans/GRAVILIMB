using UnityEngine;

public class KredsBag : MonoBehaviour
{
    [SerializeField] private int bagValue = 5000;      // Cantidad de KredTokens a sumar (editable en el Inspector)
    [SerializeField] private float countDuration = 1.25f; // Duración del conteo en segundos (editable en el Inspector)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            KredsManager.Instance.AddTokens(bagValue, countDuration); // Sumar con duración personalizada
            Destroy(gameObject); // Destruir la bolsa al ser recogida
        }
    }
}