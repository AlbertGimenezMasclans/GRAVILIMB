using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Variable para asignar el jugador que seguirá la cámara
    public Transform target;

    // Offset inicial entre la cámara y el jugador
    private Vector3 offset;

    // Límites horizontales para la cámara
    public float minX = -10f; // Límite izquierdo
    public float maxX = 10f;  // Límite derecho

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Por favor asigna un target (jugador) a la cámara.");
            return;
        }

        // Calcular el offset inicial entre la cámara y el jugador
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calcular la nueva posición de la cámara
        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            transform.position.y, // Mantiene la altura original
            transform.position.z  // Mantiene la profundidad original
        );

        // Aplicar límites horizontales
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);

        // Actualizar la posición de la cámara
        transform.position = desiredPosition;
    }
}