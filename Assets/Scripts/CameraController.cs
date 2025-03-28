using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Transform headTarget;
    public Transform habSelector;

    [SerializeField] private Vector3 offset;
    public float minX = float.MinValue;
    public float maxX = float.MaxValue;
    public float lookAheadFactor = 2f;
    public float lookAheadSpeed = 0.1f;

    private float initialMinX;
    private float initialMaxX;
    private float lookAheadOffset = 0f;
    private PlayerMovement playerMovement;
    private PlayerDeath playerDeath;
    private Zone currentZone;

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

        playerDeath = target.GetComponent<PlayerDeath>();
        if (playerDeath == null)
        {
            Debug.LogError("El target no tiene el componente PlayerDeath.");
        }

        offset = transform.position - target.position;

        initialMinX = minX;
        initialMaxX = maxX;
    }

    void LateUpdate()
    {
        if (target == null || (playerDeath != null && playerDeath.IsDead())) return;

        // Buscar la zona actual del jugador
        Vector2 targetPosition = target.position;
        Zone newZone = FindZoneAtPosition(targetPosition);

        // Actualizar los límites de la cámara si la zona ha cambiado
        if (newZone != currentZone)
        {
            currentZone = newZone;
            if (currentZone != null)
            {
                float newMinX, newMaxX;
                currentZone.GetCameraLimits(out newMinX, out newMaxX);
                minX = newMinX;
                maxX = newMaxX;
                Debug.Log($"Límites de la cámara actualizados según la zona ({currentZone.gameObject.name}): minX = {minX}, maxX = {maxX}");
            }
            else
            {
                minX = initialMinX;
                maxX = initialMaxX;
                Debug.Log($"No se encontró una zona en la posición {targetPosition}. Restaurando límites iniciales: minX = {minX}, maxX = {maxX}");
            }
        }

        // Determinar qué seguir: jugador o cabeza
        Transform currentTarget = target;
        if (playerMovement != null && playerMovement.isDismembered && headTarget != null)
        {
            currentTarget = headTarget;
        }

        // Calcular el desplazamiento dinámico (look-ahead) basado en la dirección del movimiento
        float targetVelocityX = currentTarget.GetComponent<Rigidbody2D>()?.velocity.x ?? 0f;
        float targetLookAhead = Mathf.Lerp(lookAheadOffset, targetVelocityX * lookAheadFactor, lookAheadSpeed);
        lookAheadOffset = targetLookAhead;

        // Calcular la posición deseada de la cámara
        Vector3 desiredPosition = new Vector3(
            currentTarget.position.x + offset.x + lookAheadOffset,
            transform.position.y,
            transform.position.z
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
                habSelector.position.z
            );
        }
    }

    public void TeleportCamera(Vector2 newPosition)
    {
        Vector3 cameraPosition = new Vector3(newPosition.x, transform.position.y, transform.position.z);
        transform.position = cameraPosition;
        Debug.Log($"Cámara teletransportada a: {cameraPosition}");

        // Actualizar los límites de la cámara según la zona en la nueva posición
        Zone newZone = FindZoneAtPosition(newPosition);
        if (newZone != null)
        {
            currentZone = newZone;
            float newMinX, newMaxX;
            newZone.GetCameraLimits(out newMinX, out newMaxX);
            minX = newMinX;
            maxX = newMaxX;
            Debug.Log($"Límites de la cámara actualizados después de teletransportar según la zona ({currentZone.gameObject.name}): minX = {minX}, maxX = {maxX}");
        }
        else
        {
            minX = initialMinX;
            maxX = initialMaxX;
            Debug.Log($"No se encontró una zona en la posición {newPosition}. Restaurando límites iniciales: minX = {minX}, maxX = {maxX}");
        }
    }

    public void UpdateCameraLimits(float newMinX, float newMaxX)
    {
        minX = newMinX;
        maxX = newMaxX;
        Debug.Log($"Nuevos límites de la cámara: minX = {minX}, maxX = {maxX}");
    }

    public bool IsPositionWithinLimits(Vector2 position)
    {
        return position.x >= minX && position.x <= maxX;
    }

    public void RestoreInitialLimits()
    {
        minX = initialMinX;
        maxX = initialMaxX;
        Debug.Log($"Límites de la cámara restaurados a los iniciales: minX = {minX}, maxX = {maxX}");
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
}