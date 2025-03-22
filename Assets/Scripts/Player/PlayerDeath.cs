using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public GameObject deathUI; // Nueva UI específica para la muerte
    [Tooltip("Icon Image in Death UI")]
    public Image deathUICoinIcon; // Ícono en la Death UI
    [Tooltip("Coin Count Text in Death UI")]
    public TMP_Text deathUICoinCount; // Contador de monedas en la Death UI
    [Tooltip("Time for death UI fade in and out")]
    public float deathUIFadeTime = 3f;
    [Tooltip("Amount of coins to subtract during death")]
    public int coinsToSubtract = 500; // Cantidad de monedas a restar
    [Tooltip("Duration of coin subtraction animation")]
    public float coinSubtractionDuration = 1f; // Duración de la animación de resta

    private Vector3 initialPosition;         // Posición inicial del jugador
    private Vector3 initialCameraPosition;   // Posición inicial de la cámara almacenada
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Rigidbody2D rb;

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

        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        // Almacenar posiciones iniciales
        initialPosition = transform.position;
        initialCameraPosition = mainCamera.transform.position;
        fadePanel.color = new Color(0, 0, 0, 0); // Asegurarse que el panel empiece transparente

        // Asegurarse que la deathUI esté inicialmente oculta
        SetDeathUIAlpha(0f);
        deathUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("DeathZone") && !isDead)
        {
            StartCoroutine(RespawnCoroutine());
        }
    }

    private System.Collections.IEnumerator RespawnCoroutine()
    {
        isDead = true;

        // Desactivar componentes del jugador
        spriteRenderer.enabled = false;
        if (boxCollider != null) boxCollider.enabled = false;
        rb.velocity = Vector2.zero;
        rb.simulated = false;

        if (playerMovement.isDismembered)
        {
            playerMovement.headObject.SetActive(false);
            playerMovement.bodyObject.SetActive(false);
        }

        // Desactivar el seguimiento de la cámara
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        // Esperar antes de iniciar el fade in
        yield return new WaitForSeconds(fadeInDelay);

        // Fade In (pantalla se vuelve negra)
        float elapsedTime = 0f;
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 1f); // Asegurar que esté completamente negro

        // Esperar 1 segundo antes del fade in de la Death UI
        yield return new WaitForSeconds(1f);

        // Fade in de la Death UI
        if (deathUI != null && kredsManager != null)
        {
            // Actualizar el contador de monedas en DeathUI con el valor de KredsManager
            int startCoinValue = kredsManager.displayedTokens;
            deathUICoinCount.text = startCoinValue.ToString("D9");
            deathUI.SetActive(true); // Activar la UI

            elapsedTime = 0f;
            while (elapsedTime < deathUIFadeTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsedTime / deathUIFadeTime);
                SetDeathUIAlpha(alpha);
                yield return null;
            }
            SetDeathUIAlpha(1f); // Asegurar que esté completamente visible

            // Esperar 0.4 segundos después del fade in
            yield return new WaitForSeconds(0.4f);

            // Animación de resta de monedas
            elapsedTime = 0f;
            int targetCoinValue = Mathf.Max(0, startCoinValue - coinsToSubtract); // No bajar de 0
            while (elapsedTime < coinSubtractionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / coinSubtractionDuration;
                int currentCoins = (int)Mathf.Lerp(startCoinValue, targetCoinValue, t);
                deathUICoinCount.text = currentCoins.ToString("D9");
                yield return null;
            }
            deathUICoinCount.text = targetCoinValue.ToString("D9");
            kredsManager.displayedTokens = targetCoinValue; // Actualizar el valor en KredsManager
            kredsManager.totalTokens = targetCoinValue; // Sincronizar también totalTokens
            kredsManager.UpdateHUD(); // Actualizar la UI de KredsManager

            // Esperar 0.8 segundos después de la resta
            yield return new WaitForSeconds(0.8f);

            // Fade out de la Death UI
            elapsedTime = 0f;
            while (elapsedTime < deathUIFadeTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / deathUIFadeTime);
                SetDeathUIAlpha(alpha);
                yield return null;
            }
            SetDeathUIAlpha(0f); // Asegurar que esté completamente transparente
            deathUI.SetActive(false); // Desactivar la UI

            // Esperar 1 segundo después del fade out de la Death UI
            yield return new WaitForSeconds(1f);
        }

        // Restaurar la UI de monedas a su posición original con alpha completo
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

        // Mover la cámara a la posición inicial almacenada antes del fade out
        mainCamera.transform.position = initialCameraPosition;

        // Fade Out (pantalla se desvanece)
        elapsedTime = 0f;
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 0f); // Asegurar que esté completamente transparente

        // Esperar medio segundo después del fade out
        yield return new WaitForSeconds(0.5f);

        // Restaurar al jugador
        transform.position = initialPosition;
        spriteRenderer.enabled = true;
        if (boxCollider != null) boxCollider.enabled = true;
        rb.simulated = true;

        if (playerMovement.isDismembered)
        {
            playerMovement.RecomposePlayer();
        }

        // Reactivar el seguimiento de la cámara
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }

        isDead = false;
    }

    private void SetDeathUIAlpha(float alpha)
    {
        // Ajustar el alpha del ícono y el texto en deathUI
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
    }

    public bool IsDead()
    {
        return isDead;
    }
}