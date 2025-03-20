using UnityEngine;
using TMPro;

public class HabSelector : MonoBehaviour
{
    [SerializeField] private GameObject gravityObject;
    [SerializeField] private GameObject dismemberObject;
    [SerializeField] private GameObject shootingObject;
    [SerializeField] private GameObject cursor;
    [SerializeField] private PlayerMovement playerMovement;

    // Sprites para habilidades
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite gravitySprite;
    [SerializeField] private Sprite dismemberSprite;
    [SerializeField] private Sprite shootingSprite;

    // Texto y nombres de habilidades
    [SerializeField] private TextMeshProUGUI abilityNameText;
    [SerializeField] private string gravityName = "Zer0 Bootz";
    [SerializeField] private string shootingName = "Gun";
    [SerializeField] private string dismemberName = "Head Space";

    // Variables para el cursor
    [SerializeField] private Sprite originalCursorSprite; // Sprite original del cursor
    [SerializeField] private Sprite selectionCursorSprite; // Sprite al mover/seleccionar
    [SerializeField] private float spriteChangeInterval = 0.5f; // Intervalo de cambio de sprite (0.5s)
    [SerializeField] private float moveCooldown = 1.0f; // Cooldown total al moverte (1s)
    [SerializeField] private AudioClip moveSound; // Sonido al moverse

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

        UpdateUI(playerMovement.canChangeGravity, playerMovement.canShoot, playerMovement.canDismember);
        UpdateCursorPosition();
        abilityNameText.gameObject.SetActive(false); // Ocultar texto al inicio
        cursorRenderer.sprite = originalCursorSprite; // Sprite inicial
    }

    void Update()
    {
        if (playerMovement != null && playerMovement.IsXPressed() && playerMovement.isSelectingMode)
        {
            // Mostrar texto al entrar en modo selección
            if (!abilityNameText.gameObject.activeSelf)
            {
                abilityNameText.gameObject.SetActive(true);
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
                        }
                        break;
                    case SelectedHab.Shooting:
                        if (playerMovement.canShoot)
                        {
                            playerMovement.SetShootingMode(true);
                            playerMovement.ExitSelectingMode();
                            abilityNameText.gameObject.SetActive(false);
                        }
                        break;
                    case SelectedHab.Dismember:
                        if (playerMovement.canDismember)
                        {
                            playerMovement.DismemberHead();
                            playerMovement.ExitSelectingMode();
                            abilityNameText.gameObject.SetActive(false);
                        }
                        break;
                }
            }
        }
        else
        {
            // Salir del modo selección: reiniciar estados
            abilityNameText.gameObject.SetActive(false);
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