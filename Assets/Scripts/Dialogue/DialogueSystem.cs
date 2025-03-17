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
    [SerializeField] private Sprite portraitSprite;

    [SerializeField] private bool useAlternativePosition = false;
    [SerializeField] private Vector2 alternativeTextBoxPosition = new Vector2(100, 100);

    private float typingTime = 0.05f;
    private bool isPlayerRange;
    private bool didDialogueStart;
    private int lineIndex;
    private Vector2 originalTextBoxPosition;

    public bool IsDialogueActive => didDialogueStart;
    private PlayerMovement playerMovement;

    void Start()
    {
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

        if (textBoxPortrait != null && !textBox.activeSelf)
        {
            textBoxPortrait.SetActive(false);
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
            else if (dialogueText.maxVisibleCharacters >= GetVisibleCharacterCount(dialogueLines[lineIndex]))
            {
                NextDialogueLine();
            }
            else
            {
                StopAllCoroutines();
                dialogueText.maxVisibleCharacters = GetVisibleCharacterCount(dialogueLines[lineIndex]);
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

        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect != null)
        {
            textBoxRect.anchoredPosition = useAlternativePosition ? alternativeTextBoxPosition : originalTextBoxPosition;
        }

        if (textBoxPortrait != null)
        {
            textBoxPortrait.SetActive(true);
            Image portraitImage = textBoxPortrait.GetComponent<Image>();
            if (portraitImage != null && portraitSprite != null)
            {
                portraitImage.sprite = portraitSprite;
            }
            else if (portraitImage == null)
            {
                Debug.LogError("TextBox_Portrait no tiene un componente Image.");
            }
        }

        if (playerMovement != null)
        {
            playerMovement.SetDialogueActive(this);
        }

        StartCoroutine(ShowLine());
    }

    private void NextDialogueLine()
    {
        lineIndex++;
        if (lineIndex < dialogueLines.Length)
        {
            StartCoroutine(ShowLine());
        }
        else
        {
            didDialogueStart = false;
            textBox.SetActive(false);
            if (dialogueMark != null) dialogueMark.SetActive(true);
            if (textBoxPortrait != null) textBoxPortrait.SetActive(false);
            Time.timeScale = 1f;

            if (playerMovement != null)
            {
                playerMovement.SetDialogueActive(null);
            }
        }
    }

    private IEnumerator ShowLine()
    {
        // Establecemos el texto completo desde el inicio
        dialogueText.text = dialogueLines[lineIndex];
        dialogueText.maxVisibleCharacters = 0; // Ocultamos todo al principio

        // Forzamos a TMP a calcular la disposición del texto
        dialogueText.ForceMeshUpdate();

        // Contamos solo los caracteres visibles (sin etiquetas)
        int totalVisibleChars = GetVisibleCharacterCount(dialogueLines[lineIndex]);
        int visibleCount = 0;

        while (visibleCount < totalVisibleChars)
        {
            visibleCount++;
            dialogueText.maxVisibleCharacters = visibleCount;
            yield return new WaitForSecondsRealtime(typingTime);
        }
    }

    // Calcula el número de caracteres visibles (excluyendo etiquetas)
    private int GetVisibleCharacterCount(string line)
    {
        int count = 0;
        bool inTag = false;

        foreach (char c in line)
        {
            if (c == '<')
            {
                inTag = true;
            }
            else if (c == '>')
            {
                inTag = false;
            }
            else if (!inTag)
            {
                count++;
            }
        }

        return count;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerRange = true;
            if (dialogueMark != null) dialogueMark.SetActive(true);
            playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("El jugador no tiene el componente PlayerMovement.");
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
        }
    }
}