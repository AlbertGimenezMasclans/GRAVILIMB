using UnityEngine;
using System.Collections;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Time (in seconds) the checkpoint remains in cooldown after healing the player")]
    public float healCooldown = 10f;

    [Header("Zone Management")]
    [Tooltip("The visual zone (GameObject) associated with this checkpoint")]
    public GameObject visualZone;

    private Animator animator;
    private bool isActivated = false;
    private bool isInCooldown = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator no encontrado en el Checkpoint.", this);
        }
        else
        {
            Debug.Log("Checkpoint inicializado: en estado 'Checkpoint_Idle'.");
        }

        Vector2 checkpointPosition = transform.position;
        Zone zone = FindZoneAtPosition(checkpointPosition);
        if (zone == null)
        {
            Debug.LogWarning($"El checkpoint {gameObject.name} en la posición {checkpointPosition} no está dentro de ninguna zona de cámara. Asegúrate de que esté cubierto por una zona para que los límites de la cámara se establezcan correctamente al reaparecer.");
        }
        else
        {
            Debug.Log($"El checkpoint {gameObject.name} está dentro de la zona de cámara {zone.gameObject.name}.");
        }

        if (visualZone == null)
        {
            Debug.LogWarning($"VisualZone no asignado en el checkpoint {gameObject.name}. No se activará ninguna zona visual al reaparecer en este checkpoint.");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // --- AÑADIDO: Detección mejorada para cabeza desmembrada ---
        GameObject playerObject = null;
        bool isHead = collision.CompareTag("PlayerHead");
        bool isPlayer = collision.CompareTag("Player");

        if (isHead)
        {
            PlayerMovement playerMovement = collision.GetComponentInParent<PlayerMovement>();
            if (playerMovement != null && playerMovement.isDismembered)
            {
                playerObject = playerMovement.gameObject;
                Debug.Log("Checkpoint activado por cabeza desmembrada");
            }
        }
        else if (isPlayer)
        {
            playerObject = collision.gameObject;
        }

        if (playerObject == null) return;

        // --- COMPORTAMIENTO ORIGINAL (SIN MODIFICAR) ---
        if (!isActivated)
        {
            if (animator != null)
            {
                animator.SetBool("Active", true);
                Debug.Log("Bool 'Active' establecido a true para reproducir 'Checkpoint-Active'.");
                StartCoroutine(TransitionToCheckpointC());
            }
            else
            {
                Debug.LogError("Animator es null. No se puede establecer el bool.");
            }

            isActivated = true;

            PlayerDeath playerDeath = playerObject.GetComponent<PlayerDeath>();
            if (playerDeath != null)
            {
                Vector3 checkpointPosition = transform.position;
                Vector3 newSpawnPosition = new Vector3(checkpointPosition.x, checkpointPosition.y + 0.50f, checkpointPosition.z);
                playerDeath.SetNewSpawnPositionAndCamera(newSpawnPosition, checkpointPosition.x, this);
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

        // --- COMPORTAMIENTO ORIGINAL DE CURA (SIN MODIFICAR) ---
        if (isPlayer) // Solo cura si es el Player completo
        {
            PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();
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
    }

    // --- MÉTODOS ORIGINALES (SIN MODIFICAR) ---
    private IEnumerator TransitionToCheckpointC()
    {
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Checkpoint-Active"));
        Debug.Log("Estado 'Checkpoint-Active' detectado.");

        float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        Debug.Log($"Duración de 'Checkpoint-Active': {animationLength} segundos.");

        yield return new WaitForSeconds(animationLength);

        animator.SetBool("IsCheckpointC", true);
        Debug.Log("Bool 'IsCheckpointC' establecido a true. Transicionando a 'Checkpoint-C'.");
    }

    private IEnumerator HealPlayer(PlayerHealth playerHealth)
    {
        isInCooldown = true;

        playerHealth.ShowObjects();
        Debug.Log("Barra de vida y contador mostrados con la vida actual.");

        playerHealth.currentHealth = playerHealth.maxHealth;

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

        yield return new WaitForSeconds(1f);

        playerHealth.ForceHideObjects();
        Debug.Log("Barra de vida y contador ocultados después de la curación.");

        yield return new WaitForSeconds(healCooldown);

        isInCooldown = false;
        Debug.Log($"Cooldown del checkpoint finalizado. Puede curar de nuevo. (Cooldown: {healCooldown} segundos)");
    }

    public Vector2 GetPosition()
    {
        return transform.position;
    }

    private Zone FindZoneAtPosition(Vector2 position)
    {
        Zone[] zones = FindObjectsOfType<Zone>();
        foreach (Zone zone in zones)
        {
            if (zone.ContainsPosition(position))
            {
                return zone;
            }
        }
        return null;
    }

    public GameObject GetVisualZone()
    {
        return visualZone;
    }
}