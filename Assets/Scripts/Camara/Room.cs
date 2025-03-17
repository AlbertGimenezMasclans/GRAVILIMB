using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;
    [SerializeField] private bool followPlayerInRoom = true;
    [SerializeField] private bool limitHorizontal = true; // Nuevo: limita el movimiento horizontal
    [SerializeField] private bool limitVertical = true;   // Nuevo: limita el movimiento vertical

    private CameraController cameraController;

    void Start()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController == null)
        {
            Debug.LogError("No se encontró CameraController en la cámara principal.");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            cameraController.SetFollowPlayer(followPlayerInRoom);
            if (followPlayerInRoom)
            {
                cameraController.SetCameraBounds(minBounds, maxBounds, limitHorizontal, limitVertical);
            }
            else
            {
                cameraController.ClearCameraBounds();
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Opcional: desactivar seguimiento al salir
            // cameraController.SetFollowPlayer(false);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector2 size = maxBounds - minBounds;
        Vector2 center = (minBounds + maxBounds) / 2;
        Gizmos.DrawWireCube(center, size);
    }
}