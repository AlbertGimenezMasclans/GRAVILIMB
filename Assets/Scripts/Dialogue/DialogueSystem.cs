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
            else if (dialogueText.text == dialogueLines[lineIndex])
            {
                NextDialogueLine();
            }
            else
            {
                StopAllCoroutines();
                dialogueText.text = dialogueLines[lineIndex];
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
        dialogueText.text = string.Empty;
        string fullLine = dialogueLines[lineIndex];

        // Separamos el texto en partes (etiquetas y contenido visible)
        List<string> parts = ParseLine(fullLine);
        string visibleText = GetVisibleText(parts); // Solo el texto visible
        string formattedText = string.Empty;

        // Construimos el texto completo con etiquetas desde el inicio
        foreach (string part in parts)
        {
            formattedText += part;
        }
        dialogueText.text = formattedText; // Establecemos el texto con etiquetas

        // Animamos solo el texto visible
        int visibleIndex = 0;
        string currentVisible = string.Empty;

        while (visibleIndex < visibleText.Length)
        {
            currentVisible += visibleText[visibleIndex];
            dialogueText.text = BuildTextWithVisible(parts, currentVisible);
            visibleIndex++;
            yield return new WaitForSecondsRealtime(typingTime);
        }
    }

    // Separa la línea en partes (etiquetas y texto visible)
    private List<string> ParseLine(string line)
    {
        List<string> parts = new List<string>();
        int i = 0;

        while (i < line.Length)
        {
            if (line[i] == '<')
            {
                // Encontramos una etiqueta
                int endTag = line.IndexOf('>', i);
                if (endTag == -1) break; // Etiqueta incompleta, paramos
                parts.Add(line.Substring(i, endTag - i + 1));
                i = endTag + 1;
            }
            else
            {
                // Encontramos texto visible
                int nextTag = line.IndexOf('<', i);
                if (nextTag == -1) nextTag = line.Length;
                parts.Add(line.Substring(i, nextTag - i));
                i = nextTag;
            }
        }

        return parts;
    }

    // Obtiene solo el texto visible (sin etiquetas)
    private string GetVisibleText(List<string> parts)
    {
        string visibleText = string.Empty;
        foreach (string part in parts)
        {
            if (!part.StartsWith("<"))
            {
                visibleText += part;
            }
        }
        return visibleText;
    }

    // Construye el texto completo con solo una parte del texto visible mostrado
    private string BuildTextWithVisible(List<string> parts, string currentVisible)
    {
        string result = string.Empty;
        int visibleIndex = 0;

        foreach (string part in parts)
        {
            if (part.StartsWith("<"))
            {
                result += part; // Añadimos la etiqueta tal cual
            }
            else
            {
                // Añadimos solo la porción visible que corresponde
                int remainingVisible = currentVisible.Length - visibleIndex;
                if (remainingVisible > 0)
                {
                    int charsToShow = Mathf.Min(remainingVisible, part.Length);
                    result += part.Substring(0, charsToShow);
                    visibleIndex += charsToShow;
                }
            }
        }

        return result;
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