using UnityEngine;

public class KredsBag : MonoBehaviour
{
    [SerializeField] private int bagValue = 5000;
    [SerializeField] private float countDuration = 1.25f;
    
    // Tags que pueden recolectar la bolsa
    private string[] validTags = { "Player", "PlayerHead" };

    private bool IsValidCollector(GameObject obj)
    {
        foreach (string tag in validTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsValidCollector(collision.gameObject))
        {
            KredsManager.Instance.AddTokens(bagValue, countDuration);
            Destroy(gameObject);
        }
    }
}