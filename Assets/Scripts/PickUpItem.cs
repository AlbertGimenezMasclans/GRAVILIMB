using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public string abilityToUnlock; // "gravity", "shoot", "dismember"

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.UnlockAbility(abilityToUnlock);
            }
        }
    }
}