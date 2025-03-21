using UnityEngine;

public class KredCollect : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Destruir al terminar la animación
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;
            if (animationLength > 0f)
            {
                Destroy(gameObject, animationLength);
                Debug.Log($"KredCollect creado. Duración de la animación: {animationLength}s. Se destruirá después.");
            }
            else
            {
                Debug.LogWarning("La animación tiene duración 0 o no está configurada correctamente. Destrucción en 1s.");
                Destroy(gameObject, 1f);
            }
        }
        else
        {
            Debug.LogWarning("KredCollect no tiene Animator o AnimatorController asignado. Destrucción en 1s.");
            Destroy(gameObject, 1f);
        }
    }
}