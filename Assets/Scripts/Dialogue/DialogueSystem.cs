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
    [SerializeField] private Sprite[] portraitSprites; // Arreglo de Sprites, uno por línea de diálogo
    [SerializeField] private Image Input_TB; // Imagen en el Canvas

    [SerializeField] private bool useAlternativePosition = false;
    [SerializeField] private Vector2 alternativeTextBoxPosition = new Vector2(100, 100);

    private float typingTime = 0.05f;
    private float commaPauseTime = 0.25f; // Pausa después de una coma (ajustable)
    private float periodPauseTime = 0.48f; // Pausa después de un punto (mayor que la coma, ajustable)
    private bool isPlayerRange;
    private bool didDialogueStart;
    private int lineIndex;
    private Vector2 originalTextBoxPosition;

    public bool IsDialogueActive => didDialogueStart;
    private PlayerMovement playerMovement;

    void Start()
    {
        // Validaciones iniciales
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

        // Configurar Input_TB (como imagen del Canvas)
        if (Input_TB != null)
        {
            Input_TB.gameObject.SetActive(false); // Desactivado por defecto
        }
        else
        {
            Debug.LogError("Input_TB (Image) no está asignado en el Inspector.");
        }

        // Validar que portraitSprites tenga el mismo tamaño que dialogueLines
        if (portraitSprites != null && portraitSprites.Length != dialogueLines.Length)
        {
            Debug.LogWarning("El número de portraitSprites no coincide con el número de dialogueLines. Ajusta los tamaños en el Inspector.");
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
                // Activar Input_TB cuando el texto se muestra completamente
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

        RectTransform textBoxRect = textBox.GetComponent<RectTransform>();
        if (textBoxRect != null)
        {
            textBoxRect.anchoredPosition = useAlternativePosition ? alternativeTextBoxPosition : originalTextBoxPosition;
        }

        UpdatePortrait(); // Actualizar el retrato para la primera línea

        if (playerMovement != null)
        {
            playerMovement.SetDialogueActive(this);
        }

        if (Input_TB != null)
        {
            Input_TB.gameObject.SetActive(false); // Desactivado al iniciar el diálogo
        }

        StartCoroutine(ShowLine());
    }

    private void NextDialogueLine()
    {
        lineIndex++;
        if (lineIndex < dialogueLines.Length)
        {
            if (Input_TB != null) Input_TB.gameObject.SetActive(false); // Desactivar antes de mostrar la siguiente línea
            UpdatePortrait(); // Actualizar el retrato para la siguiente línea
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

            // Activar Input_TB al finalizar el diálogo
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

        while (visibleCount < totalVisibleChars)
        {
            visibleCount++;
            dialogueText.maxVisibleCharacters = visibleCount;

            // Obtener el carácter actual (teniendo en cuenta etiquetas)
            char currentChar = GetCharAtVisibleIndex(currentLine, visibleCount - 1);

            // Pausar si es una coma o un punto
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

        // Activar Input_TB cuando el texto se ha mostrado completamente
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
        return '\0'; // Carácter nulo si no se encuentra (no debería ocurrir)
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
            Image portraitImage = textBoxPortrait.GetComponent<Image>();
            if (portraitImage != null && portraitSprites != null && lineIndex < portraitSprites.Length)
            {
                portraitImage.sprite = portraitSprites[lineIndex]; // Asignar el Sprite correspondiente a la línea actual
            }
            else if (portraitImage != null)
            {
                portraitImage.sprite = null; // Si no hay Sprite disponible, dejarlo vacío
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