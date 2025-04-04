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
    private Quaternion initialRotation;
    private Vector3 initialCameraPosition;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Rigidbody2D rb;
    private bool hasEverCollectedCoins = false;
    private PlayerHealth playerHealth;
    private float originalGravityScale;
    private bool hasCollectedCheckpoint = false;
    private Checkpoint activeCheckpoint; // Referencia al checkpoint activo

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

        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth no encontrado en " + gameObject.name);
        }

        if (rb != null)
        {
            originalGravityScale = rb.gravityScale;
            Debug.Log($"Valor original de gravityScale almacenado: {originalGravityScale}");
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;
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
        initialPosition = newPosition;
        Debug.Log($"Posición inicial actualizada por el checkpoint: {initialPosition}");
    }

    public void SetNewSpawnPositionAndCamera(Vector3 newPlayerPosition, float checkpointX, Checkpoint checkpoint = null)
    {
        initialPosition = newPlayerPosition;
        Debug.Log($"Posición inicial del jugador actualizada por el checkpoint: {initialPosition}");

        Vector3 newCameraPosition = new Vector3(checkpointX, initialCameraPosition.y, initialCameraPosition.z);
        initialCameraPosition = newCameraPosition;
        Debug.Log($"Posición inicial de la cámara actualizada por el checkpoint: {initialCameraPosition}");

        if (checkpoint != null)
        {
            activeCheckpoint = checkpoint; // Almacenar el checkpoint activo
            hasCollectedCheckpoint = true;
            Debug.Log($"Checkpoint activado: {checkpoint.gameObject.name}");
        }
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

        float elapsedTime = 0f;
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 1f);

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

        // Activar la zona visual del checkpoint activo
        if (activeCheckpoint != null)
        {
            GameObject visualZoneToActivate = activeCheckpoint.GetVisualZone();
            if (visualZoneToActivate != null)
            {
                visualZoneToActivate.SetActive(true);
                Debug.Log($"Zona visual activada al reaparecer: {visualZoneToActivate.name}");
            }
            else
            {
                Debug.LogWarning("No se encontró una zona visual asociada al checkpoint activo. No se activará ninguna zona visual.");
            }
        }
        else
        {
            Debug.LogWarning("No hay un checkpoint activo. No se activará ninguna zona visual al reaparecer.");
        }

        if (cameraController != null)
        {
            Vector2 respawnPosition = new Vector2(initialPosition.x, initialPosition.y);
            Zone currentZone = FindZoneAtPosition(respawnPosition);
            if (currentZone != null)
            {
                float newMinX, newMaxX;
                currentZone.GetCameraLimits(out newMinX, out newMaxX);
                cameraController.UpdateCameraLimits(newMinX, newMaxX);
                Debug.Log($"Límites de la cámara actualizados al reaparecer según la zona ({currentZone.gameObject.name}): minX = {newMinX}, maxX = {newMaxX}");
            }
            else
            {
                cameraController.RestoreInitialLimits();
                Debug.Log("No se encontró una zona en la posición de reaparición. Restaurando límites iniciales de la cámara.");
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

        transform.position = initialPosition;
        transform.rotation = initialRotation;
        spriteRenderer.enabled = true;
        if (boxCollider != null) boxCollider.enabled = true;
        rb.simulated = false;

        if (playerMovement.isDismembered)
        {
            playerMovement.RecomposePlayer();
        }

        if (rb != null && playerMovement != null)
        {
            rb.gravityScale = originalGravityScale;
            playerMovement.isGravityNormal = true;
            Debug.Log($"Gravedad restablecida a normal al revivir. gravityScale: {rb.gravityScale}, isGravityNormal: {playerMovement.isGravityNormal}, Rotación del jugador: {transform.rotation.eulerAngles}");
        }

        if (animator != null)
        {
            animator.Play("Protagonist_Appear");
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;
            yield return new WaitForSeconds(animationLength);
            rb.simulated = true;
            animator.SetBool("IsGrounded", playerMovement.IsGrounded());
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("VerticalSpeed", rb.velocity.y);
        }

        if (cameraController != null)
        {
            cameraController.enabled = true;
        }

        if (playerHealth != null)
        {
            playerHealth.currentHealth = playerHealth.maxHealth;
            HealthBar healthBar = FindObjectOfType<HealthBar>();
            if (healthBar != null)
            {
                healthBar.UpdateHealth(playerHealth.currentHealth);
            }
            playerHealth.GetType().GetMethod("UpdateHealthCounterText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(playerHealth, null);
        }

        isDead = false;
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

    public bool HasCollectedCheckpoint()
    {
        return hasCollectedCheckpoint;
    }
}