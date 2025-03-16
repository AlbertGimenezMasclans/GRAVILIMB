using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Necesario para el componente Image

public class DialogueSystem : MonoBehaviour
{   
    [SerializeField] private GameObject dialogueMark;
    [SerializeField] private GameObject textBox;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField, TextArea(1, 4)] private string[] dialogueLines;
    [SerializeField] private GameObject textBoxPortrait; // Referencia a TextBox_Portrait
    [SerializeField] private Sprite portraitSprite;     // Sprite para el retrato, asignable en el Inspector

    private float typingTime = 0.05f;

    private bool isPlayerRange;
    private bool didDialogueStart;
    private int lineIndex;

    void Start()
    {
        // Asegurarse de que el retrato esté desactivado al inicio si el textbox lo está
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
        didDialogueStart = true;
        textBox.SetActive(true);
        dialogueMark.SetActive(false);
        lineIndex = 0;
        Time.timeScale = 0f;

        // Configurar el retrato
        if (textBoxPortrait != null)
        {
            textBoxPortrait.SetActive(true);
            Image portraitImage = textBoxPortrait.GetComponent<Image>();
            if (portraitImage != null && portraitSprite != null)
            {
                portraitImage.sprite = portraitSprite; // Asignar el sprite desde el Inspector
            }
            else if (portraitImage == null)
            {
                Debug.LogError("TextBox_Portrait no tiene un componente Image.");
            }
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
            dialogueMark.SetActive(true);
            if (textBoxPortrait != null)
            {
                textBoxPortrait.SetActive(false); // Desactivar el retrato al finalizar
            }
            Time.timeScale = 1f;
        }
    }

    private IEnumerator ShowLine()
    {
        dialogueText.text = string.Empty;
        foreach (char ch in dialogueLines[lineIndex])
        {
            dialogueText.text += ch;
            yield return new WaitForSecondsRealtime(typingTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerRange = true;
            dialogueMark.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerRange = false;
            dialogueMark.SetActive(false);
        }
    }
}