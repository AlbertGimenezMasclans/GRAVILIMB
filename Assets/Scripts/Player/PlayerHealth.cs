using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of the player")]
    public float maxHealth = 20f;
    [Tooltip("Current health of the player")]
    public float currentHealth;

    [Header("Visibility Settings")]
    [Tooltip("GameObjects whose SpriteRenderers or TMP_Text will be made visible/invisible and whose rotation will be fixed")]
    public GameObject[] objectsToHide; // Array de GameObjects a hacer visibles/invisibles y fijar rotación

    [Header("Position Offset")]
    [Tooltip("Offset from the player's position where the objects should be positioned when gravity is normal")]
    public Vector2 positionOffsetNormal = new Vector2(0f, 2f); // Offset cuando la gravedad.ConcurrentModificationException normal (arriba del jugador)
    [Tooltip("Offset from the player's position where the objects should be positioned when gravity is inverted")]
    public Vector2 positionOffsetInverted = new Vector2(0f, -2f); // Offset cuando la gravedad está invertida (abajo del jugador)

    [Header("Health Counter Settings")]
    [Tooltip("Vertical offset for the HealthCounter relative to the base position offset when gravity is normal")]
    public float healthCounterVerticalOffsetNormal = 0.3f; // Offset vertical para el HealthCounter (gravedad normal)
    [Tooltip("Vertical offset for the HealthCounter relative to the base position offset when gravity is inverted")]
    public float healthCounterVerticalOffsetInverted = -0.3f; // Offset vertical para el HealthCounter (gravedad invertida)
    [Tooltip("Horizontal offset for the HealthCounter relative to the base position offset")]
    public float healthCounterHorizontalOffset = 0f; // Offset horizontal para el HealthCounter
    [Tooltip("Scale of the HealthCounter (affects the size of the text)")]
    public float healthCounterScale = 0.05f; // Escala del HealthCounter (ajusta para cambiar el tamaño del texto)
    [Tooltip("Sorting Order for the HealthCounter to ensure it renders on top of everything")]
    public int healthCounterSortingOrder = 100; // Sorting Order alto para el HealthCounter
    [Tooltip("Z position for the HealthCounter to ensure it is closer to the camera")]
    public float healthCounterZPosition = -5f; // Posición Z para acercar el HealthCounter a la cámara

    [Header("Damage Effect Settings")]
    [Tooltip("Color al recibir daño")]
    public Color damageColor = Color.red; // Color rojo por defecto
    [Tooltip("Duración del efecto de color al recibir daño (en segundos)")]
    public float damageColorDuration = 0.4f; // Duración de 0.4 segundos

    private HealthBar healthBar; // Referencia al script HealthBar
    private PlayerDeath playerDeath; // Referencia al script PlayerDeath
    private PlayerMovement playerMovement; // Referencia al script PlayerMovement
    private Coroutine hideHealthBarCoroutine; // Corutina para ocultar los objetos
    private SpriteRenderer[] spriteRenderers; // Array de SpriteRenderers de los objetos
    private TMP_Text[] textRenderers; // Array de TMP_Text para los contadores de texto
    private Transform[] objectTransforms; // Array de Transforms de los objetos para fijar rotación y posición
    private Transform playerTransform; // Transform del jugador
    private TMP_Text healthCounterText; // Referencia al componente TextMeshPro del contador
    private Transform healthCounterTransform; // Referencia al Transform del HealthCounter
    private Canvas healthCounterCanvas; // Referencia al Canvas del HealthCounter
    private Rigidbody2D playerRigidbody; // Referencia al Rigidbody2D del jugador para detectar la gravedad
    private bool isGravityInverted; // Estado de la gravedad
    private SpriteRenderer[] playerSpriteRenderers; // Array de SpriteRenderers del jugador y sus hijos
    private Color[] originalColors; // Colores originales de los SpriteRenderers del jugador
    private bool isChangingColor; // Para evitar que se solapen los efectos de color

    void Start()
    {
        // Inicializar la vida al máximo
        currentHealth = maxHealth;

        // Obtener el Transform del jugador
        playerTransform = transform;
        if (playerTransform == null)
        {
            Debug.LogError("playerTransform es null. Asegúrate de que el script esté adjunto a un GameObject.", this);
            return;
        }

        // Obtener el Rigidbody2D del jugador para detectar la gravedad
        playerRigidbody = GetComponent<Rigidbody2D>();
        if (playerRigidbody == null)
        {
            Debug.LogError("Rigidbody2D no encontrado en el jugador. Asegúrate de que el jugador tenga un Rigidbody2D.", this);
        }

        // Obtener el script PlayerMovement para detectar la gravedad
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement no encontrado en el jugador. Asegúrate de que el jugador tenga el script PlayerMovement.", this);
        }

        // Inicializar los arrays para SpriteRenderers, TMP_Text y Transforms
        spriteRenderers = new SpriteRenderer[objectsToHide.Length];
        textRenderers = new TMP_Text[objectsToHide.Length];
        objectTransforms = new Transform[objectsToHide.Length];

        for (int i = 0; i < objectsToHide.Length; i++)
        {
            if (objectsToHide[i] != null)
            {
                // Obtener el SpriteRenderer (si existe)
                spriteRenderers[i] = objectsToHide[i].GetComponent<SpriteRenderer>();
                // Obtener el TMP_Text (si existe)
                textRenderers[i] = objectsToHide[i].GetComponent<TMP_Text>();
                // Obtener el Transform
                objectTransforms[i] = objectsToHide[i].transform;

                // Asegurarse de que el objeto no sea hijo del jugador
                if (objectTransforms[i].parent == playerTransform)
                {
                    objectTransforms[i].SetParent(null);
                }

                // Buscar el HealthCounter
                if (objectsToHide[i].name == "HealthCounter")
                {
                    healthCounterText = objectsToHide[i].GetComponent<TMP_Text>();
                    healthCounterTransform = objectsToHide[i].transform;
                    healthCounterCanvas = objectsToHide[i].GetComponent<Canvas>(); // Obtener el Canvas del HealthCounter
                    if (healthCounterText == null)
                    {
                        Debug.LogError($"TMP_Text no encontrado en {objectsToHide[i].name}.", this);
                    }
                    if (healthCounterTransform == null)
                    {
                        Debug.LogError($"Transform no encontrado en {objectsToHide[i].name}.", this);
                    }
                    if (healthCounterCanvas == null)
                    {
                        Debug.LogError($"Canvas no encontrado en {objectsToHide[i].name}. Asegúrate de que el HealthCounter sea un objeto TextMeshPro en el espacio del mundo.", this);
                    }
                    else
                    {
                        // Establecer el Sorting Layer a uno superior (por ejemplo, "UI")
                        healthCounterCanvas.sortingLayerName = "UI"; // Asegúrate de que este Sorting Layer exista
                        // Establecer el Sorting Order del Canvas para que el HealthCounter se dibuje por encima de todo
                        healthCounterCanvas.sortingOrder = healthCounterSortingOrder;
                        // Ajustar la posición Z para que esté más cerca de la cámara
                        Vector3 currentPosition = healthCounterTransform.position;
                        healthCounterTransform.position = new Vector3(currentPosition.x, currentPosition.y, healthCounterZPosition);
                        Debug.Log($"HealthCounter detectado: {healthCounterTransform.name}, Sorting Layer: {healthCounterCanvas.sortingLayerName}, Sorting Order: {healthCounterCanvas.sortingOrder}, Z Position: {healthCounterTransform.position.z}");
                        // Asegurarse de que la escala del HealthCounter sea la especificada
                        healthCounterTransform.localScale = new Vector3(healthCounterScale, healthCounterScale, healthCounterScale);
                    }
                }
            }
        }

        // Obtener todos los SpriteRenderers del jugador y sus hijos
        playerSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        if (playerSpriteRenderers.Length == 0)
        {
            Debug.LogError("No se encontraron SpriteRenderers en el jugador o sus hijos. No se podrá cambiar el color al recibir daño.", this);
        }
        else
        {
            // Guardar los colores originales de todos los SpriteRenderers
            originalColors = new Color[playerSpriteRenderers.Length];
            for (int i = 0; i < playerSpriteRenderers.Length; i++)
            {
                originalColors[i] = playerSpriteRenderers[i].color;
            }
        }

        // Obtener la referencia al script HealthBar
        healthBar = FindObjectOfType<HealthBar>();
        if (healthBar == null)
        {
            Debug.LogError("HealthBar no encontrado en la escena.", this);
        }
        else
        {
            // Inicializar la barra de vida
            healthBar.SetMaxHealth(maxHealth);
            healthBar.UpdateHealth(currentHealth);
            // Actualizar el texto del contador
            UpdateHealthCounterText();
            // Hacer los objetos invisibles al inicio
            HideObjects();
        }

        // Obtener la referencia al script PlayerDeath
        playerDeath = GetComponent<PlayerDeath>();
        if (playerDeath == null)
        {
            Debug.LogError("PlayerDeath no encontrado en el jugador.", this);
        }
    }

    void Update()
    {
        // Perder vida al presionar la tecla "I" (para probar)
        if (Input.GetKeyDown(KeyCode.I))
        {
            TakeDamage(5f); // Puedes ajustar la cantidad de daño
        }

        // Asegurarse de que playerTransform no sea null
        if (playerTransform == null)
        {
            Debug.LogError("playerTransform es null en Update().", this);
            return;
        }

        // Detectar si la gravedad está invertida usando PlayerMovement
        if (playerMovement != null)
        {
            isGravityInverted = !playerMovement.IsGravityNormal();
        }
        else if (playerRigidbody != null)
        {
            isGravityInverted = playerRigidbody.gravityScale < 0; // Fallback si PlayerMovement no está disponible
        }

        // Seleccionar los offsets según el estado de la gravedad
        Vector2 currentPositionOffset = isGravityInverted ? positionOffsetInverted : positionOffsetNormal;
        float currentHealthCounterVerticalOffset = isGravityInverted ? healthCounterVerticalOffsetInverted : healthCounterVerticalOffsetNormal;

        // Obtener la posición del jugador una sola vez
        Vector3 playerPosition = playerTransform.position;

        // Actualizar la posición y rotación de todos los objetos en objectsToHide
        for (int i = 0; i < objectTransforms.Length; i++)
        {
            Transform objTransform = objectTransforms[i];
            if (objTransform != null)
            {
                // Posicionar el HealthCounter respecto al jugador con su propio offset
                if (objectsToHide[i].name == "HealthCounter")
                {
                    if (healthCounterTransform != null)
                    {
                        // Posicionar el HealthCounter respecto al jugador
                        healthCounterTransform.position = new Vector3(
                            playerPosition.x + currentPositionOffset.x + healthCounterHorizontalOffset,
                            playerPosition.y + currentPositionOffset.y + currentHealthCounterVerticalOffset,
                            healthCounterZPosition // Usar la Z especificada
                        );
                        // Asegurarse de que la escala del HealthCounter sea constante
                        healthCounterTransform.localScale = new Vector3(healthCounterScale, healthCounterScale, healthCounterScale);
                        // No rotar el HealthCounter, mantenerlo siempre en su rotación original
                        healthCounterTransform.rotation = Quaternion.identity;
                        Debug.Log($"HealthCounter posición: {healthCounterTransform.position}, isGravityInverted: {isGravityInverted}");
                    }
                    else
                    {
                        Debug.LogWarning("healthCounterTransform es null. Asegúrate de que HealthCounter esté en objectsToHide y que su nombre sea exactamente 'HealthCounter'.");
                    }
                    continue;
                }

                // Actualizar la posición para los otros objetos (como HealthBar y HealthBar_Fill)
                objTransform.position = new Vector3(
                    playerPosition.x + currentPositionOffset.x,
                    playerPosition.y + currentPositionOffset.y,
                    objTransform.position.z // Mantener la Z original del objeto
                );

                // Ajustar la rotación según la gravedad para los otros objetos (HealthBar y HealthBar_Fill)
                objTransform.rotation = isGravityInverted ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
                Debug.Log($"{objTransform.name} rotación: {objTransform.rotation.eulerAngles.z} (isGravityInverted: {isGravityInverted})");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Si colisiona con una DeathZone, reducir la vida a 0
        if (collision.CompareTag("DeathZone"))
        {
            // Calcular la vida después del daño
            float newHealth = 0f; // La DeathZone reduce la vida a 0 directamente

            // Actualizar la barra de vida y hacer los objetos visibles
            currentHealth = newHealth;
            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth);
                UpdateHealthCounterText();
                ShowObjects();
            }

            // No aplicamos el efecto rojo porque este daño matará al jugador
            Debug.Log("Colisión con DeathZone. No se aplica el efecto rojo.");

            // Llamar a Die() directamente
            Die();
        }
    }

    public void TakeDamage(float damage)
    {
        // Si el jugador ya está muerto, no aplicar daño
        if (playerDeath != null && playerDeath.IsDead())
        {
            Debug.Log("El jugador ya está muerto. No se aplica daño.");
            return;
        }

        // Calcular la vida después del daño
        float newHealth = currentHealth - damage;

        // Actualizar la barra de vida y hacer los objetos visibles
        currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth); // Asegurarse de que no baje de 0 ni exceda el máximo
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
            UpdateHealthCounterText();
            ShowObjects();
        }

        // Cambiar el color del jugador a rojo por 0.4 segundos solo si el daño no lo mata
        if (newHealth > 0f && playerSpriteRenderers != null && playerSpriteRenderers.Length > 0 && !isChangingColor)
        {
            StartCoroutine(ChangeColorOnDamage());
        }
        else if (newHealth <= 0f)
        {
            Debug.Log("El daño matará al jugador. No se aplica el efecto rojo.");
        }

        // Comprobar si el jugador ha muerto
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private IEnumerator ChangeColorOnDamage()
    {
        isChangingColor = true;

        // Cambiar el color de todos los SpriteRenderers del jugador a rojo
        foreach (SpriteRenderer renderer in playerSpriteRenderers)
        {
            if (renderer != null)
            {
                renderer.color = damageColor;
            }
        }

        // Esperar 0.4 segundos
        yield return new WaitForSeconds(damageColorDuration);

        // Restaurar los colores originales
        for (int i = 0; i < playerSpriteRenderers.Length; i++)
        {
            if (playerSpriteRenderers[i] != null)
            {
                playerSpriteRenderers[i].color = originalColors[i];
            }
        }

        isChangingColor = false;
    }

    private void Die()
    {
        // Si el jugador ya está muerto, no hacer nada
        if (playerDeath != null && playerDeath.IsDead()) return;

        // Llamar a la lógica de muerte de PlayerDeath
        if (playerDeath != null)
        {
            StartCoroutine(playerDeath.RespawnCoroutine());
        }
    }

    private IEnumerator HideObjectsAfterDelay()
    {
        // Esperar 1 segundo
        yield return new WaitForSeconds(1f);
        // Hacer los objetos invisibles si el jugador no está muerto
        if (playerDeath == null || !playerDeath.IsDead())
        {
            HideObjects();
        }
    }

    // Método público para hacer los objetos visibles (usado al perder vida)
    public void ShowObjects()
    {
        // Habilitar los SpriteRenderers
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
        // Habilitar los TMP_Text
        foreach (TMP_Text text in textRenderers)
        {
            if (text != null)
            {
                text.enabled = true;
            }
        }
        // Reiniciar la corutina para ocultar los objetos
        if (hideHealthBarCoroutine != null)
        {
            StopCoroutine(hideHealthBarCoroutine);
        }
        hideHealthBarCoroutine = StartCoroutine(HideObjectsAfterDelay());
    }

    // Método privado para hacer los objetos invisibles
    private void HideObjects()
    {
        // Deshabilitar los SpriteRenderers
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
        // Deshabilitar los TMP_Text
        foreach (TMP_Text text in textRenderers)
        {
            if (text != null)
            {
                text.enabled = false;
            }
        }
    }

    // Método público para forzar la invisibilidad de los objetos (usado por PlayerDeath)
    public void ForceHideObjects()
    {
        // Detener la corutina de ocultar si está activa
        if (hideHealthBarCoroutine != null)
        {
            StopCoroutine(hideHealthBarCoroutine);
        }
        // Hacer los objetos invisibles inmediatamente
        HideObjects();
    }

    // Método para actualizar el texto del contador de vida
    public void UpdateHealthCounterText()
    {
        if (healthCounterText != null)
        {
            healthCounterText.text = $"{(int)currentHealth} / {(int)maxHealth}";
        }
    }
}