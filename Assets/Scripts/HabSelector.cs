using UnityEngine;
using TMPro;

public class HabSelector : MonoBehaviour
{
    [SerializeField] private GameObject gravityObject;
    [SerializeField] private GameObject dismemberObject;
    [SerializeField] private GameObject shootingObject;
    [SerializeField] private GameObject cursor;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Sprite lockedSprite; // Sprite del candado, asignado en el Inspector del HabSelector
    [SerializeField] private Sprite gravitySprite; // Sprite desbloqueado para gravedad
    [SerializeField] private Sprite dismemberSprite; // Sprite desbloqueado para desmembrar
    [SerializeField] private Sprite shootingSprite; // Sprite desbloqueado para disparar

    // Referencia al TextMeshProUGUI y mensajes personalizables desde el Inspector
    [SerializeField] private TextMeshProUGUI abilityNameText; // Asigna en el Inspector
    [SerializeField] private string gravityName = "Zer0 Bootz"; // Nombre para Gravity
    [SerializeField] private string shootingName = "Gun"; // Nombre para Shooting
    [SerializeField] private string dismemberName = "Head Space"; // Nombre para Dismember

    private enum SelectedHab { Gravity, Dismember, Shooting }
    private SelectedHab currentSelection = SelectedHab.Gravity;
    private const float LOCKED_SCALE = 0.647712f; // Escala fija para el candado

    void Start()
    {
        if (playerMovement == null) { Debug.LogError("PlayerMovement no asignado."); return; }
        if (gravityObject == null || dismemberObject == null || shootingObject == null) { Debug.LogError("GameObjects no asignados."); return; }
        if (cursor == null) { Debug.LogError("Cursor no asignado."); return; }
        if (lockedSprite == null) { Debug.LogError("LockedSprite no asignado en HabSelector."); return; }
        if (abilityNameText == null) { Debug.LogError("TextMeshProUGUI no asignado en HabSelector."); return; }

        UpdateUI(playerMovement.canChangeGravity, playerMovement.canShoot, playerMovement.canDismember);
        UpdateCursorPosition();

        // Inicialmente ocultar el texto
        abilityNameText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerMovement != null && playerMovement.IsXPressed() && playerMovement.isSelectingMode)
        {
            // Mostrar el texto mientras X está presionada y estamos en modo selección
            if (abilityNameText != null && !abilityNameText.gameObject.activeSelf)
            {
                abilityNameText.gameObject.SetActive(true);
                UpdateAbilityNameText(); // Mostrar el nombre inicial
            }

            if (Input.GetKeyDown(KeyCode.RightArrow)) MoveCursorRight();
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveCursorLeft();
            else if (Input.GetKeyDown(KeyCode.UpArrow)) MoveCursorUp();
            else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveCursorDown();
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                // Activar habilidad solo si está desbloqueada y ocultar el texto después
                switch (currentSelection)
                {
                    case SelectedHab.Gravity:
                        if (playerMovement.canChangeGravity)
                        {
                            playerMovement.SetShootingMode(false);
                            playerMovement.ExitSelectingMode();
                            if (abilityNameText != null) abilityNameText.gameObject.SetActive(false); // Ocultar texto
                        }
                        break;
                    case SelectedHab.Shooting:
                        if (playerMovement.canShoot)
                        {
                            playerMovement.SetShootingMode(true);
                            playerMovement.ExitSelectingMode();
                            if (abilityNameText != null) abilityNameText.gameObject.SetActive(false); // Ocultar texto
                        }
                        break;
                    case SelectedHab.Dismember:
                        if (playerMovement.canDismember)
                        {
                            playerMovement.DismemberHead();
                            playerMovement.ExitSelectingMode();
                            if (abilityNameText != null) abilityNameText.gameObject.SetActive(false); // Ocultar texto
                        }
                        break;
                }
            }
        }
        else
        {
            // Ocultar el texto cuando X no está presionada o no estamos en modo selección
            if (abilityNameText != null && abilityNameText.gameObject.activeSelf)
            {
                abilityNameText.gameObject.SetActive(false);
            }
        }
    }

    private void MoveCursorRight()
    {
        switch (currentSelection)
        {
            case SelectedHab.Gravity:
                if (playerMovement.canDismember) currentSelection = SelectedHab.Dismember;
                break;
            case SelectedHab.Shooting:
                if (playerMovement.canDismember) currentSelection = SelectedHab.Dismember;
                break;
            case SelectedHab.Dismember:
                return;
        }
        UpdateCursorPosition();
        UpdateAbilityNameText(); // Actualizar texto al mover el cursor
    }

    private void MoveCursorLeft()
    {
        switch (currentSelection)
        {
            case SelectedHab.Gravity:
                if (playerMovement.canShoot) currentSelection = SelectedHab.Shooting;
                break;
            case SelectedHab.Dismember:
                if (playerMovement.canShoot) currentSelection = SelectedHab.Shooting;
                break;
            case SelectedHab.Shooting:
                return;
        }
        UpdateCursorPosition();
        UpdateAbilityNameText(); // Actualizar texto al mover el cursor
    }

    private void MoveCursorUp()
    {
        switch (currentSelection)
        {
            case SelectedHab.Dismember:
                if (playerMovement.canChangeGravity) currentSelection = SelectedHab.Gravity;
                break;
            case SelectedHab.Shooting:
                if (playerMovement.canChangeGravity) currentSelection = SelectedHab.Gravity;
                break;
            case SelectedHab.Gravity:
                return;
        }
        UpdateCursorPosition();
        UpdateAbilityNameText(); // Actualizar texto al mover el cursor
    }

    private void MoveCursorDown()
    {
        return; // No hay movimiento hacia abajo en este diseño
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
                gravityRenderer.sprite = gravityUnlocked ? gravitySprite : lockedSprite; // Usa lockedSprite desde el Inspector
                if (!gravityUnlocked)
                    gravityObject.transform.localScale = new Vector3(LOCKED_SCALE, LOCKED_SCALE, LOCKED_SCALE);
            }
        }
        if (shootingObject != null)
        {
            SpriteRenderer shootRenderer = shootingObject.GetComponent<SpriteRenderer>();
            if (shootRenderer != null)
            {
                shootRenderer.sprite = shootUnlocked ? shootingSprite : lockedSprite; // Usa lockedSprite desde el Inspector
                if (!shootUnlocked)
                    shootingObject.transform.localScale = new Vector3(LOCKED_SCALE, LOCKED_SCALE, LOCKED_SCALE);
            }
        }
        if (dismemberObject != null)
        {
            SpriteRenderer dismemberRenderer = dismemberObject.GetComponent<SpriteRenderer>();
            if (dismemberRenderer != null)
            {
                dismemberRenderer.sprite = dismemberUnlocked ? dismemberSprite : lockedSprite; // Usa lockedSprite desde el Inspector
                if (!dismemberUnlocked)
                    dismemberObject.transform.localScale = new Vector3(LOCKED_SCALE, LOCKED_SCALE, LOCKED_SCALE);
            }
        }
    }

    private void UpdateAbilityNameText()
    {
        if (abilityNameText == null) return;

        switch (currentSelection)
        {
            case SelectedHab.Gravity:
                abilityNameText.text = gravityName; // "Zer0 Bootz" por defecto
                break;
            case SelectedHab.Shooting:
                abilityNameText.text = shootingName; // "Gun" por defecto
                break;
            case SelectedHab.Dismember:
                abilityNameText.text = dismemberName; // "Head Space" por defecto
                break;
        }
    }
}