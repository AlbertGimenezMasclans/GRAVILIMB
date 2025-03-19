using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Transform headTarget; // Asignar "PlayerHead" aquí en el Inspector
    public Transform habSelector; // Asignar "Hab-Selector" aquí en el Inspector

    [SerializeField] private Vector3 offset;
    public float minX = -10f; // Límite izquierdo
    public float maxX = 10f;  // Límite derecho
    public float lookAheadFactor = 2f; // Cuánto mira hacia adelante
    public float lookAheadSpeed = 0.1f; // Velocidad de ajuste del look-ahead

    private float lookAheadOffset = 0f; // Offset dinámico del look-ahead
    private PlayerMovement playerMovement; // Referencia al script del jugador

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Por favor asigna un target (jugador) a la cámara.");
            return;
        }

        playerMovement = target.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("El target no tiene el componente PlayerMovement.");
        }

        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Determinar qué seguir: jugador o cabeza
        Transform currentTarget = target;
        if (playerMovement != null && playerMovement.isDismembered && headTarget != null)
        {
            currentTarget = headTarget; // Cambiar al "PlayerHead" si está desmiembrado
        }

        // Calcular el desplazamiento dinámico (look-ahead) basado en la dirección del movimiento
        float targetVelocityX = currentTarget.GetComponent<Rigidbody2D>()?.velocity.x ?? 0f;
        float targetLookAhead = Mathf.Lerp(lookAheadOffset, targetVelocityX * lookAheadFactor, lookAheadSpeed);
        lookAheadOffset = targetLookAhead;

        // Calcular la posición deseada de la cámara
        Vector3 desiredPosition = new Vector3(
            currentTarget.position.x + offset.x + lookAheadOffset, // Incluye look-ahead
            transform.position.y, // Mantiene la altura original
            transform.position.z  // Mantiene la profundidad original
        );

        // Aplicar límites horizontales
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);

        // Establecer la posición de la cámara directamente
        transform.position = desiredPosition;

        // Hacer que el Hab-Selector siga a la cámara
        if (habSelector != null)
        {
            habSelector.position = new Vector3(
                transform.position.x,
                transform.position.y,
                habSelector.position.z // Mantener la Z original del Hab-Selector
            );
        }
    }
}