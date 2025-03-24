using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Icon shown when player is in dialogue range")]
    [SerializeField] private GameObject dialogueMark;
    [Tooltip("Main dialogue text box container")]
    [SerializeField] private GameObject textBox;
    [Tooltip("Primary text field for dialogue (preferred if both are set)")]
    [SerializeField] private TMP_Text textField1;
    [Tooltip("Secondary text field for dialogue")]
    [SerializeField] private TMP_Text textField2;
    [Tooltip("Input indicator image")]
    [SerializeField] private Image Input_TB;

    [Header("Dialogue Content")]
    [Tooltip("Lines of dialogue for normal state")]
    [SerializeField, TextArea(1, 4)] private string[] dialogueLines;
    [Tooltip("Lines of dialogue when player is dismembered")]
    [SerializeField, TextArea(1, 4)] private string[] headlessDialogueLines;
    [Tooltip("Is this line spoken by another character? (Normal state)")]
    [SerializeField] private bool[] isOtherCharacterNormal; // Checkbox para diálogo normal
    [Tooltip("Is this line spoken by another character? (Headless state)")]
    [SerializeField] private bool[] isOtherCharacterHeadless; // Checkbox para diálogo headless

    [Header("Portrait Settings")]
    [Tooltip("GameObject containing the portrait image")]
    [SerializeField] private GameObject textBoxPortrait;
    [Tooltip("Sprites for normal dialogue portraits")]
    [SerializeField] private Sprite[] portraitSprites;
    [Tooltip("Sprites for headless dialogue portraits")]
    [SerializeField] private Sprite[] headlessPortraitSprites;

    [Header("Position Settings")]
    [Tooltip("Use an alternative position for the text box?")]
    [SerializeField] private bool useAlternativePosition = false;
    [Tooltip("Custom position for text box when using alternative position")]
    [SerializeField] private Vector2 alternativeTextBoxPosition = new Vector2(100, 100);

    [Header("Audio Settings")]
    [Tooltip("Sound played during text typing")]
    [SerializeField] private AudioClip typingSound;

    [Header("Blink Animation")]
    [Tooltip("Default portrait sprite when idle")]
    [SerializeField] private Sprite idleSprite;
    [Tooltip("Portrait sprite during blink animation")]
    [SerializeField] private Sprite blinkSprite;

    private float typingTime = 0.05f;
    private float commaPauseTime = 0.25f;
    private float periodPauseTime = 0.48f;
    private const float otherCharacterDelay = 0.25f; // Retraso de 0.25 segundos para "otro personaje"
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
    private bool[] activeIsOtherCharacter; // Array activo para el checkbox
    private TMP_Text dialogueText;

    public bool IsDialogueActive => didDialogueStart;
    private PlayerMovement playerMovement;
    private GameObject playerObject;
    private CoinControllerUI coinControllerUI;

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        dialogueAdvanceSound = Resources.Load<AudioClip>("SFX/DialogueNEXT");
        dialogueEndSound = Resources.Load<AudioClip>("SFX/DialogueEND");

        coinControllerUI = FindObjectOfType<CoinControllerUI>();
        if (coinControllerUI == null) Debug.LogError("CoinControllerUI not found in the scene.");

        if (textBox == null) { Debug.LogError("TextBox is not assigned."); return; }
        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        originalTextBoxPosition = textBoxRect.anchoredPosition;

        dialogueText = textField1 != null ? textField1 : textField2;
        if (textField2 != null && dialogueText == textField1) textField2.gameObject.SetActive(false);
        if (dialogueText != null && !didDialogueStart) dialogueText.gameObject.SetActive(false);

        if (textBoxPortrait != null)
        {
            portraitImage = textBoxPortrait.GetComponent<Image>();
            if (!textBox.activeSelf) textBoxPortrait.SetActive(false);
        }

        if (Input_TB != null) Input_TB.gameObject.SetActive(false);

        if (idleSprite != null && blinkSprite != null && portraitImage != null)
            StartCoroutine(BlinkRoutine());

        sceneAnimators = new List<Animator>();
        originalUpdateModes = new List<AnimatorUpdateMode>();

        // Validar que los arrays de "isOtherCharacter" tengan la longitud correcta
        if (isOtherCharacterNormal == null || isOtherCharacterNormal.Length != dialogueLines.Length)
        {
            Debug.LogWarning($"isOtherCharacterNormal length does not match dialogueLines length. Adjusting...");
            isOtherCharacterNormal = new bool[dialogueLines.Length];
        }
        if (headlessDialogueLines != null && (isOtherCharacterHeadless == null || isOtherCharacterHeadless.Length != headlessDialogueLines.Length))
        {
            Debug.LogWarning($"isOtherCharacterHeadless length does not match headlessDialogueLines length. Adjusting...");
            isOtherCharacterHeadless = new bool[headlessDialogueLines.Length];
        }
    }

    void Update()
    {
        if (isPlayerRange && Input.GetKeyDown(KeyCode.C))
        {
            if (!didDialogueStart)
            {
                StartDialogue();
            }
            else if (dialogueText != null && dialogueText.maxVisibleCharacters >= GetVisibleCharacterCount(activeDialogueLines[lineIndex]))
            {
                NextDialogueLine();
            }
            else if (dialogueText != null)
            {
                StopAllCoroutines();
                dialogueText.maxVisibleCharacters = GetVisibleCharacterCount(activeDialogueLines[lineIndex]);
                if (Input_TB != null) Input_TB.gameObject.SetActive(true);
            }
        }
    }

    private void StartDialogue()
    {
        if (textBox == null || dialogueText == null || dialogueLines == null || dialogueLines.Length == 0) return;

        didDialogueStart = true;
        textBox.SetActive(true);
        if (dialogueMark != null) dialogueMark.SetActive(false);
        dialogueText.gameObject.SetActive(true);
        lineIndex = 0;
        Time.timeScale = 0f;

        FacePlayer();
        ConfigureAnimatorsForDialogue(true);

        bool isDismembered = false;
        if (playerMovement != null)
        {
            isDismembered = playerMovement.isDismembered;
        }
        else if (playerObject != null && playerObject.GetComponent<Dismember>() != null)
        {
            isDismembered = true;
        }

        if (isDismembered && headlessDialogueLines != null && headlessDialogueLines.Length > 0)
        {
            // Combinar los diálogos normales y headless
            activeDialogueLines = new string[dialogueLines.Length + headlessDialogueLines.Length];
            dialogueLines.CopyTo(activeDialogueLines, 0);
            headlessDialogueLines.CopyTo(activeDialogueLines, dialogueLines.Length);

            // Combinar los retrasos de "otro personaje"
            activeIsOtherCharacter = new bool[dialogueLines.Length + headlessDialogueLines.Length];
            isOtherCharacterNormal.CopyTo(activeIsOtherCharacter, 0);
            isOtherCharacterHeadless.CopyTo(activeIsOtherCharacter, dialogueLines.Length);

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
            activeDialogueLines = dialogueLines;
            activeIsOtherCharacter = isOtherCharacterNormal;
            activePortraitSprites = portraitSprites;
        }

        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect != null)
            textBoxRect.anchoredPosition = useAlternativePosition ? alternativeTextBoxPosition : originalTextBoxPosition;

        UpdatePortrait();

        if (playerMovement != null)
            playerMovement.SetDialogueActive(this);

        if (coinControllerUI != null)
            coinControllerUI.gameObject.SetActive(false);

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
            dialogueText.gameObject.SetActive(false);
            Time.timeScale = 1f;

            ConfigureAnimatorsForDialogue(false);

            if (playerMovement != null)
                playerMovement.SetDialogueActive(null);

            if (coinControllerUI != null)
                coinControllerUI.gameObject.SetActive(true);

            if (Input_TB != null)
                Input_TB.gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowLine()
    {
        if (dialogueText == null) yield break;

        // Desactivar inicialmente el texto y el retrato
        dialogueText.gameObject.SetActive(false);
        if (textBoxPortrait != null) textBoxPortrait.SetActive(false);

        // Comprobar si esta línea tiene el retraso de "otro personaje"
        bool applyDelay = activeIsOtherCharacter[lineIndex];
        if (applyDelay)
        {
            yield return new WaitForSecondsRealtime(otherCharacterDelay); // Retraso de 0.70 segundos
        }

        // Activar el texto y el retrato después del retraso (si aplica)
        dialogueText.gameObject.SetActive(true);
        if (textBoxPortrait != null) textBoxPortrait.SetActive(true);

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
            // No activamos aquí, ya que ShowLine se encarga de activar/desactivar
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