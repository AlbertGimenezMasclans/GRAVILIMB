using UnityEngine;
using TMPro;
using System.Collections;

public class KredsManager : MonoBehaviour
{
    public static KredsManager Instance { get; private set; } // Singleton
    [SerializeField] public TMP_Text coinCountText;         // Referencia al texto del HUD
    [SerializeField] public RectTransform coinIcon;         // Referencia al ícono en la UI
    [SerializeField] public RectTransform uiContainer;      // Contenedor de la UI (texto + ícono)
    public int totalTokens = 0;                             // Valor inicial (0)
    public int displayedTokens = 000000000;                 // Valor mostrado en pantalla
    public Vector2 originalUIPosition;                      // Posición inicial visible de la UI
    public Vector2 hiddenUIPosition;                        // Posición fuera de la cámara
    private Vector2 originalIconPosition;                   // Posición original del ícono relativa al contenedor
    private int consecutiveCoins = 0;                       // Contador de monedas consecutivas
    private float timeSinceLastCoin = 0f;                   // Tiempo desde la última moneda
    private float resetDelay = 1f;                          // Margen de 1s para monedas consecutivas
    public bool isAnimating = false;                        // Evitar múltiples animaciones simultáneas
    private Coroutine currentAnimation;                     // Referencia a la corrutina actual
    private PlayerDeath playerDeath;                        // Referencia a PlayerDeath

    [Header("Bounce Effect Settings")]
    [Tooltip("Total duration of the bounce animation when losing coins")]
    public float lossBounceDuration = 0.5f;
    [Tooltip("Y position of the top bounce endpoint (in UI units)")]
    public float topBounceY = 5f;
    [Tooltip("Y position of the bottom bounce endpoint (in UI units)")]
    public float bottomBounceY = -5f;
    [Tooltip("Number of bounces (one bounce = bottom to top and back)")]
    public int lossBounceCount = 3;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (coinCountText == null)
        {
            Debug.LogError("CoinCountText no está asignado en el Inspector del KredsManager.");
        }
        if (coinIcon == null)
        {
            Debug.LogError("CoinIcon no está asignado en el Inspector del KredsManager.");
        }
        if (uiContainer == null)
        {
            Debug.LogError("UIContainer no está asignado en el Inspector del KredsManager.");
        }
        else
        {
            originalUIPosition = uiContainer.anchoredPosition;
            hiddenUIPosition = originalUIPosition + Vector2.up * 240f;
            uiContainer.anchoredPosition = hiddenUIPosition;
            originalIconPosition = coinIcon.anchoredPosition;
        }

        // Obtener referencia a PlayerDeath
        playerDeath = FindObjectOfType<PlayerDeath>();
        if (playerDeath == null)
        {
            Debug.LogError("PlayerDeath no encontrado en la escena.");
        }

