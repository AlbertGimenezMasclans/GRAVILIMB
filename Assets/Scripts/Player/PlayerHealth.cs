using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of the player")]
    public float maxHealth = 20f;
    [Tooltip("Current health of the player")]
    public float currentHealth;

    [Header("Visibility Settings")]
    [Tooltip("GameObjects whose SpriteRenderers will be made visible/invisible and whose rotation will be fixed")]
    public GameObject[] objectsToHide; // Array de GameObjects a hacer visibles/invisibles y fijar rotación

    [Header("Position Offset")]
    [Tooltip("Offset from the player's position where the objects should be positioned")]
    public Vector2 positionOffset = new Vector2(0f, 2f); // Subimos la barra de vida a 2 unidades por encima del jugador

    private HealthBar healthBar; // Referencia al script HealthBar
    private PlayerDeath playerDeath; // Referencia al script PlayerDeath
    private Coroutine hideHealthBarCoroutine; // Corutina para ocultar los objetos
    private SpriteRenderer[] spriteRenderers; // Array de SpriteRenderers de los objetos
    private Transform[] objectTransforms; // Array de Transforms de los objetos para fijar rotación y posición
    private Transform playerTransform; // Transform del jugador

    void Start()
    {
        // Inicializar la vida al máximo
        currentHealth = maxHealth;

        // Obtener el Transform del jugador
        playerTransform = transform;

        // Obtener los SpriteRenderers y Transforms de los objetos a ocultar
        spriteRenderers = new SpriteRenderer[objectsToHide.Length];
        objectTransforms = new Transform[objectsToHide.Length];
        for (int i = 0; i < objectsToHide.Length; i++)
        {
            if (objectsToHide[i] != null)
            {
                spriteRenderers[i] = objectsToHide[i].GetComponent<SpriteRenderer>();
                if (spriteRenderers[i] == null)
                {
                    Debug.LogError($"SpriteRenderer no encontrado en {objectsToHide[i].name}.", this);
                }
                objectTransforms[i] = objectsToHide[i].transform;

                // Asegurarse de que el objeto no sea hijo del jugador
                if (objectTransforms[i].parent == playerTransform)
                {
                    objectTransforms[i].SetParent(null);
                }
            }
        }

        // Obtener la referencia al script HealthBar
        healthBar = FindObjectOfType<HealthBar>();
        if (healthBar == null)
        {
            Debug.LogError("HealthBar no encontrado en la escena.", this);
        }
        else
        {
            // Inicializar la barra de vida
            healthBar.SetMaxHealth(maxHealth);
            healthBar.UpdateHealth(currentHealth);
            // Hacer los objetos invisibles al inicio
            HideObjects();
        }

        // Obtener la referencia al script PlayerDeath
        playerDeath = GetComponent<PlayerDeath>();
        if (playerDeath == null)
        {
            Debug.LogError("PlayerDeath no encontrado en el jugador.", this);
        }
    }

    void Update()
    {
        // Perder vida al presionar la tecla "I" (para probar)
        if (Input.GetKeyDown(KeyCode.I))
        {
            TakeDamage(5f); // Puedes ajustar la cantidad de daño
        }

        // Actualizar la posición y rotación de todos los objetos en objectsToHide
        foreach (Transform objTransform in objectTransforms)
        {
            if (objTransform != null)
            {
                // Actualizar la posición para que siga al jugador con el offset
                Vector3 playerPosition = playerTransform.position;
                objTransform.position = new Vector3(
                    playerPosition.x + positionOffset.x,
                    playerPosition.y + positionOffset.y,
                    objTransform.position.z // Mantener la Z original del objeto
                );

                // Fijar la rotación en el espacio global
                objTransform.rotation = Quaternion.identity; // Rotación fija a (0, 0, 0)
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Si colisiona con una DeathZone, reducir la vida a 0
        if (collision.CompareTag("DeathZone"))
        {
            currentHealth = 0f;
            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth);
                // Hacer los objetos visibles al colisionar con DeathZone
                ShowObjects();
            }
            Die();
        }
    }

    public void TakeDamage(float damage)
    {
        // Reducir la vida
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth); // Asegurarse de que no baje de 0 ni exceda el máximo

        // Actualizar la barra de vida y hacer los objetos visibles
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
            ShowObjects();
        }

        // Comprobar si el jugador ha muerto
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // Si el jugador ya está muerto, no hacer nada
        if (playerDeath != null && playerDeath.IsDead()) return;

        // Llamar a la lógica de muerte de PlayerDeath
        if (playerDeath != null)
        {
            StartCoroutine(playerDeath.RespawnCoroutine());
        }
        else
        {
            Debug.Log("¡El jugador ha muerto! (PlayerDeath no encontrado)");
        }
    }

    private IEnumerator HideObjectsAfterDelay()
    {
        // Esperar 1 segundo
        yield return new WaitForSeconds(1f);
        // Hacer los objetos invisibles si el jugador no está muerto
        if (playerDeath == null || !playerDeath.IsDead())
        {
            HideObjects();
        }
    }

    // Método público para hacer los objetos visibles (usado al perder vida)
    public void ShowObjects()
    {
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
        // Reiniciar la corutina para ocultar los objetos
        if (hideHealthBarCoroutine != null)
        {
            StopCoroutine(hideHealthBarCoroutine);
        }
        hideHealthBarCoroutine = StartCoroutine(HideObjectsAfterDelay());
    }

    // Método privado para hacer los objetos invisibles
    private void HideObjects()
    {
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    // Método público para forzar la invisibilidad de los objetos (usado por PlayerDeath)
    public void ForceHideObjects()
    {
        // Detener la corutina de ocultar si está activa
        if (hideHealthBarCoroutine != null)
        {
            StopCoroutine(hideHealthBarCoroutine);
        }
        // Hacer los objetos invisibles inmediatamente
        HideObjects();
    }
}