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
    [SerializeField, TextArea(1, 4)] private string[] dialogueLines; // Diálogos normales
    [SerializeField, TextArea(1, 4)] private string[] headlessDialogueLines; // Diálogos adicionales para desmembrado
    [SerializeField] private GameObject textBoxPortrait;
    [SerializeField] private Sprite[] portraitSprites; // Retratos normales
    [SerializeField] private Sprite[] headlessPortraitSprites; // Retratos para desmembrado
    [SerializeField] private Image Input_TB;
    [SerializeField] private bool useAlternativePosition = false;
    [SerializeField] private Vector2 alternativeTextBoxPosition = new Vector2(100, 100);
    [SerializeField] private AudioClip typingSound;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite blinkSprite;
    
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
    private Image portraitImage;
    private List<Animator> sceneAnimators;
    private List<AnimatorUpdateMode> originalUpdateModes;
    private string[] activeDialogueLines;
    private Sprite[] activePortraitSprites;

    public bool IsDialogueActive => didDialogueStart;
    private PlayerMovement playerMovement; // Para el estado normal
    private GameObject playerObject; // Objeto que entró al trigger (cuerpo o cabeza)

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        dialogueAdvanceSound = Resources.Load<AudioClip>("SFX/DialogueNEXT");
        dialogueEndSound = Resources.Load<AudioClip>("SFX/DialogueEND");

        if (dialogueAdvanceSound == null) Debug.LogError("No se pudo cargar DialogueNEXT desde Resources/SFX.");
        if (dialogueEndSound == null) Debug.LogError("No se pudo cargar DialogueEND desde Resources/SFX.");
        if (typingSound == null) Debug.LogWarning("TypingSound no está asignado.");

        if (textBox == null) { Debug.LogError("TextBox no está asignado."); return; }
        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect == null) { Debug.LogError("TextBox no tiene RectTransform."); return; }
        originalTextBoxPosition = textBoxRect.anchoredPosition;

        if (dialogueText == null) { Debug.LogError("DialogueText no está asignado."); return; }

        if (textBoxPortrait != null)
        {
            portraitImage = textBoxPortrait.GetComponent<Image>();
            if (!textBox.activeSelf) textBoxPortrait.SetActive(false);
        }

        if (Input_TB != null) Input_TB.gameObject.SetActive(false);
        else Debug.LogError("Input_TB no está asignado.");

        if (portraitSprites != null && portraitSprites.Length != dialogueLines.Length)
            Debug.LogWarning("El número de portraitSprites no coincide con dialogueLines.");
        if (headlessPortraitSprites != null && headlessPortraitSprites.Length != headlessDialogueLines.Length)
            Debug.LogWarning("El número de headlessPortraitSprites no coincide con headlessDialogueLines.");

        if (idleSprite != null && blinkSprite != null && portraitImage != null)
            StartCoroutine(BlinkRoutine());

        sceneAnimators = new List<Animator>();
        originalUpdateModes = new List<AnimatorUpdateMode>();
    }

    void Update()
    {
        if (isPlayerRange && Input.GetKeyDown(KeyCode.C))
        {
            if (!didDialogueStart)
            {
                StartDialogue();
            }
            else if (dialogueText.maxVisibleCharacters >= GetVisibleCharacterCount(activeDialogueLines[lineIndex]))
            {
                NextDialogueLine();
            }
            else
            {
                StopAllCoroutines();
                dialogueText.maxVisibleCharacters = GetVisibleCharacterCount(activeDialogueLines[lineIndex]);
                if (Input_TB != null) Input_TB.gameObject.SetActive(true);
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

        FacePlayer();
        ConfigureAnimatorsForDialogue(true);

        // Determinar si el jugador está desmembrado
        bool isDismembered = false;
        if (playerMovement != null)
        {
            isDismembered = playerMovement.isDismembered;
        }
        else if (playerObject != null && playerObject.GetComponent<Dismember>() != null)
        {
            isDismembered = true;
        }

        // Configurar los diálogos activos
        if (isDismembered && headlessDialogueLines != null && headlessDialogueLines.Length > 0)
        {
            // Combinar diálogos normales con los de desmembrado
            activeDialogueLines = new string[dialogueLines.Length + headlessDialogueLines.Length];
            dialogueLines.CopyTo(activeDialogueLines, 0);
            headlessDialogueLines.CopyTo(activeDialogueLines, dialogueLines.Length);

            // Combinar retratos (si hay retratos para desmembrado, si no, repetir los normales)
            if (headlessPortraitSprites != null && headlessPortraitSprites.Length > 0)
            {
                activePortraitSprites = new Sprite[portraitSprites.Length + headlessPortraitSprites.Length];
                portraitSprites.CopyTo(activePortraitSprites, 0);
                headlessPortraitSprites.CopyTo(activePortraitSprites, portraitSprites.Length);
            }
            else
            {
                activePortraitSprites = new Sprite[dialogueLines.Length + headlessDialogueLines.Length];
                for (int i = 0; i < activePortraitSprites.Length; i++)
                {
                    activePortraitSprites[i] = portraitSprites[Mathf.Min(i, portraitSprites.Length - 1)];
                }
            }
        }
        else
        {
            // Solo diálogos normales si no está desmembrado
            activeDialogueLines = dialogueLines;
            activePortraitSprites = portraitSprites;
        }

        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect != null)
            textBoxRect.anchoredPosition = useAlternativePosition ? alternativeTextBoxPosition : originalTextBoxPosition;

        UpdatePortrait();

        if (playerMovement != null)
            playerMovement.SetDialogueActive(this);

        if (Input_TB != null)
            Input_TB.gameObject.SetActive(false);

        StartCoroutine(ShowLine());
    }

    private void NextDialogueLine()
    {
        lineIndex++;
        if (lineIndex < activeDialogueLines.Length)
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

            ConfigureAnimatorsForDialogue(false);

            if (playerMovement != null)
                playerMovement.SetDialogueActive(null);

            if (Input_TB != null)
                Input_TB.gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowLine()
    {
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

    private void UpdatePortrait()
    {
        if (textBoxPortrait != null)
        {
            textBoxPortrait.SetActive(true);
            if (portraitImage != null && activePortraitSprites != null && lineIndex < activePortraitSprites.Length)
                portraitImage.sprite = activePortraitSprites[lineIndex];
            else if (portraitImage != null)
                portraitImage.sprite = null;
        }
    }

    private void PlayDialogueSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
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
                if (didDialogueStart && lineIndex < activePortraitSprites.Length && activePortraitSprites[lineIndex] == idleSprite)
                    portraitImage.sprite = idleSprite;
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
                    sceneAnimators[i].updateMode = originalUpdateModes[i];
            }
        }
    }

    private void FacePlayer()
    {
        if (playerMovement != null)
        {
            float playerFacingDirection = Mathf.Sign(playerMovement.transform.localScale.x);
            bool isPlayerGravityNormal = playerMovement.IsGravityNormal();
            Vector3 currentScale = transform.localScale;

            if (isPlayerGravityNormal)
                transform.localScale = new Vector3(
                    playerFacingDirection > 0 ? Mathf.Abs(currentScale.x) : -Mathf.Abs(currentScale.x),
                    currentScale.y, currentScale.z);
            else
                transform.localScale = new Vector3(
                    playerFacingDirection > 0 ? -Mathf.Abs(currentScale.x) : Mathf.Abs(currentScale.x),
                    currentScale.y, currentScale.z);
        }
        else if (playerObject != null)
        {
            float playerFacingDirection = Mathf.Sign(playerObject.transform.localScale.x);
            Vector3 currentScale = transform.localScale;
            transform.localScale = new Vector3(
                playerFacingDirection > 0 ? Mathf.Abs(currentScale.x) : -Mathf.Abs(currentScale.x),
                currentScale.y, currentScale.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerRange = true;
            if (dialogueMark != null) dialogueMark.SetActive(true);

            playerObject = collision.gameObject;
            playerMovement = collision.gameObject.GetComponent<PlayerMovement>();

            if (playerMovement == null && collision.gameObject.GetComponent<Dismember>() != null)
            {
                Debug.Log("Detectada cabeza desmembrada.");
            }
            else if (playerMovement != null)
            {
                Debug.Log("Detectado cuerpo completo. isDismembered: " + playerMovement.isDismembered);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerRange = false;
            if (dialogueMark != null) dialogueMark.SetActive(false);
            playerMovement = null;
            playerObject = null;
        }
    }
}