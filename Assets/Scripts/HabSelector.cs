using UnityEngine;

public class HabSelector : MonoBehaviour
{
    [SerializeField] private GameObject gravityObject;
    [SerializeField] private GameObject dismemberObject;
    [SerializeField] private GameObject shootingObject;
    [SerializeField] private GameObject cursor;
    [SerializeField] private PlayerMovement playerMovement;

    private enum SelectedHab { Gravity, Dismember, Shooting }
    private SelectedHab currentSelection = SelectedHab.Gravity;

    void Start()
    {
        if (playerMovement == null) { Debug.LogError("PlayerMovement no asignado."); return; }
        if (gravityObject == null || dismemberObject == null || shootingObject == null) { Debug.LogError("GameObjects no asignados."); return; }
        if (cursor == null) { Debug.LogError("Cursor no asignado."); return; }

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
                // Cambiar modo y desactivar HabSelector
                switch (currentSelection)
                {
                    case SelectedHab.Gravity:
                        playerMovement.SetShootingMode(false); // Modo cambio de gravedad
                        playerMovement.ExitSelectingMode();
                        break;
                    case SelectedHab.Shooting:
                        playerMovement.SetShootingMode(true); // Modo disparo
                        playerMovement.ExitSelectingMode();
                        break;
                    case SelectedHab.Dismember:
                        playerMovement.DismemberHead(); // Ejecutar desmembramiento inmediato
                        playerMovement.ExitSelectingMode();
                        break;
                }
            }
        }
    }

    private void MoveCursorRight()
    {
        switch (currentSelection)
        {
            case SelectedHab.Gravity: currentSelection = SelectedHab.Dismember; break;
            case SelectedHab.Shooting: currentSelection = SelectedHab.Dismember; break;
            case SelectedHab.Dismember: return;
        }
        UpdateCursorPosition();
    }

    private void MoveCursorLeft()
    {
        switch (currentSelection)
        {
            case SelectedHab.Gravity: currentSelection = SelectedHab.Shooting; break;
            case SelectedHab.Dismember: currentSelection = SelectedHab.Shooting; break;
            case SelectedHab.Shooting: return;
        }
        UpdateCursorPosition();
    }

    private void MoveCursorUp()
    {
        switch (currentSelection)
        {
            case SelectedHab.Dismember: currentSelection = SelectedHab.Gravity; break;
            case SelectedHab.Shooting: currentSelection = SelectedHab.Gravity; break;
            case SelectedHab.Gravity: return;
        }
        UpdateCursorPosition();
    }

    private void MoveCursorDown()
    {
        return;
    }

    private void UpdateCursorPosition()
    {
        if (cursor == null) return;

        switch (currentSelection)
        {
            case SelectedHab.Gravity: cursor.transform.position = gravityObject.transform.position; break;
            case SelectedHab.Dismember: cursor.transform.position = dismemberObject.transform.position; break;
            case SelectedHab.Shooting: cursor.transform.position = shootingObject.transform.position; break;
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
}