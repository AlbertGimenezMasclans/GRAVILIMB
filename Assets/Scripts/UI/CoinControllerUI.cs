using UnityEngine;

public class CoinControllerUI : MonoBehaviour
{
    [SerializeField] private GameObject coinUIPanel;
    [SerializeField] private PlayerMovement playerMovement;

    void Start()
    {
        if (coinUIPanel == null)
        {
            Debug.LogError("CoinUIPanel no está asignado en CoinControllerUI.");
            return;
        }
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement no está asignado en CoinControllerUI.");
            return;
        }

        coinUIPanel.SetActive(true);
    }

    void Update()
    {
        if (playerMovement != null && playerMovement.activeDialogueSystem != null)
        {
            if (playerMovement.activeDialogueSystem is DialogueSystem dialogue && dialogue.IsDialogueActive)
            {
                coinUIPanel.SetActive(false);
            }
            else if (playerMovement.activeDialogueSystem is ItemMessage itemMessage && itemMessage.IsDialogueActive)
            {
                coinUIPanel.SetActive(false);
            }
            else
            {
                coinUIPanel.SetActive(true);
            }
        }
        else
        {
            // No desactivamos aquí manualmente, dejamos que OnItemCollision lo maneje al inicio
        }
    }

    public void OnItemCollision()
    {
        coinUIPanel.SetActive(false); // Desactivar al colisionar con un ítem
    }

    // Nuevo método para controlar el estado del coinUIPanel desde fuera
    public void SetCoinUIPanelActive(bool active)
    {
        coinUIPanel.SetActive(active);
    }
}