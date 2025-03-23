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
    public GameObject deathUI; // Nueva UI específica para la muerte
    [Tooltip("Icon Image in Death UI")]
    public Image deathUICoinIcon; // Ícono en la Death UI
    [Tooltip("Coin Count Text in Death UI")]
    public TMP_Text deathUICoinCount; // Contador de monedas en la Death UI
    [Tooltip("Lost Coins Text in Death UI")]
    public TMP_Text deathUILostCoins; // Texto para mostrar la cantidad perdida
    [Tooltip("Time for death UI fade in and out")]
    public float deathUIFadeTime = 3f;
    [Tooltip("Amount of coins to subtract during death")]
    public int coinsToSubtract = 500; // Cantidad de monedas a restar
    [Tooltip("Duration of coin subtraction animation")]
    public float coinSubtractionDuration = 1f; // Duración de la animación de resta

    [Header("Bounce Effect Settings")]
    [Tooltip("Total duration of the bounce animation for the coin count text")]
    public float bounceDuration = 0.5f;
    [Tooltip("X position of the left bounce endpoint (in UI units)")]
    public float leftBounceX = -5f; // Posición X del extremo izquierdo
    [Tooltip("X position of the right bounce endpoint (in UI units)")]
    public float rightBounceX = 5f; // Posición X del extremo derecho
    [Tooltip("Number of bounces (one bounce = left to right and back)")]
    public int bounceCount = 3; // Número de rebotes completos

    private Vector3 initialPosition;         // Posición inicial del jugador
    private Vector3 initialCameraPosition;   // Posición inicial de la cámara almacenada
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Rigidbody2D rb;
    private bool hasEverCollectedCoins = false; // Para rastrear si alguna vez ha recolectado monedas

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
                deathUILostCoins = texts[1]; // Asumir que el segundo TMP_Text es el de monedas perdidas
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
        
        // Almacenar posiciones iniciales
        initialPosition = transform.position;
        initialCameraPosition = mainCamera.transform.position;
        fadePanel.color = new Color(0, 0, 0, 0); // Asegurarse que el panel empiece transparente
        
        // Asegurarse que la deathUI esté inicialmente oculta
        SetDeathUIAlpha(0f);
        deathUI.SetActive(false);

        // Verificar si el jugador alguna vez ha recolectado monedas
        if (kredsManager != null && kredsManager.totalTokens > 0)
        {
            hasEverCollectedCoins = true;
        }
    }

    public void OnCoinCollected()
    {
        hasEverCollectedCoins = true;
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
            deathUILostCoins.text = ""; // Inicialmente vacío
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

            // Determinar si el jugador alguna vez ha recolectado monedas
            bool neverCollectedCoins = !hasEverCollectedCoins && startCoinValue == 0;

            // Calcular las monedas perdidas
            int coinsLost = neverCollectedCoins ? 0 : coinsToSubtract; // Si nunca ha recolectado, perderá 0
            int targetCoinValue = Mathf.Max(0, startCoinValue - coinsLost); // No bajar de 0

            // Animación de resta de monedas con texto de pérdida
            elapsedTime = 0f;
            deathUILostCoins.text = "-" + coinsLost; // Mostrar la cantidad perdida
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

            // Si nunca ha recolectado monedas, aplicar el efecto de rebote al contador de monedas
            if (neverCollectedCoins)
            {
                StartCoroutine(BounceEffect());
            }

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

            // Comprobar si las monedas llegaron a 0
            if (targetCoinValue == 0)
            {
                // Esperar 2 segundos y cambiar a la escena GameOver
                yield return new WaitForSeconds(2f);
                SceneManager.LoadScene("GameOver");
                yield break; // Terminar la corrutina aquí
            }
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

        // Fade Out (pantalla se desvanece) - Solo si no es Game Over
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

    private IEnumerator BounceEffect()
    {
        if (deathUICoinCount == null) yield break;

        // Usar RectTransform para mover el texto en UI space
        RectTransform textTransform = deathUICoinCount.GetComponent<RectTransform>();
        Vector2 originalPosition = textTransform.anchoredPosition;

        float elapsedTime = 0f;
        float timePerBounce = bounceDuration / (bounceCount * 2); // Cada rebote tiene 2 movimientos (izquierda y derecha)

        for (int i = 0; i < bounceCount * 2; i++)
        {
            // Determinar la posición objetivo (izquierda o derecha) usando las posiciones absolutas
            float targetX = (i % 2 == 0) ? rightBounceX : leftBounceX;
            Vector2 targetPosition = new Vector2(targetX, originalPosition.y);

            // Mover de golpe (sin suavizado)
            textTransform.anchoredPosition = targetPosition;

            // Esperar el tiempo de cada movimiento
            elapsedTime += timePerBounce;
            yield return new WaitForSeconds(timePerBounce);
        }

        // Restaurar la posición original de golpe
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