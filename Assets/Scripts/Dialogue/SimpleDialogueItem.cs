using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleDialogueItem : MonoBehaviour
{
    [SerializeField] private GameObject textBox;              // El GameObject del cuadro de diálogo
    [SerializeField, TextArea(1, 4)] private string[] dialogueLines; // Las líneas del diálogo
    [SerializeField] private TMP_Text textField;              // El campo de texto para mostrar el diálogo
    [SerializeField] private Image Input_TB;                  // Indicador de "presiona C" (opcional)
    [SerializeField] private AudioClip fanfareSong;           // Sonido de fanfare especificado en el Inspector
    [SerializeField] private AudioClip dialogueAdvanceSound;  // Sonido al avanzar
    [SerializeField] private AudioClip dialogueEndSound;      // Sonido al terminar
    [SerializeField] private AudioClip typingSound;           // Sonido de escritura
    [SerializeField, Tooltip("GameObject to make invisible after pickup and dialogue end")] 
    private GameObject objectToMakeInvisible;                 // Objeto a hacer invisible desde el Inspector
    private SpriteRenderer objectRenderer;                    // SpriteRenderer del objeto a hacer invisible

    private float typingTime = 0.05f;                         // Velocidad de escritura
    private float commaPauseTime = 0.25f;                     // Pausa en comas
    private float periodPauseTime = 0.48f;                    // Pausa en puntos
    private bool didDialogueStart;                            // ¿Está activo el diálogo?
    private int lineIndex;                                    // Línea actual
    private AudioSource audioSource;                          // Para reproducir sonidos
    private PlayerMovement playerMovement;                    // Referencia al movimiento del jugador
    private Animator playerAnimator;                          // Referencia al animator del jugador
    private bool hasCollided;                                 // Evitar colisiones múltiples
    private float originalPitch;                              // Pitch original del audio
    private KredsManager kredsManager;                        // Referencia al gestor de monedas
    private bool isWaitingForGround;                          // Esperar a que el jugador caiga

    private const int KREDS_TO_ADD = 5000;                    // Cantidad de Kreds a sumar (fija en código)
    private const float Y_OFFSET = 2f;                        // Desplazamiento en Y relativo al jugador

    public bool IsDialogueActive => didDialogueStart;

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        originalPitch = audioSource.pitch;

        // Obtener el SpriteRenderer del objeto a hacer invisible
        if (objectToMakeInvisible != null)
        {
            objectRenderer = objectToMakeInvisible.GetComponent<SpriteRenderer>();
            if (objectRenderer == null)
            {
                Debug.LogWarning("El objeto asignado para hacer invisible no tiene un SpriteRenderer.", this);
            }
        }
        else
        {
            Debug.LogWarning("No se ha asignado un objeto para hacer invisible en SimpleDialogueItem.", this);
        }

        // Cargar sonidos por defecto si no están asignados
        if (dialogueAdvanceSound == null) dialogueAdvanceSound = Resources.Load<AudioClip>("SFX/DialogueNEXT");
        if (dialogueEndSound == null) dialogueEndSound = Resources.Load<AudioClip>("SFX/DialogueEND");
        if (typingSound == null) typingSound = Resources.Load<AudioClip>("SFX/Dialogue2");

        // Obtener el KredsManager
        kredsManager = KredsManager.Instance;
        if (kredsManager == null) Debug.LogError("KredsManager no encontrado en la escena.");

        // Verificar referencias
        if (textBox == null) Debug.LogError("TextBox no está asignado.");
        if (textField == null) Debug.LogError("TextField no está asignado.");
        else if (!didDialogueStart) textField.gameObject.SetActive(false);

        if (Input_TB != null) Input_TB.gameObject.SetActive(false);
    }

    void Update()
    {
        // Comprobar si el jugador ha tocado el suelo tras la colisión
        if (isWaitingForGround && playerMovement != null && playerMovement.IsGrounded())
        {
            isWaitingForGround = false;
            StartCoroutine(PlayFanfareAndAnimation());
        }

        if (didDialogueStart && Input.GetKeyDown(KeyCode.C))
        {
            if (textField != null && textField.maxVisibleCharacters >= GetVisibleCharacterCount(dialogueLines[lineIndex]))
            {
                NextDialogueLine();
            }
            else if (textField != null)
            {
                StopAllCoroutines();
                textField.maxVisibleCharacters = GetVisibleCharacterCount(dialogueLines[lineIndex]);
                if (Input_TB != null) Input_TB.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !hasCollided)
        {
            hasCollided = true;

            // Hacer invisible el objeto al colisionar ("tras recogerlo")
            if (objectRenderer != null)
            {
                objectRenderer.enabled = false;
            }

            playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
            playerAnimator = collision.gameObject.GetComponent<Animator>();

            if (playerMovement != null)
            {
                // Bloquear el movimiento pero permitir que caiga
                playerMovement.SetMovementLocked(true);
                playerMovement.rb.velocity = new Vector2(0f, playerMovement.rb.velocity.y);

                // Esperar a que toque el suelo
                isWaitingForGround = true;
            }
        }
    }

    private IEnumerator PlayFanfareAndAnimation()
    {
        // Reposicionar el objeto relativo al jugador y hacerlo visible para la animación
        if (playerMovement != null)
        {
            Vector2 newPosition = new Vector2(playerMovement.transform.position.x, playerMovement.transform.position.y + Y_OFFSET);
            transform.position = newPosition;
            if (objectRenderer != null)
            {
                objectRenderer.enabled = true; // Hacer visible durante la fanfare
            }
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("PlayKeyItem", true);
            playerAnimator.SetFloat("Speed", 0f);
            playerAnimator.SetBool("IsGrounded", true);
            playerAnimator.SetFloat("VerticalSpeed", 0f);
        }

        if (fanfareSong != null && audioSource != null)
        {
            audioSource.PlayOneShot(fanfareSong);
        }

        // Esperar a que termine el fanfare antes de iniciar el diálogo
        if (fanfareSong != null)
        {
            yield return new WaitForSecondsRealtime(fanfareSong.length);
        }

        // Iniciar el diálogo
        StartDialogue();
    }

    private void StartDialogue()
    {
        if (textBox == null || textField == null || dialogueLines == null || dialogueLines.Length == 0) return;

        didDialogueStart = true;
        textBox.SetActive(true);
        textField.gameObject.SetActive(true);
        lineIndex = 0;
        Time.timeScale = 0f; // Pausar el tiempo para el diálogo

        if (Input_TB != null) Input_TB.gameObject.SetActive(false);
        StartCoroutine(ShowLine());
    }

    private void NextDialogueLine()
    {
        lineIndex++;
        if (lineIndex < dialogueLines.Length)
        {
            PlayDialogueSound(dialogueAdvanceSound);
            if (Input_TB != null) Input_TB.gameObject.SetActive(false);
            StartCoroutine(ShowLine());
        }
        else
        {
            PlayDialogueSound(dialogueEndSound);
            didDialogueStart = false;
            textBox.SetActive(false);
            textField.gameObject.SetActive(false);
            Time.timeScale = 1f;

            // Hacer invisible el objeto al terminar el diálogo
            if (objectRenderer != null)
            {
                objectRenderer.enabled = false;
            }

            // Restablecer la animación del jugador a la normalidad
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("PlayKeyItem", false);
            }

            // NO desbloquear el movimiento aquí, se hará tras el conteo
            if (kredsManager != null)
            {
                StartCoroutine(ShowCoinUIAndAddKreds());
            }
        }
    }

    private IEnumerator ShowCoinUIAndAddKreds()
    {
        // Asegurarse de que la UI esté activa y notificar a KredsManager que estamos animando
        kredsManager.uiContainer.gameObject.SetActive(true);
        kredsManager.isAnimating = true; // Evitar que Update() interfiera

        // Animación de entrada (bajar desde hiddenUIPosition a originalUIPosition)
        float moveDownDuration = 0.2f; // Tiempo original de KredsManager
        float elapsedTime = 0f;
        Vector2 startPosition = kredsManager.hiddenUIPosition;
        while (elapsedTime < moveDownDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDownDuration;
            kredsManager.uiContainer.anchoredPosition = Vector2.Lerp(startPosition, kredsManager.originalUIPosition, t);
            yield return null;
        }
        kredsManager.uiContainer.anchoredPosition = kredsManager.originalUIPosition;

        // Esperar 0.30 segundos antes de sumar los Kreds
        yield return new WaitForSecondsRealtime(0.30f);

        // Conteo animado de 5000 Kreds durante 1.25 segundos
        int startValue = kredsManager.displayedTokens;
        int targetValue = kredsManager.totalTokens + KREDS_TO_ADD;
        float countDuration = 1.25f; // Duración fija del conteo
        elapsedTime = 0f;
        int bounceCount = 3; // Número de rebotes como en el original
        float bounceDuration = countDuration / bounceCount;
        Vector2 originalIconPosition = kredsManager.coinIcon.anchoredPosition;

        while (elapsedTime < countDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / countDuration;
            kredsManager.displayedTokens = (int)Mathf.Lerp(startValue, targetValue, t);

            // Animación de rebote del ícono
            int currentBounce = Mathf.FloorToInt(elapsedTime / bounceDuration);
            float bounceProgress = (elapsedTime % bounceDuration) / bounceDuration;
            float bounceHeight = 6f;
            Vector2 startBouncePosition = originalIconPosition - Vector2.up * bounceHeight * 0.4f;
            Vector2 peakPosition = originalIconPosition + Vector2.up * bounceHeight * 0.6f;

            if (bounceProgress < 0.4f)
            {
                float downProgress = bounceProgress / 0.4f;
                kredsManager.coinIcon.anchoredPosition = Vector2.Lerp(originalIconPosition, startBouncePosition, downProgress);
            }
            else
            {
                float upProgress = (bounceProgress - 0.4f) / 0.6f;
                kredsManager.coinIcon.anchoredPosition = Vector2.Lerp(startBouncePosition, peakPosition, upProgress);
            }

            kredsManager.UpdateHUD();
            yield return null;
        }

        // Finalizar el conteo
        kredsManager.totalTokens = targetValue;
        kredsManager.displayedTokens = kredsManager.totalTokens;
        kredsManager.coinIcon.anchoredPosition = originalIconPosition;
        kredsManager.UpdateHUD();

        // Esperar 0.6 segundos (tiempo original de KredsManager)
        yield return new WaitForSecondsRealtime(0.6f);

        // Animación de salida (subir a hiddenUIPosition)
        float moveUpDuration = 0.2f; // Tiempo original de KredsManager
        elapsedTime = 0f;
        startPosition = kredsManager.originalUIPosition;
        while (elapsedTime < moveUpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveUpDuration;
            kredsManager.uiContainer.anchoredPosition = Vector2.Lerp(startPosition, kredsManager.hiddenUIPosition, t);
            yield return null;
        }
        kredsManager.uiContainer.anchoredPosition = kredsManager.hiddenUIPosition;

        // Desbloquear el movimiento al terminar el conteo y la animación
        if (playerMovement != null)
        {
            playerMovement.SetMovementLocked(false);
        }

        // Terminar la animación y permitir que KredsManager.Update() tome el control
        kredsManager.isAnimating = false;
    }

    private IEnumerator ShowLine()
    {
        if (textField == null) yield break;

        textField.text = dialogueLines[lineIndex];
        textField.maxVisibleCharacters = 0;
        textField.ForceMeshUpdate();

        int totalVisibleChars = GetVisibleCharacterCount(dialogueLines[lineIndex]);
        int visibleCount = 0;
        string currentLine = dialogueLines[lineIndex];
        int nonSpaceCharCount = 0;

        while (visibleCount < totalVisibleChars)
        {
            visibleCount++;
            textField.maxVisibleCharacters = visibleCount;

            char currentChar = GetCharAtVisibleIndex(currentLine, visibleCount - 1);
            if (currentChar != ' ')
            {
                nonSpaceCharCount++;
                if (nonSpaceCharCount % 2 == 0) PlayDialogueSound(typingSound);
            }

            if (currentChar == ',') yield return new WaitForSecondsRealtime(commaPauseTime);
            else if (currentChar == '.' || currentChar == '?' || currentChar == '!') yield return new WaitForSecondsRealtime(periodPauseTime);
            else yield return new WaitForSecondsRealtime(typingTime);
        }

        if (Input_TB != null) Input_TB.gameObject.SetActive(true);
    }

    private char GetCharAtVisibleIndex(string line, int visibleIndex)
    {
        int visibleCount = 0;
        bool inTag = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '<') inTag = true;
            else if (c == '>') inTag = false;
            else if (!inTag)
            {
                if (visibleCount == visibleIndex) return c;
                visibleCount++;
            }
        }
        return '\0';
    }

    private int GetVisibleCharacterCount(string line)
    {
        int count = 0;
        bool inTag = false;

        foreach (char c in line)
        {
            if (c == '<') inTag = true;
            else if (c == '>') inTag = false;
            else if (!inTag) count++;
        }
        return count;
    }

    private void PlayDialogueSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = clip == typingSound ? 2.38f : originalPitch;
            audioSource.PlayOneShot(clip);
        }
    }
}