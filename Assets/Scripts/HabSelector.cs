using UnityEngine;

public class HabSelector : MonoBehaviour
{
    [SerializeField] private GameObject gravityObject;    // GameObject para "Gravity"
    [SerializeField] private GameObject dismemberObject;  // GameObject para "Dismember"
    [SerializeField] private GameObject shootingObject;   // GameObject para "Shooting"
    [SerializeField] private GameObject cursor;           // GameObject para el cursor
    [SerializeField] private PlayerMovement playerMovement; // Referencia directa al PlayerMovement

    private enum SelectedHab { Gravity, Dismember, Shooting }
    private SelectedHab currentSelection = SelectedHab.Gravity; // Comienza en Gravity por defecto

    void Start()
    {
        // Asegurarse de que los GameObjects y el PlayerMovement estén asignados
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement no está asignado en el Inspector.");
            return;
        }
        if (gravityObject == null || dismemberObject == null || shootingObject == null)
        {
            Debug.LogError("Uno o más GameObjects (Gravity, Dismember, Shooting) no están asignados en el Inspector.");
            return;
        }
        if (cursor == null)
        {
            Debug.LogError("El cursor no está asignado en el Inspector.");
            return;
        }

        // Posicionar el cursor en la posición inicial (Gravity)
        UpdateCursorPosition();
    }

    void Update()
    {
        // Solo procesar entrada si X está pulsada y estamos en modo de selección
        if (playerMovement != null && playerMovement.IsXPressed() && playerMovement.isSelectingMode)
        {
            // Mover el cursor con las flechas
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                MoveCursorRight();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                MoveCursorLeft();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveCursorUp();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveCursorDown();
            }
        }
    }

    private void MoveCursorRight()
    {
        switch (currentSelection)
        {
            case SelectedHab.Gravity:
                currentSelection = SelectedHab.Dismember;
                break;
            case SelectedHab.Shooting:
                currentSelection = SelectedHab.Dismember;
                break;
            case SelectedHab.Dismember:
                // No hace nada (derecha desde Dismember no lleva a ninguna)
                return;
        }
        UpdateCursorPosition();
    }

    private void MoveCursorLeft()
    {
        switch (currentSelection)
        {
            case SelectedHab.Gravity:
                currentSelection = SelectedHab.Shooting;
                break;
            case SelectedHab.Dismember:
                currentSelection = SelectedHab.Shooting;
                break;
            case SelectedHab.Shooting:
                // No hace nada (izquierda desde Shooting no lleva a ninguna)
                return;
        }
        UpdateCursorPosition();
    }

    private void MoveCursorUp()
    {
        switch (currentSelection)
        {
            case SelectedHab.Dismember:
                currentSelection = SelectedHab.Gravity;
                break;
            case SelectedHab.Shooting:
                currentSelection = SelectedHab.Gravity;
                break;
            case SelectedHab.Gravity:
                // No hace nada (arriba desde Gravity no lleva a ninguna)
                return;
        }
        UpdateCursorPosition();
    }

    private void MoveCursorDown()
    {
        // Ninguna transición permite bajar desde cualquier posición
        return;
    }

    private void UpdateCursorPosition()
    {
        if (cursor == null) return;

        // Mover el cursor a la posición del GameObject seleccionado
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

    // Método público para obtener la habilidad seleccionada
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