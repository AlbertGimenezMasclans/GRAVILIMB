using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerDeath : MonoBehaviour
{
    [Header("Death Settings")]
    [Tooltip("Time to wait before respawning (in seconds)")]
    public float respawnTime = 3f;
    [Tooltip("Reference to the PlayerMovement script")]
    public PlayerMovement playerMovement;
    [Tooltip("Reference to the main camera")]
    public Camera mainCamera;
    [Tooltip("Reference to the CameraController script")]
    public CameraController cameraController;

    [Header("Fade Settings")]
    [Tooltip("UI Panel for fade effect")]
    public Image fadePanel;
    [Tooltip("Time for fade in (black screen appears)")]
    public float fadeInTime = 1f;
    [Tooltip("Time for fade out (black screen disappears)")]
    public float fadeOutTime = 1f;
    [Tooltip("Delay before fade in starts")]
    public float fadeInDelay = 2f;
    [Tooltip("Time black screen remains fully visible")]
    public float blackScreenDuration = 3f;

    [Header("Death UI Settings")]
    [Tooltip("Reference to the KredsManager script (for coin UI)")]
    public KredsManager kredsManager;
    [Tooltip("UI GameObject to show during death")]
    public GameObject deathUI;
    [Tooltip("Icon Image in Death UI")]
    public Image deathUICoinIcon;
    [Tooltip("Coin Count Text in Death UI")]
    public TMP_Text deathUICoinCount;
    [Tooltip("Lost Coins Text in Death UI")]
    public TMP_Text deathUILostCoins;
    [Tooltip("Time for death UI fade in and out")]
    public float deathUIFadeTime = 3f;
    [Tooltip("Amount of coins to subtract during death")]
    public int coinsToSubtract = 500;
    [Tooltip("Duration of coin subtraction animation")]
    public float coinSubtractionDuration = 1f;

    [Header("Bounce Effect Settings")]
    [Tooltip("Total duration of the bounce animation for the coin count text (never collected coins)")]
    public float bounceDuration = 0.5f;
    [Tooltip("X position of the left bounce endpoint (in UI units)")]
    public float leftBounceX = -5f;
    [Tooltip("X position of the right bounce endpoint (in UI units)")]
    public float rightBounceX = 5f;
    [Tooltip("Number of bounces (one bounce = left to right and back)")]
    public int bounceCount = 3;

    [Header("Loss Bounce Settings")]
    [Tooltip("Total duration of the vertical bounce animation when losing coins")]
    public float lossBounceDuration = 0.5f;
    [Tooltip("Y position of the top bounce endpoint (in UI units)")]
    public float topBounceY = 5f;
    [Tooltip("Y position of the bottom bounce endpoint (in UI units)")]
    public float bottomBounceY = -5f;
    [Tooltip("Number of vertical bounces when losing coins")]
    public int lossBounceCount = 3;

    [Header("Animation Settings")]
    [Tooltip("Reference to the Animator component")]
    public Animator animator;

    private Vector3 initialPosition;
    private Quaternion initialRotation; // Almacenar la rotación inicial del jugador
    private Vector3 initialCameraPosition;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Rigidbody2D rb;
    private bool hasEverCollectedCoins = false;
    private PlayerHealth playerHealth; // Referencia al script PlayerHealth
    private float originalGravityScale; // Almacenar el valor original de gravityScale

    void Start()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("PlayerMovement no encontrado en " + gameObject.name);
                return;
            }
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("No se encontró la cámara principal");
                return;
            }
        }

        if (cameraController == null)
        {
            cameraController = mainCamera.GetComponent<CameraController>();
            if (cameraController == null)
            {
                Debug.LogError("CameraController no encontrado en la cámara principal");
                return;
            }
        }

        if (fadePanel == null)
        {
            Debug.LogError("Fade Panel no asignado en " + gameObject.name);
            return;
        }

        if (kredsManager == null)
        {
            kredsManager = FindObjectOfType<KredsManager>();
            if (kredsManager == null)
            {
                Debug.LogError("KredsManager no encontrado en la escena");
                return;
            }
        }

        if (deathUI == null)
        {
            Debug.LogError("DeathUI no asignado en el Inspector");
            return;
        }

        if (deathUICoinIcon == null)
        {
            deathUICoinIcon = deathUI.GetComponentInChildren<Image>();
            if (deathUICoinIcon == null)
            {
                Debug.LogError("DeathUICoinIcon no encontrado en DeathUI");
                return;
            }
        }

        if (deathUICoinCount == null)
        {
            deathUICoinCount = deathUI.GetComponentInChildren<TMP_Text>();
            if (deathUICoinCount == null)
            {
                Debug.LogError("DeathUICoinCount no encontrado en DeathUI");
                return;
            }
        }

        if (deathUILostCoins == null)
        {
            TMP_Text[] texts = deathUI.GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 1)
            {
                deathUILostCoins = texts[1];
            }
            if (deathUILostCoins == null)
            {
                Debug.LogError("DeathUILostCoins no encontrado en DeathUI");
                return;
            }
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator no encontrado en " + gameObject.name);
        }

        // Obtener referencia al script PlayerHealth
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth no encontrado en " + gameObject.name);
        }

        // Almacenar el valor original de gravityScale
        if (rb != null)
        {
            originalGravityScale = rb.gravityScale; // Guardar el valor inicial (positivo)
            Debug.Log($"Valor original de gravityScale almacenado: {originalGravityScale}");
        }

        // Almacenar la posición y rotación inicial del jugador y la cámara
        initialPosition = transform.position;
        initialRotation = transform.rotation; // Guardar la rotación inicial
        initialCameraPosition = mainCamera.transform.position;
        fadePanel.color = new Color(0, 0, 0, 0);

        SetDeathUIAlpha(0f);
        deathUI.SetActive(false);

        if (kredsManager != null && kredsManager.totalTokens > 0)
        {
            hasEverCollectedCoins = true;
        }

        if (animator != null)
        {
            animator.Play("Protagonist_Idle");
        }

        Debug.Log($"Rotación inicial almacenada: {initialRotation.eulerAngles}");
    }

    void Update()
    {
        if (!isDead && playerMovement != null)
        {
            animator.SetBool("IsGrounded", playerMovement.IsGrounded());
            float horizontalSpeed = Mathf.Abs(rb.velocity.x);
            animator.SetFloat("Speed", horizontalSpeed);
            float adjustedVerticalSpeed = playerMovement.IsGravityNormal() ? rb.velocity.y : -rb.velocity.y;
            animator.SetFloat("VerticalSpeed", adjustedVerticalSpeed);
        }
    }

    public void OnCoinCollected()
    {
        hasEverCollectedCoins = true;
    }

    public void SetNewSpawnPosition(Vector3 newPosition)
    {
        // Actualizar la posición inicial para reaparecer
        initialPosition = newPosition;
        Debug.Log($"Posición inicial actualizada por el checkpoint: {initialPosition}");
    }

    public void SetNewSpawnPositionAndCamera(Vector3 newPlayerPosition, float checkpointX)
    {
        // Actualizar la posición inicial del jugador
        initialPosition = newPlayerPosition;
        Debug.Log($"Posición inicial del jugador actualizada por el checkpoint: {initialPosition}");

        // Actualizar la posición inicial de la cámara (solo en el eje X, sin limitar)
        Vector3 newCameraPosition = new Vector3(checkpointX, initialCameraPosition.y, initialCameraPosition.z);
        initialCameraPosition = newCameraPosition;
        Debug.Log($"Posición inicial de la cámara actualizada por el checkpoint: {initialCameraPosition}");
    }

    public IEnumerator RespawnCoroutine()
    {
        isDead = true;

        if (animator != null)
        {
            animator.Play("Protagonist_Die");
        }

        if (boxCollider != null) boxCollider.enabled = false;
        rb.velocity = Vector2.zero;
        rb.simulated = false;

        if (playerMovement.isDismembered)
        {
            playerMovement.headObject.SetActive(false);
            playerMovement.bodyObject.SetActive(false);
        }

        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        yield return new WaitForSeconds(fadeInDelay);

        // Fade-in: la pantalla se pone negra
        float elapsedTime = 0f;
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 1f);

        // Hacer los objetos invisibles cuando la pantalla se pone negra
        if (playerHealth != null)
        {
            playerHealth.ForceHideObjects();
        }

        yield return new WaitForSeconds(1f);

        if (deathUI != null && kredsManager != null)
        {
            int startCoinValue = kredsManager.displayedTokens;
            deathUICoinCount.text = startCoinValue.ToString("D9");
            deathUILostCoins.text = "";
            deathUI.SetActive(true);

            elapsedTime = 0f;
            while (elapsedTime < deathUIFadeTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsedTime / deathUIFadeTime);
                SetDeathUIAlpha(alpha);
                yield return null;
            }
            SetDeathUIAlpha(1f);

            yield return new WaitForSeconds(0.4f);

            // Hacer al jugador invisible justo antes de que comience el conteo de monedas
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
                Debug.Log("Jugador hecho invisible al inicio del conteo de monedas.");
            }

            bool neverCollectedCoins = !hasEverCollectedCoins && startCoinValue == 0;
            int coinsLost = neverCollectedCoins ? 0 : (startCoinValue < coinsToSubtract ? startCoinValue : coinsToSubtract);
            int targetCoinValue = Mathf.Max(0, startCoinValue - coinsLost);

            deathUILostCoins.text = "-" + coinsLost;

            elapsedTime = 0f;
            while (elapsedTime < coinSubtractionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / coinSubtractionDuration;
                int currentCoins = (int)Mathf.Lerp(startCoinValue, targetCoinValue, t);
                deathUICoinCount.text = currentCoins.ToString("D9");
                yield return null;
            }
            deathUICoinCount.text = targetCoinValue.ToString("D9");

            // Rebote vertical en deathUICoinCount si se pierden monedas
            if (coinsLost > 0)
            {
                yield return StartCoroutine(LossBounceEffect());
            }

            kredsManager.displayedTokens = targetCoinValue;
            kredsManager.totalTokens = targetCoinValue;
            kredsManager.UpdateHUD();

            if (neverCollectedCoins)
            {
                yield return StartCoroutine(BounceEffect());
            }

            yield return new WaitForSeconds(0.8f);

            elapsedTime = 0f;
            while (elapsedTime < deathUIFadeTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / deathUIFadeTime);
                SetDeathUIAlpha(alpha);
                yield return null;
            }
            SetDeathUIAlpha(0f);
            deathUI.SetActive(false);

            if (targetCoinValue == 0)
            {
                yield return new WaitForSeconds(2f);
                SceneManager.LoadScene("GameOver");
                yield break;
            }
        }

        if (kredsManager != null)
        {
            Color textColor = kredsManager.coinCountText.color;
            Color iconColor = kredsManager.coinIcon.GetComponent<Image>().color;
            textColor.a = 1f;
            iconColor.a = 1f;
            kredsManager.coinCountText.color = textColor;
            kredsManager.coinIcon.GetComponent<Image>().color = iconColor;
            kredsManager.uiContainer.anchoredPosition = kredsManager.originalUIPosition;
        }

        mainCamera.transform.position = initialCameraPosition;

        // Verificar si el checkpoint está dentro de los límites actuales de la cámara
        if (cameraController != null)
        {
            Vector2 checkpointPosition = new Vector2(initialPosition.x, initialPosition.y);
            if (!cameraController.IsPositionWithinLimits(checkpointPosition))
            {
                // Buscar el checkpoint en la posición de initialPosition
                Checkpoint checkpoint = FindCheckpointAtPosition(checkpointPosition);
                if (checkpoint != null)
                {
                    // Obtener los límites del checkpoint y actualizar la cámara
                    float newMinX, newMaxX;
                    checkpoint.GetCameraLimits(out newMinX, out newMaxX);
                    cameraController.UpdateCameraLimits(newMinX, newMaxX);
                }
                else
                {
                    // Si no se encuentra un checkpoint, establecer límites sin restricciones
                    cameraController.UpdateCameraLimits(float.MinValue, float.MaxValue);
                    Debug.Log("No se encontró un checkpoint. Estableciendo límites de cámara sin restricciones (infinitos).");
                }
            }
            else
            {
                Debug.Log("El checkpoint está dentro de los límites actuales de la cámara. No se ajustan los límites.");
            }
        }

        elapsedTime = 0f;
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 0f);

        yield return new WaitForSeconds(0.5f);

        // Restablecer la posición y rotación inicial del jugador
        transform.position = initialPosition;
        transform.rotation = initialRotation; // Aplicar la rotación inicial
        spriteRenderer.enabled = true;
        if (boxCollider != null) boxCollider.enabled = true;
        rb.simulated = false; // Mantener el Rigidbody2D desactivado durante la animación

        if (playerMovement.isDismembered)
        {
            playerMovement.RecomposePlayer();
        }

        // Restablecer la gravedad a normal (hacia abajo)
        if (rb != null && playerMovement != null)
        {
            // Restablecer gravityScale al valor original (positivo) y isGravityNormal
            rb.gravityScale = originalGravityScale; // Usar el valor original almacenado
            playerMovement.isGravityNormal = true; // Asegurar que isGravityNormal sea true

            Debug.Log($"Gravedad restablecida a normal al revivir. gravityScale: {rb.gravityScale}, isGravityNormal: {playerMovement.isGravityNormal}, Rotación del jugador: {transform.rotation.eulerAngles}");
        }

        if (animator != null)
        {
            animator.Play("Protagonist_Appear");
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;
            yield return new WaitForSeconds(animationLength); // Esperar a que termine la animación
            rb.simulated = true; // Activar el Rigidbody2D después de la animación
            animator.SetBool("IsGrounded", playerMovement.IsGrounded());
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("VerticalSpeed", rb.velocity.y);
        }

        if (cameraController != null)
        {
            cameraController.enabled = true;
        }

        // Restablecer la vida del jugador al máximo (sin hacer los objetos visibles)
        if (playerHealth != null)
        {
            playerHealth.currentHealth = playerHealth.maxHealth;
            HealthBar healthBar = FindObjectOfType<HealthBar>();
            if (healthBar != null)
            {
                healthBar.UpdateHealth(playerHealth.currentHealth);
            }
            // Actualizar el texto del contador después de restablecer la vida
            playerHealth.GetType().GetMethod("UpdateHealthCounterText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(playerHealth, null);
            // No llamamos a ShowObjects aquí para que los objetos permanezcan invisibles al reaparecer
        }

        isDead = false;
    }

    // Método para buscar un checkpoint en la posición dada
    private Checkpoint FindCheckpointAtPosition(Vector2 position)
    {
        Checkpoint[] checkpoints = FindObjectsOfType<Checkpoint>();
        foreach (Checkpoint checkpoint in checkpoints)
        {
            Vector2 checkpointPos = checkpoint.GetPosition();
            // Consideramos que la posición coincide si está muy cerca (por ejemplo, a 0.5 unidades)
            if (Vector2.Distance(checkpointPos, position) < 0.5f)
            {
                return checkpoint;
            }
        }
        return null;
    }

    private IEnumerator BounceEffect()
    {
        if (deathUICoinCount == null) yield break;

        RectTransform textTransform = deathUICoinCount.GetComponent<RectTransform>();
        Vector2 originalPosition = textTransform.anchoredPosition;

        float timePerBounce = bounceDuration / (bounceCount * 2);

        for (int i = 0; i < bounceCount * 2; i++)
        {
            float targetX = (i % 2 == 0) ? rightBounceX : leftBounceX;
            Vector2 targetPosition = new Vector2(targetX, originalPosition.y);
            textTransform.anchoredPosition = targetPosition;
            yield return new WaitForSeconds(timePerBounce);
        }

        textTransform.anchoredPosition = originalPosition;
    }

    private IEnumerator LossBounceEffect()
    {
        if (deathUICoinCount == null) yield break;

        RectTransform textTransform = deathUICoinCount.GetComponent<RectTransform>();
        Vector2 originalPosition = textTransform.anchoredPosition;

        float timePerBounce = lossBounceDuration / (lossBounceCount * 2);

        for (int i = 0; i < lossBounceCount * 2; i++)
        {
            float targetY = (i % 2 == 0) ? topBounceY : bottomBounceY;
            Vector2 targetPosition = new Vector2(originalPosition.x, targetY);
            textTransform.anchoredPosition = targetPosition;
            yield return new WaitForSeconds(timePerBounce);
        }

        textTransform.anchoredPosition = originalPosition;
    }

    private void SetDeathUIAlpha(float alpha)
    {
        if (deathUICoinIcon != null)
        {
            Color iconColor = deathUICoinIcon.color;
            iconColor.a = alpha;
            deathUICoinIcon.color = iconColor;
        }
        if (deathUICoinCount != null)
        {
            Color textColor = deathUICoinCount.color;
            textColor.a = alpha;
            deathUICoinCount.color = textColor;
        }
        if (deathUILostCoins != null)
        {
            Color lostColor = deathUILostCoins.color;
            lostColor.a = alpha;
            deathUILostCoins.color = lostColor;
        }
    }

    public bool IsDead()
    {
        return isDead;
    }
}