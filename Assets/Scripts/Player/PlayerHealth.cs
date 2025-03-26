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
    public Vector2 positionOffsetNormal = new Vector2(0f, 2f); // Offset cuando la gravedad es normal (arriba del jugador)
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

    [Header("Kreds Loss Settings")]
    [Tooltip("Mínima cantidad de Kreds a perder al alcanzar un umbral de vida")]
    public int minKredsToLose = 25; // Mínimo 25 Kreds
    [Tooltip("Máxima cantidad de Kreds a perder al alcanzar un umbral de vida")]
    public int maxKredsToLose = 200; // Máximo 200 Kreds

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

    // Variables para rastrear los umbrales de vida
    private bool hasLostAt75Percent = false; // Indica si ya perdió Kreds al 75%
    private bool hasLostAt50Percent = false; // Indica si ya perdió Kreds al 50%
    private bool hasLostAt25Percent = false; // Indica si ya perdió Kreds al 25%
    private float previousHealth; // Vida anterior para comparar cambios

    void Start()
    {
        // Inicializar la vida al máximo
        currentHealth = maxHealth;
        previousHealth = currentHealth; // Inicializar la vida anterior

        // Obtener el Transform del jugador
        playerTransform = transform;
        if (playerTransform == null)
        {
            UnityEngine.Debug.LogError("playerTransform es null. Asegúrate de que el script esté adjunto a un GameObject.", this);
            return;
        }

        // Obtener el Rigidbody2D del jugador para detectar la gravedad
        playerRigidbody = GetComponent<Rigidbody2D>();
        if (playerRigidbody == null)
        {
            UnityEngine.Debug.LogError("Rigidbody2D no encontrado en el jugador. Asegúrate de que el jugador tenga un Rigidbody2D.", this);
        }

        // Obtener el script PlayerMovement para detectar la gravedad
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            UnityEngine.Debug.LogError("PlayerMovement no encontrado en el jugador. Asegúrate de que el jugador tenga el script PlayerMovement.", this);
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
                        UnityEngine.Debug.LogError($"TMP_Text no encontrado en {objectsToHide[i].name}.", this);
                    }
                    if (healthCounterTransform == null)
                    {
                        UnityEngine.Debug.LogError($"Transform no encontrado en {objectsToHide[i].name}.", this);
                    }
                    if (healthCounterCanvas == null)
                    {
                        UnityEngine.Debug.LogError($"Canvas no encontrado en {objectsToHide[i].name}. Asegúrate de que el HealthCounter sea un objeto TextMeshPro en el espacio del mundo.", this);
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
                        UnityEngine.Debug.Log($"HealthCounter detectado: {healthCounterTransform.name}, Sorting Layer: {healthCounterCanvas.sortingLayerName}, Sorting Order: {healthCounterCanvas.sortingOrder}, Z Position: {healthCounterTransform.position.z}");
                        // Asegurarse de que la escala del HealthCounter sea la especificada
                        healthCounterTransform.localScale = new Vector3(healthCounterScale, healthCounterScale, healthCounterScale);
                    }
                }
            }
        }

        // Obtener la referencia al script HealthBar
        healthBar = FindObjectOfType<HealthBar>();
        if (healthBar == null)
        {
            UnityEngine.Debug.LogError("HealthBar no encontrado en la escena.", this);
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
            UnityEngine.Debug.LogError("PlayerDeath no encontrado en el jugador.", this);
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
            UnityEngine.Debug.LogError("playerTransform es null en Update().", this);
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
                        UnityEngine.Debug.LogWarning("healthCounterTransform es null. Asegúrate de que HealthCounter esté en objectsToHide y que su nombre sea exactamente 'HealthCounter'.");
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
            currentHealth = 0f;
            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth);
                // Actualizar el texto del contador
                UpdateHealthCounterText();
                // Hacer los objetos visibles al colisionar con DeathZone
                ShowObjects();
            }
            Die();
        }
    }

    public void TakeDamage(float damage)
    {
        // Guardar la vida anterior para comparar
        previousHealth = currentHealth;

        // Reducir la vida
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth); // Asegurarse de que no baje de 0 ni exceda el máximo

        // Actualizar la barra de vida y hacer los objetos visibles
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
            // Actualizar el texto del contador
            UpdateHealthCounterText();
            ShowObjects();
        }

        // Verificar si se cruzaron los umbrales de vida y perder Kreds si es necesario
        CheckHealthThresholds();

        // Comprobar si el jugador ha muerto
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void CheckHealthThresholds()
    {
        // Solo perder Kreds si el jugador no está muerto
        if (playerDeath != null && playerDeath.IsDead()) return;

        // Calcular los umbrales de vida
        float healthPercentage = currentHealth / maxHealth;
        float previousHealthPercentage = previousHealth / maxHealth;

        // Umbral del 75%
        if (previousHealthPercentage > 0.75f && healthPercentage <= 0.75f && !hasLostAt75Percent)
        {
            hasLostAt75Percent = true;
            LoseKreds();
            Debug.Log("Perdió Kreds al 75% de vida.");
        }

        // Umbral del 50%
        if (previousHealthPercentage > 0.50f && healthPercentage <= 0.50f && !hasLostAt50Percent)
        {
            hasLostAt50Percent = true;
            LoseKreds();
            Debug.Log("Perdió Kreds al 50% de vida.");
        }

        // Umbral del 25%
        if (previousHealthPercentage > 0.25f && healthPercentage <= 0.25f && !hasLostAt25Percent)
        {
            hasLostAt25Percent = true;
            LoseKreds();
            Debug.Log("Perdió Kreds al 25% de vida.");
        }
    }

    private void LoseKreds()
    {
        // Verificar si hay un KredsManager
        if (KredsManager.Instance == null)
        {
            Debug.LogWarning("KredsManager no encontrado. No se pueden perder Kreds.");
            return;
        }

        // Generar una cantidad aleatoria de Kreds a perder (entre 25 y 200, en incrementos de 25)
        int kredsToLose = Random.Range(minKredsToLose, maxKredsToLose + 1);
        kredsToLose = (kredsToLose / 25) * 25; // Asegurarse de que sea un múltiplo de 25

        // Ajustar la cantidad de Kreds a perder según los Kreds disponibles
        int availableTokens = KredsManager.Instance.totalTokens;
        if (kredsToLose > availableTokens)
        {
            kredsToLose = availableTokens; // No perder más de lo que tiene
        }

        // Si no hay Kreds que perder, no hacer nada
        if (kredsToLose <= 0)
        {
            return;
        }

        // Restar los Kreds y actualizar la UI
        KredsManager.Instance.LoseTokens(kredsToLose);
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

    // Método para reiniciar los umbrales (por ejemplo, al respawnear)
    public void ResetThresholds()
    {
        hasLostAt75Percent = false;
        hasLostAt50Percent = false;
        hasLostAt25Percent = false;
        previousHealth = currentHealth;
    }
}