        UpdateHUD();
    }

    void Update()
    {
        // Controlar la UI con la tecla X (sin animación)
        if (Input.GetKey(KeyCode.X))
        {
            uiContainer.anchoredPosition = originalUIPosition; // Mostrar instantáneamente
        }
        else if (!isAnimating && uiContainer.anchoredPosition != hiddenUIPosition)
        {
            uiContainer.anchoredPosition = hiddenUIPosition; // Ocultar instantáneamente si no hay animación
        }

        if (consecutiveCoins > 0)
        {
            timeSinceLastCoin += Time.deltaTime;
            if (timeSinceLastCoin > resetDelay && !isAnimating)
            {
                consecutiveCoins = 0;
                timeSinceLastCoin = 0f;
            }
        }
    }

    // Método original para KredTokens normales
    public void AddTokens(int amount)
    {
        AddTokens(amount, -1f); // -1f indica usar la duración por defecto basada en consecutiveCoins
    }

    // Método sobrecargado para permitir duración personalizada
    public void AddTokens(int amount, float duration = -1f)
    {
        totalTokens += amount; // Sumar el valor exacto recibido
        consecutiveCoins++;    // Incrementar por cada item recogido
        timeSinceLastCoin = 0f;

        // Notificar a PlayerDeath que se ha recolectado una moneda
        if (playerDeath != null)
        {
            playerDeath.OnCoinCollected();
        }

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        currentAnimation = StartCoroutine(AnimateUIAndTokens(amount, duration));
    }

    // Nuevo método para restar monedas con rebote vertical
    public IEnumerator AnimateLoss(int amountLost)
    {
        if (coinCountText == null || uiContainer == null) yield break;

        isAnimating = true;

        // Mostrar la UI en la posición original
        uiContainer.anchoredPosition = originalUIPosition;

        // Actualizar el texto inmediatamente con el valor perdido
        totalTokens = Mathf.Max(0, totalTokens - amountLost);
        displayedTokens = totalTokens;
        UpdateHUD();

        // Rebote vertical sin fluidez
        RectTransform textTransform = coinCountText.GetComponent<RectTransform>();
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

        yield return new WaitForSeconds(0.8f);

        // Ocultar la UI si no se está pulsando X
        if (!Input.GetKey(KeyCode.X))
        {
            float moveUpDuration = 0.2f;
            float elapsedTime = 0f;
            Vector2 startPosition = uiContainer.anchoredPosition;
            while (elapsedTime < moveUpDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveUpDuration;
                uiContainer.anchoredPosition = Vector2.Lerp(startPosition, hiddenUIPosition, t);
                yield return null;
            }
            uiContainer.anchoredPosition = hiddenUIPosition;
        }

        isAnimating = false;
    }

    public void UpdateHUD()
    {
        if (coinCountText != null)
        {
            coinCountText.text = displayedTokens.ToString("D9");
        }
    }

    private IEnumerator AnimateUIAndTokens(int amount, float customDuration)
    {
        isAnimating = true;

        float elapsedTime;
        Vector2 startPosition;

        // Animación de entrada normal (solo si no está pulsando X)
        if (uiContainer.anchoredPosition != originalUIPosition && !Input.GetKey(KeyCode.X))
        {
            if (uiContainer.anchoredPosition.y > originalUIPosition.y)
            {
                float quickMoveDuration = 0.1f;
                elapsedTime = 0f;
                startPosition = uiContainer.anchoredPosition;
                while (elapsedTime < quickMoveDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / quickMoveDuration;
                    uiContainer.anchoredPosition = Vector2.Lerp(startPosition, originalUIPosition, t);
                    yield return null;
                }
                uiContainer.anchoredPosition = originalUIPosition;
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.1f);
                float moveDownDuration = 0.2f;
                elapsedTime = 0f;
                startPosition = uiContainer.anchoredPosition;
                while (elapsedTime < moveDownDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / moveDownDuration;
                    uiContainer.anchoredPosition = Vector2.Lerp(startPosition, originalUIPosition, t);
                    yield return null;
                }
                uiContainer.anchoredPosition = originalUIPosition;
            }
        }

        // Conteo de monedas
        int startValue = displayedTokens;
        int targetValue = totalTokens;
        float baseDurationPerCoin = 0.25f;
        float totalDuration = (customDuration >= 0f) ? customDuration : baseDurationPerCoin * consecutiveCoins;
        elapsedTime = 0f;
        int baseBounceCount = 3;
        int totalBounceCount = baseBounceCount * consecutiveCoins;
        float bounceDuration = totalDuration / totalBounceCount;

        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / totalDuration;
            displayedTokens = (int)Mathf.Lerp(startValue, targetValue, t);

            int currentBounce = Mathf.FloorToInt(elapsedTime / bounceDuration);
            float bounceProgress = (elapsedTime % bounceDuration) / bounceDuration;

            if (coinIcon != null)
            {
                float bounceHeight = 6f;
                Vector2 startBouncePosition = originalIconPosition - Vector2.up * bounceHeight * 0.4f;
                Vector2 peakPosition = originalIconPosition + Vector2.up * bounceHeight * 0.6f;

                if (bounceProgress < 0.4f)
                {
                    float downProgress = bounceProgress / 0.4f;
                    coinIcon.anchoredPosition = Vector2.Lerp(originalIconPosition, startBouncePosition, downProgress);
                }
                else
                {
                    float upProgress = (bounceProgress - 0.4f) / 0.6f;
                    coinIcon.anchoredPosition = Vector2.Lerp(startBouncePosition, peakPosition, upProgress);
                }
            }

            UpdateHUD();
            yield return null;
        }

        displayedTokens = targetValue;
        if (coinIcon != null)
        {
            coinIcon.anchoredPosition = originalIconPosition;
        }
        UpdateHUD();

        yield return new WaitForSecondsRealtime(0.6f);

        // Animación de salida normal (solo si no está pulsando X)
        if (!Input.GetKey(KeyCode.X))
        {
            float moveUpDuration = 0.2f;
            elapsedTime = 0f;
            startPosition = uiContainer.anchoredPosition;
            while (elapsedTime < moveUpDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveUpDuration;
                uiContainer.anchoredPosition = Vector2.Lerp(startPosition, hiddenUIPosition, t);
                yield return null;
            }
            uiContainer.anchoredPosition = hiddenUIPosition;
        }

        isAnimating = false;
        currentAnimation = null;
    }
}