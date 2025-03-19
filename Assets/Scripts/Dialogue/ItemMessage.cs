using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemMessage : MonoBehaviour
{
    [SerializeField] private GameObject dialogueMark;
    [SerializeField] private GameObject textBox;
    [SerializeField, TextArea(1, 4)] private string[] dialogueLines; // Diálogos normales
    [SerializeField] private bool useAlternativePosition = false;
    [SerializeField] private Vector2 alternativeTextBoxPosition = new Vector2(100, 100);
    [SerializeField] private TMP_Text textField1; // Primer campo de texto
    [SerializeField] private TMP_Text textField2; // Segundo campo de texto
    [SerializeField] private AudioClip fanfareSong; // Sonido "Fanfare Song" especificado en el Inspector
    [SerializeField] private Image Input_TB; // Imagen para indicar que se puede avanzar
    
    private float typingTime = 0.05f;
    private float commaPauseTime = 0.25f;
    private float periodPauseTime = 0.48f;
    private bool didDialogueStart;
    private int lineIndex;
    private Vector2 originalTextBoxPosition;
    private AudioSource audioSource;
    private AudioClip dialogueAdvanceSound;
    private AudioClip dialogueEndSound;
    private AudioClip typingSound; // Sonido fijo "Dialogue2"
    private List<Animator> sceneAnimators;
    private List<AnimatorUpdateMode> originalUpdateModes;
    private string[] activeDialogueLines;
    private TMP_Text dialogueText; // Campo activo seleccionado
    private PlayerMovement playerMovement; // Para verificar el estado del jugador
    private Animator playerAnimator; // Para controlar la animación del jugador
    private bool isWaitingForGround; // Para controlar la espera del suelo
    private bool hasCollided; // Para rastrear si ya colisionó

    public bool IsDialogueActive => didDialogueStart;

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        dialogueAdvanceSound = Resources.Load<AudioClip>("SFX/DialogueNEXT");
        dialogueEndSound = Resources.Load<AudioClip>("SFX/DialogueEND");
        typingSound = Resources.Load<AudioClip>("SFX/Dialogue2");

        if (dialogueAdvanceSound == null) Debug.LogError("No se pudo cargar DialogueNEXT desde Resources/SFX.");
        if (dialogueEndSound == null) Debug.LogError("No se pudo cargar DialogueEND desde Resources/SFX.");
        if (typingSound == null) Debug.LogError("No se pudo cargar Dialogue2 desde Resources/SFX.");
        if (fanfareSong == null) Debug.LogWarning("Fanfare Song no está asignado en el Inspector.");

        if (textBox == null) { Debug.LogError("TextBox no está asignado."); return; }
        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect == null) { Debug.LogError("TextBox no tiene RectTransform."); return; }
        originalTextBoxPosition = textBoxRect.anchoredPosition;

        // Determinar qué campo de texto usar
        if (textField1 != null)
        {
            dialogueText = textField1;
            if (textField2 != null) textField2.gameObject.SetActive(false); // Desactiva el otro si está asignado
        }
        else if (textField2 != null)
        {
            dialogueText = textField2;
        }
        else
        {
            Debug.LogError("Ningún campo de texto (textField1 o textField2) está asignado en el Inspector.");
        }

        // Asegurarse de que el texto esté inicialmente oculto si no está activo
        if (dialogueText != null && !didDialogueStart)
        {
            dialogueText.gameObject.SetActive(false);
        }

        if (Input_TB != null)
            Input_TB.gameObject.SetActive(false);
        else
            Debug.LogError("Input_TB no está asignado en el Inspector.");

        sceneAnimators = new List<Animator>();
        originalUpdateModes = new List<AnimatorUpdateMode>();
    }

    void Update()
    {
        // Manejar la espera hasta que el jugador toque el suelo
        if (isWaitingForGround && playerMovement != null && playerMovement.IsGrounded())
        {
            isWaitingForGround = false;
            playerMovement.SetMovementLocked(false); // Desbloquear movimiento
            SetPlayerKeyItemAnimation();
            StartCoroutine(PlayFanfareAndStartDialogue());
        }

        // Control del diálogo una vez iniciado
        if (didDialogueStart && Input.GetKeyDown(KeyCode.C))
        {
            if (dialogueText != null && dialogueText.maxVisibleCharacters >= GetVisibleCharacterCount(activeDialogueLines[lineIndex]))
            {
                NextDialogueLine();
            }
            else if (dialogueText != null)
            {
                StopAllCoroutines();
                dialogueText.maxVisibleCharacters = GetVisibleCharacterCount(activeDialogueLines[lineIndex]);
                if (Input_TB != null) Input_TB.gameObject.SetActive(true); // Mostrar Input_TB cuando termine de escribir
            }
        }
    }

    private IEnumerator PlayFanfareAndStartDialogue()
    {
        if (fanfareSong != null && audioSource != null)
        {
            audioSource.PlayOneShot(fanfareSong);
            // Esperar la duración del sonido más 1 segundo
            yield return new WaitForSecondsRealtime(fanfareSong.length + 0.35f);
        }
        StartDialogue();
    }

    private void StartDialogue()
    {
        if (textBox == null || dialogueText == null || dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogError("Faltan referencias o dialogueLines está vacío.");
            return;
        }

        didDialogueStart = true;
        textBox.SetActive(true);
        dialogueText.gameObject.SetActive(true); // Activar el campo de texto seleccionado
        lineIndex = 0;
        Time.timeScale = 0f;

        ConfigureAnimatorsForDialogue(true);

        activeDialogueLines = dialogueLines;

        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect != null)
            textBoxRect.anchoredPosition = useAlternativePosition ? alternativeTextBoxPosition : originalTextBoxPosition;

        if (playerMovement != null)
            playerMovement.SetDialogueActive(this);

        if (Input_TB != null) Input_TB.gameObject.SetActive(false); // Asegurarse de que Input_TB esté oculto al inicio

        StartCoroutine(ShowLine());
    }

    private void NextDialogueLine()
    {
        lineIndex++;
        if (lineIndex < activeDialogueLines.Length)
        {
            PlayDialogueSound(dialogueAdvanceSound);
            if (Input_TB != null) Input_TB.gameObject.SetActive(false); // Ocultar Input_TB al avanzar
            StartCoroutine(ShowLine());
        }
        else
        {
            PlayDialogueSound(dialogueEndSound);
            didDialogueStart = false;
            textBox.SetActive(false);
            dialogueText.gameObject.SetActive(false); // Ocultar el campo de texto al finalizar
            Time.timeScale = 1f;

            ConfigureAnimatorsForDialogue(false);

            if (playerMovement != null)
                playerMovement.SetDialogueActive(null);

            // Resetear la animación a Idle cuando el diálogo termine
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("PlayKeyItem", false);
            }

            if (Input_TB != null) Input_TB.gameObject.SetActive(false); // Ocultar Input_TB al finalizar
        }
    }

    private IEnumerator ShowLine()
    {
        if (dialogueText == null) yield break;

        dialogueText.text = activeDialogueLines[lineIndex];
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int totalVisibleChars = GetVisibleCharacterCount(activeDialogueLines[lineIndex]);
        int visibleCount = 0;
        string currentLine = activeDialogueLines[lineIndex];
        int nonSpaceCharCount = 0;

        while (visibleCount < totalVisibleChars)
        {
            visibleCount++;
            dialogueText.maxVisibleCharacters = visibleCount;

            char currentChar = GetCharAtVisibleIndex(currentLine, visibleCount - 1);

            if (currentChar != ' ')
            {
                nonSpaceCharCount++;
                if (nonSpaceCharCount % 2 == 0)
                    PlayDialogueSound(typingSound);
            }

            if (currentChar == ',')
                yield return new WaitForSecondsRealtime(commaPauseTime);
            else if (currentChar == '.' || currentChar == '?' || currentChar == '!')
                yield return new WaitForSecondsRealtime(periodPauseTime);
            else
                yield return new WaitForSecondsRealtime(typingTime);
        }

        if (Input_TB != null) Input_TB.gameObject.SetActive(true); // Mostrar Input_TB cuando termine de escribir
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
            audioSource.PlayOneShot(clip);
    }

    private void ConfigureAnimatorsForDialogue(bool isStarting)
    {
        if (isStarting)
        {
            sceneAnimators.Clear();
            originalUpdateModes.Clear();
            Animator[] animators = FindObjectsOfType<Animator>();
            foreach (Animator animator in animators)
            {
                sceneAnimators.Add(animator);
                originalUpdateModes.Add(animator.updateMode);
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }
        else
        {
            for (int i = 0; i < sceneAnimators.Count; i++)
            {
                if (sceneAnimators[i] != null)
                    sceneAnimators[i].updateMode = originalUpdateModes[i];
            }
        }
    }

    private void SetPlayerKeyItemAnimation()
    {
        if (playerAnimator != null)
        {
            // Forzar la animación a Protagonist_KeyItem usando un parámetro booleano
            playerAnimator.SetBool("PlayKeyItem", true);
            // Asegurar que otras animaciones no interfieran
            playerAnimator.SetFloat("Speed", 0f);
            playerAnimator.SetBool("IsGrounded", true);
            playerAnimator.SetFloat("VerticalSpeed", 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !hasCollided)
        {
            // Marcar que ya colisionó para evitar múltiples activaciones
            hasCollided = true;

            // Hacer el objeto invisible
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
            else
            {
                Debug.LogWarning("No se encontró SpriteRenderer en el objeto. Asegúrate de que tenga uno si quieres que se haga invisible.");
            }
            if (dialogueMark != null) dialogueMark.SetActive(false);

            // Obtener referencias al PlayerMovement y Animator
            playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
            playerAnimator = collision.gameObject.GetComponent<Animator>();

            if (playerMovement != null)
            {
                // Verificar si el jugador está en el suelo
                if (playerMovement.IsGrounded())
                {
                    // Si está en el suelo, establecer animación Protagonist_KeyItem y reproducir fanfare antes del diálogo
                    SetPlayerKeyItemAnimation();
                    StartCoroutine(PlayFanfareAndStartDialogue());
                }
                else
                {
                    // Si no está en el suelo, bloquear movimiento y esperar a que lo esté
                    playerMovement.SetMovementLocked(true);
                    isWaitingForGround = true;
                }
            }
            else
            {
                Debug.LogError("No se encontró PlayerMovement en el objeto con tag 'Player'.");
            }

            if (playerAnimator == null)
            {
                Debug.LogError("No se encontró Animator en el objeto con tag 'Player'.");
            }
        }
    }
}