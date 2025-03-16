using System.Collections;
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

    private float typingTime = 0.05f;

    private bool isPlayerRange;
    private bool didDialogueStart;
    private int lineIndex;

    // Propiedad pública para el estado del diálogo
    public bool IsDialogueActive => didDialogueStart;

    // Referencia al PlayerMovement para notificarle
    private PlayerMovement playerMovement;

    void Start()
    {
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

        // Notificar al PlayerMovement que el diálogo ha comenzado
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
            dialogueMark.SetActive(true);
            if (textBoxPortrait != null)
            {
                textBoxPortrait.SetActive(false);
            }
            Time.timeScale = 1f;

            // Notificar al PlayerMovement que el diálogo ha terminado
            if (playerMovement != null)
            {
                playerMovement.SetDialogueActive(null);
            }
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
            // Obtener la referencia al PlayerMovement cuando el jugador entra en rango
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
            dialogueMark.SetActive(false);
            // Limpiar la referencia cuando el jugador sale del rango
            playerMovement = null;
        }
    }
}