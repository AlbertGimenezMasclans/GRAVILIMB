using UnityEngine;
using UnityEngine.UI;

public class HabSelector : MonoBehaviour
{
    [SerializeField] private GameObject gravityObject;
    [SerializeField] private GameObject dismemberObject;
    [SerializeField] private GameObject shootingObject;
    [SerializeField] private GameObject cursor;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Sprite lockedSprite; // Asigna "Locked_Logo_0" en el Inspector
    [SerializeField] private Sprite gravitySprite; // Sprite desbloqueado para gravedad
    [SerializeField] private Sprite dismemberSprite; // Sprite desbloqueado para desmembrar
    [SerializeField] private Sprite shootingSprite; // Sprite desbloqueado para disparar

    private enum SelectedHab { Gravity, Dismember, Shooting }
    private SelectedHab currentSelection = SelectedHab.Gravity;
    private const float LOCKED_SCALE = 0.647712f; // Escala fija para el candado

    void Start()
    {
        if (playerMovement == null) { Debug.LogError("PlayerMovement no asignado."); return; }
        if (gravityObject == null || dismemberObject == null || shootingObject == null) { Debug.LogError("GameObjects no asignados."); return; }
        if (cursor == null) { Debug.LogError("Cursor no asignado."); return; }
        if (lockedSprite == null) { Debug.LogError("LockedSprite no asignado."); return; }

        UpdateUI(playerMovement.canChangeGravity, playerMovement.canShoot, playerMovement.canDismember);
        UpdateCursorPosition();
    }

    void Update()
    {
        if (playerMovement != null && playerMovement.IsXPressed() && playerMovement.isSelectingMode)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) MoveCursorRight();
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveCursorLeft();
            else if (Input.GetKeyDown(KeyCode.UpArrow)) MoveCursorUp();
            else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveCursorDown();
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                // Activar habilidad solo si está desbloqueada
                switch (currentSelection)
                {
                    case SelectedHab.Gravity:
                        if (playerMovement.canChangeGravity)
                        {
                            playerMovement.SetShootingMode(false); // Modo cambio de gravedad
                            playerMovement.ExitSelectingMode();
                        }
                        break;
                    case SelectedHab.Shooting:
                        if (playerMovement.canShoot)
                        {
                            playerMovement.SetShootingMode(true); // Modo disparo
                            playerMovement.ExitSelectingMode();
                        }
                        break;
                    case SelectedHab.Dismember:
                        if (playerMovement.canDismember)
                        {
                            playerMovement.DismemberHead(); // Ejecutar desmembramiento inmediato
                            playerMovement.ExitSelectingMode();
                        }
                        break;
                }
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
                gravityRenderer.sprite = gravityUnlocked ? gravitySprite : lockedSprite;
                if (!gravityUnlocked)
                    gravityObject.transform.localScale = new Vector3(LOCKED_SCALE, LOCKED_SCALE, LOCKED_SCALE);
            }
        }
        if (shootingObject != null)
        {
            SpriteRenderer shootRenderer = shootingObject.GetComponent<SpriteRenderer>();
            if (shootRenderer != null)
            {
                shootRenderer.sprite = shootUnlocked ? shootingSprite : lockedSprite;
                if (!shootUnlocked)
                    shootingObject.transform.localScale = new Vector3(LOCKED_SCALE, LOCKED_SCALE, LOCKED_SCALE);
            }
        }
        if (dismemberObject != null)
        {
            SpriteRenderer dismemberRenderer = dismemberObject.GetComponent<SpriteRenderer>();
            if (dismemberRenderer != null)
            {
                dismemberRenderer.sprite = dismemberUnlocked ? dismemberSprite : lockedSprite;
                if (!dismemberUnlocked)
                    dismemberObject.transform.localScale = new Vector3(LOCKED_SCALE, LOCKED_SCALE, LOCKED_SCALE);
            }
        }
    }
}