using UnityEngine;
using TMPro;
using System.Collections;

public class KredsManager : MonoBehaviour
{
    public static KredsManager Instance { get; private set; } // Singleton
    [SerializeField] private TMP_Text coinCountText;         // Referencia al texto del HUD
    [SerializeField] private RectTransform coinIcon;         // Referencia al ícono en la UI
    [SerializeField] private RectTransform uiContainer;      // Contenedor de la UI (texto + ícono)
    private int totalTokens = 0;                          // Valor inicial (1000)
    private int displayedTokens = 000000000;                 // Valor mostrado en pantalla
    private Vector2 originalUIPosition;                      // Posición inicial visible de la UI
    private Vector2 hiddenUIPosition;                        // Posición fuera de la cámara
    private Vector2 originalIconPosition;                    // Posición original del ícono relativa al contenedor
    private int consecutiveCoins = 0;                        // Contador de monedas consecutivas
    private float timeSinceLastCoin = 0f;                    // Tiempo desde la última moneda
    private float resetDelay = 1f;                           // Margen de 1s para monedas consecutivas
    private bool isAnimating = false;                        // Evitar múltiples animaciones simultáneas
    private Coroutine currentAnimation;                      // Referencia a la corrutina actual

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
        UpdateHUD();
    }

    void Update()
    {
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

    public void AddTokens(int amount)
    {
        totalTokens += amount; // Sumar el valor exacto recibido
        consecutiveCoins++;    // Incrementar por cada moneda recogida
        timeSinceLastCoin = 0f;

        // Reiniciar la animación siempre para reflejar los nuevos valores
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        currentAnimation = StartCoroutine(AnimateUIAndTokens(amount));
    }

    private void UpdateHUD()
    {
        if (coinCountText != null)
        {
            coinCountText.text = displayedTokens.ToString("D9");
        }
    }

    private IEnumerator AnimateUIAndTokens(int amount)
    {
        isAnimating = true;

        float elapsedTime; // Declarar fuera de los bucles
        Vector2 startPosition; // Declarar fuera de los bucles

        // Si la UI no está en posición visible, moverla rápidamente o con retraso según el caso
        if (uiContainer.anchoredPosition != originalUIPosition)
        {
            // Si estaba subiendo o fuera de pantalla, mover rápidamente
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
            // Si estaba arriba del todo, bajar con retraso normal
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

        // Animar el aumento de tokens y rebotes (más rápido)
        int startValue = displayedTokens;
        int targetValue = totalTokens;
        float baseDurationPerCoin = 0.25f; // Reducido de 0.5f a 0.25f para mayor velocidad
        float totalDuration = baseDurationPerCoin * consecutiveCoins;
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

        // Esperar antes de subir
        yield return new WaitForSecondsRealtime(0.6f);

        // Subir la UI
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

        isAnimating = false;
        currentAnimation = null;
    }
}