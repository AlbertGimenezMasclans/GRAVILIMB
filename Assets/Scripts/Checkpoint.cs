using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private SpriteRenderer spriteRenderer; // Referencia al SpriteRenderer del checkpoint
    private bool isActivated = false; // Indica si el checkpoint ya ha sido activado

    void Start()
    {
        // Obtener el SpriteRenderer del checkpoint
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer no encontrado en el Checkpoint. Aseg�rate de que el GameObject tenga un SpriteRenderer.", this);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Comprobar si el objeto que colisiona es el jugador
        if (collision.CompareTag("Player") && !isActivated)
        {
            // Cambiar el color del sprite del checkpoint a verde
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.green;
                Debug.Log("Checkpoint activado: sprite cambiado a verde.");
            }

            // Marcar el checkpoint como activado para evitar que se active de nuevo
            isActivated = true;

            // Obtener el script PlayerDeath del jugador
            PlayerDeath playerDeath = collision.GetComponent<PlayerDeath>();
            if (playerDeath != null)
            {
                // Calcular la nueva posici�n inicial del jugador (posici�n del checkpoint + 0.75 en Y)
                Vector3 checkpointPosition = transform.position;
                Vector3 newSpawnPosition = new Vector3(checkpointPosition.x, checkpointPosition.y + 0.75f, checkpointPosition.z);

                // Notificar a PlayerDeath para que actualice la posici�n inicial del jugador y la c�mara
                playerDeath.SetNewSpawnPositionAndCamera(newSpawnPosition, checkpointPosition.x);
                Debug.Log($"Nueva posici�n inicial del jugador: {newSpawnPosition}, Nueva posici�n X de la c�mara: {checkpointPosition.x}");
            }
            else
            {
                Debug.LogError("PlayerDeath no encontrado en el jugador. Aseg�rate de que el jugador tenga el script PlayerDeath.", this);
            }
        }
    }
}