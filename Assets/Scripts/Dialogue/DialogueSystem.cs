using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    [SerializeField] private GameObject dialogueMark;
    [SerializeField] private GameObject textBox;
    [SerializeField] private TMP_Text textField1;
    [SerializeField] private TMP_Text textField2;
    [SerializeField, TextArea(1, 4)] private string[] dialogueLines;
    [SerializeField, TextArea(1, 4)] private string[] headlessDialogueLines;
    [SerializeField] private GameObject textBoxPortrait;
    [SerializeField] private Sprite[] portraitSprites;
    [SerializeField] private Sprite[] headlessPortraitSprites;
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
    private TMP_Text dialogueText;

    public bool IsDialogueActive => didDialogueStart;
    private PlayerMovement playerMovement;
    private GameObject playerObject;
    private CoinControllerUI coinControllerUI; // Nueva referencia

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        dialogueAdvanceSound = Resources.Load<AudioClip>("SFX/DialogueNEXT");
        dialogueEndSound = Resources.Load<AudioClip>("SFX/DialogueEND");

        coinControllerUI = FindObjectOfType<CoinControllerUI>(); // Buscar en la escena
        if (coinControllerUI == null) Debug.LogError("CoinControllerUI no encontrado en la escena.");

        if (textBox == null) { Debug.LogError("TextBox no está asignado."); return; }
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
            activeDialogueLines = new string[dialogueLines.Length + headlessDialogueLines.Length];
            dialogueLines.CopyTo(activeDialogueLines, 0);
            headlessDialogueLines.CopyTo(activeDialogueLines, dialogueLines.Length);

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
            activePortraitSprites = portraitSprites;
        }

        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect != null)
            textBoxRect.anchoredPosition = useAlternativePosition ? alternativeTextBoxPosition : originalTextBoxPosition;

        UpdatePortrait();

        if (playerMovement != null)
            playerMovement.SetDialogueActive(this);

        if (coinControllerUI != null)
            coinControllerUI.gameObject.SetActive(false); // Desactivar la UI al iniciar el diálogo

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
                coinControllerUI.gameObject.SetActive(true); // Reactivar la UI al terminar el diálogo

            if (Input_TB != null)
                Input_TB.gameObject.SetActive(false);
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