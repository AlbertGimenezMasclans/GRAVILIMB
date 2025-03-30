using UnityEngine;

public class RecomposeGizmo : MonoBehaviour
{
    public float detectionRadius = 0.2f; // Radio de detección, ajustable en el Inspector
    private PlayerMovement playerMovement;
    private GameObject head;

    void Start()
    {
        // Buscar PlayerMovement en la escena (asumiendo que hay solo un jugador)
        playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("No se encontró PlayerMovement en la escena. Asegúrate de que el jugador tenga el componente PlayerMovement.");
            return;
        }

        head = playerMovement.headObject;
        if (head == null)
        {
            Debug.LogError("headObject no está asignado en PlayerMovement.");
        }
    }

    void Update()
    {
        // Solo ejecutar si playerMovement y head están asignados
        if (playerMovement != null && head != null && playerMovement.isDismembered)
        {
            // Calcular la distancia entre "recompose" y la cabeza
            float distance = Vector2.Distance(transform.position, head.transform.position);
            if (distance <= detectionRadius)
            {
                playerMovement.RecomposePlayer();
            }
        }
    }

    // Dibujar el Gizmo en el Editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green; // Color del Gizmo
        Gizmos.DrawWireSphere(transform.position, detectionRadius); // Esfera que representa el rango
    }
}