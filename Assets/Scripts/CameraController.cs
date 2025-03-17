using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Variable para asignar el jugador que seguir� la c�mara
    public Transform target;

    // Offset inicial entre la c�mara y el jugador
    private Vector3 offset;

    // L�mites horizontales para la c�mara
    public float minX = -10f; // L�mite izquierdo
    public float maxX = 10f;  // L�mite derecho

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Por favor asigna un target (jugador) a la c�mara.");
            return;
        }

        // Calcular el offset inicial entre la c�mara y el jugador
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calcular la nueva posici�n de la c�mara
        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            transform.position.y, // Mantiene la altura original
            transform.position.z  // Mantiene la profundidad original
        );

        // Aplicar l�mites horizontales
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);

        // Actualizar la posici�n de la c�mara
        transform.position = desiredPosition;
    }
}