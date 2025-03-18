using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    [SerializeField] private GameObject dialogueMark;
    [SerializeField] private GameObject textBox;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField, TextArea(1, 4)] private string[] dialogueLines;
    [SerializeField] private GameObject textBoxPortrait;
    [SerializeField] private Sprite[] portraitSprites;
    [SerializeField] private Image Input_TB;
    [SerializeField] private bool useAlternativePosition = false;
    [SerializeField] private Vector2 alternativeTextBoxPosition = new Vector2(100, 100);
    [SerializeField] private AudioClip typingSound; // Sonido para cada dos caracteres, asignado desde Inspector
    [SerializeField] private Sprite idleSprite;     // Sprite específico que activará el parpadeo
    [SerializeField] private Sprite blinkSprite;   // Sprite de parpadeo
    
    private float typingTime = 0.05f;
    private float commaPauseTime = 0.25f;
    private float periodPauseTime = 0.48f;
    private bool isPlayerRange;
    private bool didDialogueStart;
    private int lineIndex;
    private Vector2 originalTextBoxPosition;
    private AudioSource audioSource;
    private AudioClip dialogueAdvanceSound;
    private AudioClip dialogueEndSound;
    private Image portraitImage; // Referencia al componente Image del portrait
    private List<Animator> sceneAnimators; // Lista para almacenar todos los animadores de la escena
    private List<AnimatorUpdateMode> originalUpdateModes; // Lista para guardar los modos originales

    public bool IsDialogueActive => didDialogueStart;
    private PlayerMovement playerMovement;

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Cargar los sonidos desde Resources/SFX (dentro de Assets/Resources)
        dialogueAdvanceSound = Resources.Load<AudioClip>("SFX/DialogueNEXT");
        dialogueEndSound = Resources.Load<AudioClip>("SFX/DialogueEND");

        // Validar que los sonidos se hayan cargado
        if (dialogueAdvanceSound == null)
        {
            Debug.LogError("No se pudo cargar DialogueNEXT desde Resources/SFX. Asegúrate de que esté en Assets/Resources/SFX");
        }
        if (dialogueEndSound == null)
        {
            Debug.LogError("No se pudo cargar DialogueEND desde Resources/SFX. Asegúrate de que esté en Assets/Resources/SFX");
        }
        if (typingSound == null)
        {
            Debug.LogWarning("TypingSound no está asignado en el Inspector.");
        }

        if (textBox == null)
        {
            Debug.LogError("TextBox no está asignado en el Inspector.");
            return;
        }

        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect == null)
        {
            Debug.LogError("TextBox no tiene un componente RectTransform.");
            return;
        }
        originalTextBoxPosition = textBoxRect.anchoredPosition;

        if (dialogueText == null)
        {
            Debug.LogError("DialogueText no está asignado en el Inspector.");
            return;
        }

        if (textBoxPortrait != null)
        {
            portraitImage = textBoxPortrait.GetComponent<Image>();
            if (!textBox.activeSelf)
            {
                textBoxPortrait.SetActive(false);
            }
        }

        if (Input_TB != null)
        {
            Input_TB.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Input_TB (Image) no está asignado en el Inspector.");
        }

        if (portraitSprites != null && portraitSprites.Length != dialogueLines.Length)
        {
            Debug.LogWarning("El número de portraitSprites no coincide con el número de dialogueLines.");
        }

        // Iniciar la corrutina de parpadeo si hay un idleSprite y blinkSprite asignados
        if (idleSprite != null && blinkSprite != null && portraitImage != null)
        {
            StartCoroutine(BlinkRoutine());
        }

        // Inicializar las listas para animadores
        sceneAnimators = new List<Animator>();
        originalUpdateModes = new List<AnimatorUpdateMode>();
    }

    void Update()
    {
        if (isPlayerRange && Input.GetKeyDown(KeyCode.C))
        {
            if (playerMovement != null && playerMovement.IsGrounded())
            {
                if (!didDialogueStart)
                {
                    StartDialogue();
                }
                else if (dialogueText.maxVisibleCharacters >= GetVisibleCharacterCount(dialogueLines[lineIndex]))
                {
                    NextDialogueLine();
                }
                else
                {
                    StopAllCoroutines();
                    dialogueText.maxVisibleCharacters = GetVisibleCharacterCount(dialogueLines[lineIndex]);
                    if (Input_TB != null) Input_TB.gameObject.SetActive(true);
                }
            }
        }
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
        if (dialogueMark != null) dialogueMark.SetActive(false);
        lineIndex = 0;
        Time.timeScale = 0f;

        // Girar el NPC en dirección opuesta al jugador, considerando la gravedad
        FacePlayer();

        // Configurar todos los animadores para usar UnscaledTime
        ConfigureAnimatorsForDialogue(true);

        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect != null)
        {
            textBoxRect.anchoredPosition = useAlternativePosition ? alternativeTextBoxPosition : originalTextBoxPosition;
        }

        UpdatePortrait();

        if (playerMovement != null)
        {
            playerMovement.SetDialogueActive(this);
        }

        if (Input_TB != null)
        {
            Input_TB.gameObject.SetActive(false);
        }

        StartCoroutine(ShowLine());
    }

    private void NextDialogueLine()
    {
        lineIndex++;
        if (lineIndex < dialogueLines.Length)
        {
            PlayDialogueSound(dialogueAdvanceSound);
            if (Input_TB != null) Input_TB.gameObject.SetActive(false);
            UpdatePortrait();
            StartCoroutine(ShowLine());
        }
        else
        {
            PlayDialogueSound(dialogueEndSound);
            didDialogueStart = false;
            textBox.SetActive(false);
            if (dialogueMark != null) dialogueMark.SetActive(true);
            if (textBoxPortrait != null) textBoxPortrait.SetActive(false);
            Time.timeScale = 1f;

            // Restaurar los modos originales de los animadores
            ConfigureAnimatorsForDialogue(false);

            if (playerMovement != null)
            {
                playerMovement.SetDialogueActive(null);
            }

            if (Input_TB != null)
            {
                Input_TB.gameObject.SetActive(true);
            }
        }
    }

    private IEnumerator ShowLine()
    {
        dialogueText.text = dialogueLines[lineIndex];
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int totalVisibleChars = GetVisibleCharacterCount(dialogueLines[lineIndex]);
        int visibleCount = 0;
        string currentLine = dialogueLines[lineIndex];
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
                {
                    PlayDialogueSound(typingSound);
                }
            }

            if (currentChar == ',')
            {
                yield return new WaitForSecondsRealtime(commaPauseTime);
            }
            else if (currentChar == '.')
            {
                yield return new WaitForSecondsRealtime(periodPauseTime);
            }
            else
            {
                yield return new WaitForSecondsRealtime(typingTime);
            }
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
                if (visibleCount == visibleIndex)
                {
                    return c;
                }
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

    private void UpdatePortrait()
    {
        if (textBoxPortrait != null)
        {
            textBoxPortrait.SetActive(true);
            if (portraitImage != null && portraitSprites != null && lineIndex < portraitSprites.Length)
            {
                portraitImage.sprite = portraitSprites[lineIndex];
            }
            else if (portraitImage != null)
            {
                portraitImage.sprite = null;
            }
        }
    }

    private void PlayDialogueSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(2f, 5f);
            yield return new WaitForSecondsRealtime(waitTime);

            if (portraitImage != null && portraitImage.sprite == idleSprite)
            {
                portraitImage.sprite = blinkSprite;
                yield return new WaitForSecondsRealtime(0.26f);
                if (didDialogueStart && lineIndex < portraitSprites.Length && portraitSprites[lineIndex] == idleSprite)
                {
                    portraitImage.sprite = idleSprite;
                }
            }
        }
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
                {
                    sceneAnimators[i].updateMode = originalUpdateModes[i];
                }
            }
        }
    }

    private void FacePlayer()
    {
        if (playerMovement != null)
        {
            // Obtener la dirección en la que mira el jugador (basada en su escala en X)
            float playerFacingDirection = Mathf.Sign(playerMovement.transform.localScale.x);
            bool isPlayerGravityNormal = playerMovement.IsGravityNormal(); // Obtener el estado de la gravedad del jugador
            Vector3 currentScale = transform.localScale;

            // Ajustar la dirección del NPC según la gravedad del jugador
            if (isPlayerGravityNormal)
            {
                // Gravedad normal: jugador mirando derecha -> NPC izquierda, y viceversa
                if (playerFacingDirection > 0) // Jugador mira a la derecha, NPC mira a la izquierda
                {
                    transform.localScale = new Vector3(Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
                }
                else if (playerFacingDirection < 0) // Jugador mira a la izquierda, NPC mira a la derecha
                {
                    transform.localScale = new Vector3(-Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
                }
            }
            else
            {
                // Gravedad invertida: jugador mirando derecha -> NPC derecha, y viceversa
                if (playerFacingDirection > 0) // Jugador mira a la derecha, NPC mira a la derecha
                {
                    transform.localScale = new Vector3(-Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
                }
                else if (playerFacingDirection < 0) // Jugador mira a la izquierda, NPC mira a la izquierda
                {
                    transform.localScale = new Vector3(Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerRange = true;
            if (dialogueMark != null) dialogueMark.SetActive(true);
            playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerRange = false;
            if (dialogueMark != null) dialogueMark.SetActive(false);
            playerMovement = null;
        }
    }
}