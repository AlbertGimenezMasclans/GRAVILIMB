using UnityEngine;
using TMPro;

public class HabSelector : MonoBehaviour
{
    [Header("Hability Objects")]
    [SerializeField, Tooltip("GameObject representing the Gravity ability icon")] private GameObject gravityObject;
    [SerializeField, Tooltip("GameObject representing the Dismember ability icon")] private GameObject dismemberObject;
    [SerializeField, Tooltip("GameObject representing the Shooting ability icon")] private GameObject shootingObject;
    
    [Header("Cursor Settings")]
    [SerializeField, Tooltip("GameObject representing the cursor")] private GameObject cursor;
    [SerializeField, Tooltip("Sprite for the cursor in its default state")] private Sprite originalCursorSprite;
    [SerializeField, Tooltip("Sprite for the cursor when selecting/moving")] private Sprite selectionCursorSprite;
    [SerializeField, Tooltip("Time interval (in seconds) for alternating cursor sprites")] private float spriteChangeInterval = 0.5f;
    [SerializeField, Tooltip("Cooldown (in seconds) between cursor movements")] private float moveCooldown = 1.0f;

    [Header("Ability Sprites")]
    [SerializeField, Tooltip("Sprite shown when an ability is locked")] private Sprite lockedSprite;
    [SerializeField, Tooltip("Sprite for the Gravity ability when unlocked")] private Sprite gravitySprite;
    [SerializeField, Tooltip("Sprite for the Dismember ability when unlocked")] private Sprite dismemberSprite;
    [SerializeField, Tooltip("Sprite for the Shooting ability when unlocked")] private Sprite shootingSprite;

    [Header("Ability Names")]
    [SerializeField, Tooltip("Name displayed for the Gravity ability")] private string gravityName = "Zer0 Bootz";
    [SerializeField, Tooltip("Name displayed for the Shooting ability")] private string shootingName = "Gun";
    [SerializeField, Tooltip("Name displayed for the Dismember ability")] private string dismemberName = "Head Space";

    [Header("Text References")]
    [SerializeField, Tooltip("Text component for displaying the ability name (title)")] private TextMeshProUGUI abilityNameText;
    [SerializeField, Tooltip("Additional text component to show/hide with the selector")] private TextMeshProUGUI ZText; // Nuevo campo para el texto adicional
    
    [Header("References")]
    [SerializeField, Tooltip("Reference to the PlayerMovement script")] private PlayerMovement playerMovement;
    
    [Header("Audio")]
    [SerializeField, Tooltip("Sound played when moving the cursor")] private AudioClip moveSound;

    // Variables para el cursor
    private SpriteRenderer cursorRenderer;
    private AudioSource audioSource;
    private float spriteTimer = 0f; // Temporizador para cambio de sprite
    private float cooldownTimer = 0f; // Temporizador para cooldown de movimiento
    private bool isInCooldown = false; // Indica si estamos en cooldown de movimiento
    private bool isAlternating = false; // Indica si estamos alternando sprites

    private enum SelectedHab { Gravity, Dismember, Shooting }
    private SelectedHab currentSelection = SelectedHab.Gravity;
    private const float LOCKED_SCALE = 0.62f;

