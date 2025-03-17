using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private bool followPlayer = false;

    private Vector2 minBounds;
    private Vector2 maxBounds;
    private bool useHorizontalBounds = false; // Límite horizontal
    private bool useVerticalBounds = false;   // Límite vertical (nuevo)
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (player == null)
        {
            Debug.LogError("No se ha asignado el jugador en el CameraController.");
        }
    }

    void LateUpdate()
    {
        if (followPlayer && player != null)
        {
            Vector3 desiredPosition = new Vector3(player.position.x, player.position.y, transform.position.z);
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            float camHeight = 2f * cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            // Aplicar límites horizontales si están activos
            if (useHorizontalBounds)
            {
                smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x + camWidth / 2, maxBounds.x - camWidth / 2);
            }

            // Aplicar límites verticales si están activos
            if (useVerticalBounds)
            {
                smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y + camHeight / 2, maxBounds.y - camHeight / 2);
            }

            transform.position = smoothedPosition;
        }
    }

    public void SetFollowPlayer(bool shouldFollow)
    {
        followPlayer = shouldFollow;
    }

    // Método para establecer límites personalizados
    public void SetCameraBounds(Vector2 min, Vector2 max, bool limitHorizontal = true, bool limitVertical = true)
    {
        minBounds = min;
        maxBounds = max;
        useHorizontalBounds = limitHorizontal;
        useVerticalBounds = limitVertical;
    }

    public void ClearCameraBounds()
    {
        useHorizontalBounds = false;
        useVerticalBounds = false;
    }
}   