using UnityEngine;
using System.Collections;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Time (in seconds) the checkpoint remains in cooldown after healing the player")]
    public float healCooldown = 10f; // Cooldown del checkpoint, 10 segundos

    [Header("Camera Limits for this Checkpoint")]
    [Tooltip("Minimum X limit for the camera in this checkpoint's zone")]
    public float minXLimit = -10f; // Límite izquierdo
    [Tooltip("Maximum X limit for the camera in this checkpoint's zone")]
    public float maxXLimit = 10f;  // Límite derecho

    private Animator animator; // Referencia al Animator del checkpoint
    private bool isActivated = false; // Indica si el checkpoint ya ha sido activado (para el cambio de spawn)
    private bool isInCooldown = false; // Indica si el checkpoint está en cooldown para curar

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
        // Comprobar si el objeto que colisiona es el jugador
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Colisión detectada con el jugador.");

            // Actualizar la posición inicial del jugador solo si el checkpoint no ha sido activado
            if (!isActivated)
            {
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

                // Marcar el checkpoint como activado (para el cambio de spawn)
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
                Debug.Log("Checkpoint ya activado para cambio de spawn, pero se verificará si puede curar.");
            }

            // Curar al jugador si su vida es menor que la máxima y el checkpoint no está en cooldown
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                if (playerHealth.currentHealth < playerHealth.maxHealth && !isInCooldown)
                {
                    Debug.Log($"Vida actual ({playerHealth.currentHealth}) es menor que la máxima ({playerHealth.maxHealth}). Curando al jugador...");
                    StartCoroutine(HealPlayer(playerHealth));
                }
                else if (playerHealth.currentHealth >= playerHealth.maxHealth)
                {
                    Debug.Log("El jugador ya tiene la vida máxima. No se necesita curar.");
                }
                else if (isInCooldown)
                {
                    Debug.Log("El checkpoint está en cooldown. No se puede curar todavía.");
                }
            }
            else
            {
                Debug.LogError("PlayerHealth no encontrado en el jugador.");
            }
        }
        else
        {
            Debug.Log("Colisión ignorada: no es el jugador.");
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

    private IEnumerator HealPlayer(PlayerHealth playerHealth)
    {
        // Marcar el checkpoint como en cooldown
        isInCooldown = true;

        // Mostrar la barra de vida y el contador con la vida actual
        playerHealth.ShowObjects();
        Debug.Log("Barra de vida y contador mostrados con la vida actual.");

        // Curar al jugador instantáneamente
        playerHealth.currentHealth = playerHealth.maxHealth;

        // Actualizar la barra de vida y el contador
        HealthBar healthBar = FindObjectOfType<HealthBar>();
        if (healthBar != null)
        {
            healthBar.UpdateHealth(playerHealth.currentHealth);
            playerHealth.UpdateHealthCounterText();
            Debug.Log($"Jugador curado completamente. Vida actual: {playerHealth.currentHealth}");
        }
        else
        {
            Debug.LogError("HealthBar no encontrado en la escena.");
            playerHealth.UpdateHealthCounterText();
        }

        // Esperar 1 segundo antes de ocultar la barra de vida (como en el comportamiento de PlayerHealth)
        yield return new WaitForSeconds(1f);

        // Ocultar la barra de vida y el contador
        playerHealth.ForceHideObjects();
        Debug.Log("Barra de vida y contador ocultados después de la curación.");

        // Iniciar el cooldown del checkpoint
        yield return new WaitForSeconds(healCooldown);

        // Finalizar el cooldown
        isInCooldown = false;
        Debug.Log($"Cooldown del checkpoint finalizado. Puede curar de nuevo. (Cooldown: {healCooldown} segundos)");
    }

    // Método para obtener la posición del checkpoint
    public Vector2 GetPosition()
    {
        return transform.position;
    }

    // Método para obtener los límites de la cámara
    public void GetCameraLimits(out float minX, out float maxX)
    {
        minX = minXLimit;
        maxX = maxXLimit;
    }
}