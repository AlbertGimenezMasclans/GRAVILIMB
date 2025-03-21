using UnityEngine;
using TMPro;
using System.Collections;

public class KredsManager : MonoBehaviour
{
    public static KredsManager Instance { get; private set; } // Singleton
    [SerializeField] private TMP_Text coinCountText;         // Referencia al texto del HUD
    [SerializeField] private RectTransform coinIcon;         // Referencia al ícono en la UI
    private int totalTokens = 1000;                     // Valor inicial (1000)
    private int displayedTokens = 000001000;                 // Valor mostrado en pantalla
    private Vector2 originalIconPosition;                    // Posición original del ícono

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
        else
        {
            originalIconPosition = coinIcon.anchoredPosition; // Guardar la posición inicial del ícono
        }
        UpdateHUD(); // Mostrar el valor inicial
    }

    public void AddTokens(int amount)
    {
        totalTokens += amount;
        StartCoroutine(AnimateTokenIncrease(amount));
    }

    private void UpdateHUD()
    {
        if (coinCountText != null)
        {
            coinCountText.text = displayedTokens.ToString("D9"); // Solo el número con 9 dígitos
        }
    }

    private IEnumerator AnimateTokenIncrease(int amount)
    {
        int startValue = displayedTokens;
        int targetValue = totalTokens;
        float duration = 0.5f; // Duración total de medio segundo
        float elapsedTime = 0f;
        int bounceCount = 3; // Número de rebotes = 6 rebotes por segundo
        float bounceDuration = duration / bounceCount; // Duración de cada rebote (1/6 de segundo)

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration; // Progreso total (0 a 1)

            // Interpolar el valor de los tokens
            displayedTokens = (int)Mathf.Lerp(startValue, targetValue, t);

            // Calcular el rebote actual
            int currentBounce = Mathf.FloorToInt(elapsedTime / bounceDuration);
            float bounceProgress = (elapsedTime % bounceDuration) / bounceDuration; // Progreso dentro del rebote actual

            if (coinIcon != null)
            {
                // Movimiento de rebote: primero abajo, luego arriba
                float bounceHeight = 6f; // Altura máxima del rebote (ajustable)
                Vector2 startPosition = originalIconPosition - Vector2.up * bounceHeight * 0.4f; // Punto más bajo
                Vector2 peakPosition = originalIconPosition + Vector2.up * bounceHeight * 0.5f;  // Punto más alto

                if (bounceProgress < 0.4f) // Primera mitad: bajar
                {
                    float downProgress = bounceProgress / 0.4f;
                    coinIcon.anchoredPosition = Vector2.Lerp(originalIconPosition, startPosition, downProgress);
                }
                else // Segunda mitad: subir
                {
                    float upProgress = (bounceProgress - 0.5f) / 0.5f;
                    coinIcon.anchoredPosition = Vector2.Lerp(startPosition, peakPosition, upProgress);
                }
            }

            UpdateHUD();
            yield return null; // Esperar al siguiente frame
        }

        // Asegurarse de que los valores finales sean exactos
        displayedTokens = targetValue;
        if (coinIcon != null)
        {
            coinIcon.anchoredPosition = originalIconPosition; // Volver a la posición original
        }
        UpdateHUD();
    }
}