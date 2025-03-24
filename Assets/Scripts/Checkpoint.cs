using UnityEngine;
using System.Collections;

public class Checkpoint : MonoBehaviour
{
    private Animator animator; // Referencia al Animator del checkpoint
    private bool isActivated = false; // Indica si el checkpoint ya ha sido activado

    void Start()
    {
        // Obtener el Animator del checkpoint
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator no encontrado en el Checkpoint. Asegúrate de que el GameObject tenga un Animator asignado.", this);
        }
        else
        {
            Debug.Log("Checkpoint inicializado: en estado 'Checkpoint_Idle'.");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Comprobar si el objeto que colisiona es el jugador y si el checkpoint no está activado
        if (collision.CompareTag("Player") && !isActivated)
        {
            Debug.Log("Colisión detectada con el jugador.");

            // Activar la animación al colisionar
            if (animator != null)
            {
                animator.SetBool("Active", true); // Establecer el bool para "Checkpoint-Active"
                Debug.Log("Bool 'Active' establecido a true para reproducir 'Checkpoint-Active'.");

                // Iniciar la corrutina para forzar "Checkpoint-C"
                StartCoroutine(TransitionToCheckpointC());
            }
            else
            {
                Debug.LogError("Animator es null. No se puede establecer el bool.");
            }

            // Marcar el checkpoint como activado
            isActivated = true;

            // Actualizar la posición inicial del jugador
            PlayerDeath playerDeath = collision.GetComponent<PlayerDeath>();
            if (playerDeath != null)
            {
                Vector3 checkpointPosition = transform.position;
                Vector3 newSpawnPosition = new Vector3(checkpointPosition.x, checkpointPosition.y + 0.50f, checkpointPosition.z);
                playerDeath.SetNewSpawnPositionAndCamera(newSpawnPosition, checkpointPosition.x);
                Debug.Log($"Nueva posición inicial del jugador: {newSpawnPosition}");
            }
            else
            {
                Debug.LogError("PlayerDeath no encontrado en el jugador.");
            }
        }
        else
        {
            Debug.Log("Colisión ignorada: ya activado o no es el jugador.");
        }
    }

    private IEnumerator TransitionToCheckpointC()
    {
        // Esperar hasta que "Checkpoint-Active" comience
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Checkpoint-Active"));
        Debug.Log("Estado 'Checkpoint-Active' detectado.");

        // Obtener la duración de "Checkpoint-Active"
        float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        Debug.Log($"Duración de 'Checkpoint-Active': {animationLength} segundos.");

        // Esperar hasta que termine
        yield return new WaitForSeconds(animationLength);

        // Forzar la transición a "Checkpoint-C"
        animator.SetBool("IsCheckpointC", true);
        Debug.Log("Bool 'IsCheckpointC' establecido a true. Transicionando a 'Checkpoint-C'.");
    }
}