using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemMessage : MonoBehaviour
{
    [SerializeField] private GameObject textBox;
    [SerializeField, TextArea(1, 4)] private string[] dialogueLines;
    [SerializeField] private bool useAlternativePosition = false;
    [SerializeField] private Vector2 alternativeTextBoxPosition = new Vector2(100, 100);
    [SerializeField] private TMP_Text textField1;
    [SerializeField] private TMP_Text textField2;
    [SerializeField] private AudioClip fanfareSong;
    [SerializeField] private Image Input_TB;
    [SerializeField] private GameObject prefabToMove;
    [SerializeField] private Vector3 newPrefabPosition;

    private float typingTime = 0.05f;
    private float commaPauseTime = 0.25f;
    private float periodPauseTime = 0.48f;
    private bool didDialogueStart;
    private int lineIndex;
    private Vector2 originalTextBoxPosition;
    private AudioSource audioSource;
    private AudioClip dialogueAdvanceSound;
    private AudioClip dialogueEndSound;
    private AudioClip typingSound;
    private List<Animator> sceneAnimators;
    private List<AnimatorUpdateMode> originalUpdateModes;
    private string[] activeDialogueLines;
    private TMP_Text dialogueText;
    private PlayerMovement playerMovement;
    private Animator playerAnimator;
    private bool isWaitingForGround;
    private bool hasCollided;
    private float originalPitch;
    private SpriteRenderer spriteRenderer;
    private CoinControllerUI coinControllerUI;

    public bool IsDialogueActive => didDialogueStart;

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        originalPitch = audioSource.pitch;
        spriteRenderer = GetComponent<SpriteRenderer>();

        dialogueAdvanceSound = Resources.Load<AudioClip>("SFX/DialogueNEXT");
        dialogueEndSound = Resources.Load<AudioClip>("SFX/DialogueEND");
        typingSound = Resources.Load<AudioClip>("SFX/Dialogue2");

        coinControllerUI = FindObjectOfType<CoinControllerUI>();
        if (coinControllerUI == null) Debug.LogError("CoinControllerUI no encontrado en la escena.");

        if (textBox == null) { Debug.LogError("TextBox no está asignado."); return; }
        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        originalTextBoxPosition = textBoxRect.anchoredPosition;

        dialogueText = textField1 != null ? textField1 : textField2;
        if (textField2 != null && dialogueText == textField1) textField2.gameObject.SetActive(false);
        if (dialogueText != null && !didDialogueStart) dialogueText.gameObject.SetActive(false);

        if (Input_TB != null) Input_TB.gameObject.SetActive(false);

        sceneAnimators = new List<Animator>();
        originalUpdateModes = new List<AnimatorUpdateMode>();
    }

    void Update()
    {
        if (isWaitingForGround && playerMovement != null && playerMovement.IsGrounded())
        {
            isWaitingForGround = false;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            SetPlayerKeyItemAnimation(); // Forzar KeyItem inmediatamente al tocar el suelo
            StartCoroutine(PlayFanfareAndStartDialogue());
        }

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
                if (Input_TB != null) Input_TB.gameObject.SetActive(true);
            }
        }
    }

    private IEnumerator PlayFanfareAndStartDialogue()
    {
        if (fanfareSong != null && audioSource != null)
        {
            audioSource.PlayOneShot(fanfareSong);
            yield return new WaitForSecondsRealtime(fanfareSong.length + 0.35f);
        }
        StartDialogue();
    }

    private void StartDialogue()
    {
        if (textBox == null || dialogueText == null || dialogueLines == null || dialogueLines.Length == 0) return;

        didDialogueStart = true;
        textBox.SetActive(true);
        dialogueText.gameObject.SetActive(true);
        lineIndex = 0;
        Time.timeScale = 0f;

        ConfigureAnimatorsForDialogue(true);

        activeDialogueLines = dialogueLines;
        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect != null)
            textBoxRect.anchoredPosition = useAlternativePosition ? alternativeTextBoxPosition : originalTextBoxPosition;

        if (playerMovement != null) playerMovement.SetDialogueActive(this);
        if (Input_TB != null) Input_TB.gameObject.SetActive(false);

        StartCoroutine(ShowLine());
    }

    private void NextDialogueLine()
    {
        lineIndex++;
        if (lineIndex < activeDialogueLines.Length)
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
            dialogueText.gameObject.SetActive(false);
            Time.timeScale = 1f;

            ConfigureAnimatorsForDialogue(false);

            if (playerMovement != null)
            {
                playerMovement.SetDialogueActive(null);
                playerMovement.SetMovementLocked(false);
            }

            if (playerAnimator != null)
            {
                playerAnimator.SetBool("PlayKeyItem", false);
            }
            if (Input_TB != null) Input_TB.gameObject.SetActive(false);
            if (spriteRenderer != null) spriteRenderer.enabled = false;

            if (coinControllerUI != null)
                coinControllerUI.SetCoinUIPanelActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !hasCollided)
        {
            hasCollided = true;
            if (spriteRenderer != null) spriteRenderer.enabled = false;

            playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
            playerAnimator = collision.gameObject.GetComponent<Animator>();

            if (playerMovement != null)
            {
                if (coinControllerUI != null)
                    coinControllerUI.OnItemCollision();

                // Ajustar gravedad si está invertida, sin establecer Idle inmediatamente
                if (!playerMovement.IsGravityNormal())
                {
                    AdjustGravityWithInertia();
                }

                playerMovement.SetMovementLocked(true);
                playerMovement.rb.velocity = new Vector2(0f, playerMovement.rb.velocity.y);

                if (playerMovement.IsGrounded())
                {
                    if (spriteRenderer != null) spriteRenderer.enabled = true;
                    SetPlayerKeyItemAnimation(); // Forzar KeyItem si ya está en el suelo
                    StartCoroutine(PlayFanfareAndStartDialogue());
                }
                else
                {
                    isWaitingForGround = true; // Esperar a tocar el suelo
                }
            }
        }
    }

    private void AdjustGravityWithInertia()
    {
        if (playerMovement != null)
        {
            playerMovement.rb.gravityScale = Mathf.Abs(playerMovement.rb.gravityScale); // Gravedad positiva
            playerMovement.isGravityNormal = true;

            Vector3 center = playerMovement.boxCollider.bounds.center;
            playerMovement.transform.RotateAround(center, Vector3.forward, 180f);
            playerMovement.transform.RotateAround(center, Vector3.up, 180f);
        }
    }

    private void SetIdleAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", 0f);
            playerAnimator.SetBool("IsGrounded", playerMovement.IsGrounded());
            playerAnimator.SetFloat("VerticalSpeed", playerMovement.rb.velocity.y);
        }
    }

    private void SetPlayerKeyItemAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("PlayKeyItem", true);
            playerAnimator.SetFloat("Speed", 0f);
            playerAnimator.SetBool("IsGrounded", true);
            playerAnimator.SetFloat("VerticalSpeed", 0f);

            if (prefabToMove != null && playerMovement != null)
            {
                prefabToMove.transform.position = playerMovement.transform.position + newPrefabPosition;
            }
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
                if (sceneAnimators[i] != null) sceneAnimators[i].updateMode = originalUpdateModes[i];
            }
        }
    }
}