    void Start()
    {
        // Validaciones iniciales
        if (playerMovement == null || cursor == null || abilityNameText == null || 
            originalCursorSprite == null || selectionCursorSprite == null)
        {
            Debug.LogError("Faltan referencias en HabSelector.");
            return;
        }

        cursorRenderer = cursor.GetComponent<SpriteRenderer>();
        if (cursorRenderer == null)
        {
            Debug.LogError("El cursor no tiene SpriteRenderer.");
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Validar el texto adicional
        if (ZText == null)
        {
            Debug.LogWarning("Additional Text no asignado en HabSelector. No se mostrará ningún texto adicional.");
        }

        UpdateUI(playerMovement.canChangeGravity, playerMovement.canShoot, playerMovement.canDismember);
        UpdateCursorPosition();
        abilityNameText.gameObject.SetActive(false); // Ocultar texto del título al inicio
        if (ZText != null) ZText.gameObject.SetActive(false); // Ocultar texto adicional al inicio
        cursorRenderer.sprite = originalCursorSprite; // Sprite inicial
    }

    void Update()
    {
        if (playerMovement != null && playerMovement.IsXPressed() && playerMovement.isSelectingMode)
        {
            // Mostrar ambos textos al entrar en modo selección
            if (!abilityNameText.gameObject.activeSelf)
            {
                abilityNameText.gameObject.SetActive(true);
                if (ZText != null) ZText.gameObject.SetActive(true);
                UpdateAbilityNameText();
            }

            // Iniciar alternancia si no está activa
            if (!isAlternating)
            {
                isAlternating = true;
                spriteTimer = 0f;
            }

            // Manejar temporizador de cooldown si está activo
            if (isInCooldown)
            {
                cooldownTimer += Time.unscaledDeltaTime;
                if (cooldownTimer >= moveCooldown)
                {
                    isInCooldown = false;
                    cooldownTimer = 0f;
                }
            }

            // Alternar sprites cada spriteChangeInterval
            if (isAlternating)
            {
                spriteTimer += Time.unscaledDeltaTime;
                if (spriteTimer >= spriteChangeInterval)
                {
                    cursorRenderer.sprite = cursorRenderer.sprite == originalCursorSprite ? selectionCursorSprite : originalCursorSprite;
                    spriteTimer = 0f;
                }
            }

            // Permitir movimiento solo si no está en cooldown
            if (!isInCooldown)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) MoveCursorRight();
                else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveCursorLeft();
                else if (Input.GetKeyDown(KeyCode.UpArrow)) MoveCursorUp();
                else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveCursorDown();
            }

            // Activar habilidad con Z
            if (Input.GetKeyDown(KeyCode.Z))
            {
                switch (currentSelection)
                {
                    case SelectedHab.Gravity:
                        if (playerMovement.canChangeGravity)
                        {
                            playerMovement.SetShootingMode(false);
                            playerMovement.ExitSelectingMode();
                            abilityNameText.gameObject.SetActive(false);
                            if (ZText != null) ZText.gameObject.SetActive(false);
                        }
                        break;
                    case SelectedHab.Shooting:
                        if (playerMovement.canShoot)
                        {
                            playerMovement.SetShootingMode(true);
                            playerMovement.ExitSelectingMode();
                            abilityNameText.gameObject.SetActive(false);
                            if (ZText != null) ZText.gameObject.SetActive(false);
                        }
                        break;
                    case SelectedHab.Dismember:
                        if (playerMovement.canDismember)
                        {
                            playerMovement.DismemberHead();
                            playerMovement.ExitSelectingMode();
                            abilityNameText.gameObject.SetActive(false);
                            if (ZText != null) ZText.gameObject.SetActive(false);
                        }
                        break;
                }
            }
        }
        else
        {
            // Salir del modo selección: reiniciar estados
            abilityNameText.gameObject.SetActive(false);
            if (ZText != null) ZText.gameObject.SetActive(false);
            isInCooldown = false;
            isAlternating = false;
            cursorRenderer.sprite = originalCursorSprite;
            spriteTimer = 0f;
            cooldownTimer = 0f;
        }
    }

    private void MoveCursorRight()
    {
        switch (currentSelection)
        {
            case SelectedHab.Gravity:
                if (playerMovement.canDismember) currentSelection = SelectedHab.Dismember;
                else return;
                break;
            case SelectedHab.Shooting:
                if (playerMovement.canDismember) currentSelection = SelectedHab.Dismember;
                else return;
                break;
            case SelectedHab.Dismember:
                return;
        }
        OnCursorMoved();
    }

    private void MoveCursorLeft()
    {
        switch (currentSelection)
        {
            case SelectedHab.Gravity:
                if (playerMovement.canShoot) currentSelection = SelectedHab.Shooting;
                else return;
                break;
            case SelectedHab.Dismember:
                if (playerMovement.canShoot) currentSelection = SelectedHab.Shooting;
                else return;
                break;
            case SelectedHab.Shooting:
                return;
        }
        OnCursorMoved();
    }

    private void MoveCursorUp()
    {
        switch (currentSelection)
        {
            case SelectedHab.Dismember:
                if (playerMovement.canChangeGravity) currentSelection = SelectedHab.Gravity;
                else return;
                break;
            case SelectedHab.Shooting:
                if (playerMovement.canChangeGravity) currentSelection = SelectedHab.Gravity;
                else return;
                break;
            case SelectedHab.Gravity:
                return;
        }
        OnCursorMoved();
    }

    private void MoveCursorDown()
    {
        return; // No hay movimiento hacia abajo
    }

    private void OnCursorMoved()
    {
        UpdateCursorPosition();
        UpdateAbilityNameText();
        isInCooldown = true; // Activar cooldown de movimiento
        cooldownTimer = 0f;
        if (moveSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(moveSound); // Reproducir sonido
        }
        // Reiniciar el temporizador de sprite para sincronizar la alternancia
        spriteTimer = 0f;
        cursorRenderer.sprite = selectionCursorSprite; // Iniciar con sprite de selección
    }

    private void UpdateCursorPosition()
    {
        if (cursor == null) return;
        switch (currentSelection)
        {
            case SelectedHab.Gravity:
                cursor.transform.position = gravityObject.transform.position;
                break;
            case SelectedHab.Dismember:
                cursor.transform.position = dismemberObject.transform.position;
                break;
            case SelectedHab.Shooting:
                cursor.transform.position = shootingObject.transform.position;
                break;
        }
    }

    public string GetSelectedHab()
    {
        switch (currentSelection)
        {
            case SelectedHab.Gravity: return "Gravity";
            case SelectedHab.Dismember: return "Dismember";
            case SelectedHab.Shooting: return "Shooting";
            default: return "Gravity";
        }
    }

    public void UpdateUI(bool gravityUnlocked, bool shootUnlocked, bool dismemberUnlocked)
    {
        if (gravityObject != null)
        {
            SpriteRenderer gravityRenderer = gravityObject.GetComponent<SpriteRenderer>();
            if (gravityRenderer != null)
            {
                gravityRenderer.sprite = gravityUnlocked ? gravitySprite : lockedSprite;
                if (!gravityUnlocked) gravityObject.transform.localScale = new Vector3(LOCKED_SCALE, LOCKED_SCALE, LOCKED_SCALE);
            }
        }
        if (shootingObject != null)
        {
            SpriteRenderer shootRenderer = shootingObject.GetComponent<SpriteRenderer>();
            if (shootRenderer != null)
            {
                shootRenderer.sprite = shootUnlocked ? shootingSprite : lockedSprite;
                if (!shootUnlocked) shootingObject.transform.localScale = new Vector3(LOCKED_SCALE, LOCKED_SCALE, LOCKED_SCALE);
            }
        }
        if (dismemberObject != null)
        {
            SpriteRenderer dismemberRenderer = dismemberObject.GetComponent<SpriteRenderer>();
            if (dismemberRenderer != null)
            {
                dismemberRenderer.sprite = dismemberUnlocked ? dismemberSprite : lockedSprite;
                if (!dismemberUnlocked) dismemberObject.transform.localScale = new Vector3(LOCKED_SCALE, LOCKED_SCALE, LOCKED_SCALE);
            }
        }
    }

    private void UpdateAbilityNameText()
    {
        if (abilityNameText == null) return;
        switch (currentSelection)
        {
            case SelectedHab.Gravity:
                abilityNameText.text = gravityName;
                break;
            case SelectedHab.Shooting:
                abilityNameText.text = shootingName;
                break;
            case SelectedHab.Dismember:
                abilityNameText.text = dismemberName;
                break;
        }
    }
}