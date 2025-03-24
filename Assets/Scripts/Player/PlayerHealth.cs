using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of the player")]
    public float maxHealth = 20f;
    [Tooltip("Current health of the player")]
    public float currentHealth;

    [Header("Visibility Settings")]
    [Tooltip("GameObjects whose SpriteRenderers or TMP_Text will be made visible/invisible and whose rotation will be fixed")]
    public GameObject[] objectsToHide; // Array de GameObjects a hacer visibles/invisibles y fijar rotación

    [Header("Position Offset")]
    [Tooltip("Offset from the player's position where the objects should be positioned")]
    public Vector2 positionOffset = new Vector2(0f, 2f); // Offset para posicionar los objetos respecto al jugador

    [Header("Health Counter Settings")]
    [Tooltip("Vertical offset for the HealthCounter relative to the base position offset")]
    public float healthCounterVerticalOffset = 0.3f; // Offset vertical para el HealthCounter
    [Tooltip("Horizontal offset for the HealthCounter relative to the base position offset")]
    public float healthCounterHorizontalOffset = 0f; // Offset horizontal para el HealthCounter
    [Tooltip("Scale of the HealthCounter (affects the size of the text)")]
    public float healthCounterScale = 0.05f; // Escala del HealthCounter (ajusta para cambiar el tamaño del texto)

    private HealthBar healthBar; // Referencia al script HealthBar
    private PlayerDeath playerDeath; // Referencia al script PlayerDeath
    private Coroutine hideHealthBarCoroutine; // Corutina para ocultar los objetos
    private SpriteRenderer[] spriteRenderers; // Array de SpriteRenderers de los objetos
    private TMP_Text[] textRenderers; // Array de TMP_Text para los contadores de texto
    private Transform[] objectTransforms; // Array de Transforms de los objetos para fijar rotación y posición
    private Transform playerTransform; // Transform del jugador
    private TMP_Text healthCounterText; // Referencia al componente TextMeshPro del contador
    private Transform healthCounterTransform; // Referencia al Transform del HealthCounter

    void Start()
    {
        // Inicializar la vida al máximo
        currentHealth = maxHealth;

        // Obtener el Transform del jugador
        playerTransform = transform;
        if (playerTransform == null)
        {
            UnityEngine.Debug.LogError("playerTransform es null. Asegúrate de que el script esté adjunto a un GameObject.", this);
            return;
        }

        // Inicializar los arrays para SpriteRenderers, TMP_Text y Transforms
        spriteRenderers = new SpriteRenderer[objectsToHide.Length];
        textRenderers = new TMP_Text[objectsToHide.Length];
        objectTransforms = new Transform[objectsToHide.Length];

        for (int i = 0; i < objectsToHide.Length; i++)
        {
            if (objectsToHide[i] != null)
            {
                // Obtener el SpriteRenderer (si existe)
                spriteRenderers[i] = objectsToHide[i].GetComponent<SpriteRenderer>();
                // Obtener el TMP_Text (si existe)
                textRenderers[i] = objectsToHide[i].GetComponent<TMP_Text>();
                // Obtener el Transform
                objectTransforms[i] = objectsToHide[i].transform;

                // Asegurarse de que el objeto no sea hijo del jugador
                if (objectTransforms[i].parent == playerTransform)
                {
                    objectTransforms[i].SetParent(null);
                }

                // Buscar el HealthCounter
                if (objectsToHide[i].name == "HealthCounter")
                {
                    healthCounterText = objectsToHide[i].GetComponent<TMP_Text>();
                    healthCounterTransform = objectsToHide[i].transform;
                    if (healthCounterText == null)
                    {
                        UnityEngine.Debug.LogError($"TMP_Text no encontrado en {objectsToHide[i].name}.", this);
                    }
                    if (healthCounterTransform == null)
                    {
                        UnityEngine.Debug.LogError($"Transform no encontrado en {objectsToHide[i].name}.", this);
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"HealthCounter detectado: {healthCounterTransform.name}");
                        // Asegurarse de que la escala del HealthCounter sea la especificada
                        healthCounterTransform.localScale = new Vector3(healthCounterScale, healthCounterScale, healthCounterScale);
                    }
                }
            }
        }

        // Obtener la referencia al script HealthBar
        healthBar = FindObjectOfType<HealthBar>();
        if (healthBar == null)
        {
            UnityEngine.Debug.LogError("HealthBar no encontrado en la escena.", this);
        }
        else
        {
            // Inicializar la barra de vida
            healthBar.SetMaxHealth(maxHealth);
            healthBar.UpdateHealth(currentHealth);
            // Actualizar el texto del contador
            UpdateHealthCounterText();
            // Hacer los objetos invisibles al inicio
            HideObjects();
        }

        // Obtener la referencia al script PlayerDeath
        playerDeath = GetComponent<PlayerDeath>();
        if (playerDeath == null)
        {
            UnityEngine.Debug.LogError("PlayerDeath no encontrado en el jugador.", this);
        }
    }

    void Update()
    {
        // Perder vida al presionar la tecla "I" (para probar)
        if (Input.GetKeyDown(KeyCode.I))
        {
            TakeDamage(5f); // Puedes ajustar la cantidad de daño
        }

        // Asegurarse de que playerTransform no sea null
        if (playerTransform == null)
        {
            UnityEngine.Debug.LogError("playerTransform es null en Update().", this);
            return;
        }

        // Obtener la posición del jugador una sola vez
        Vector3 playerPosition = playerTransform.position;

        // Actualizar la posición y rotación de todos los objetos en objectsToHide
        for (int i = 0; i < objectTransforms.Length; i++)
        {
            Transform objTransform = objectTransforms[i];
            if (objTransform != null)
            {
                // Posicionar el HealthCounter respecto al jugador con su propio offset
                if (objectsToHide[i].name == "HealthCounter")
                {
                    if (healthCounterTransform != null)
                    {
                        // Posicionar el HealthCounter respecto al jugador
                        healthCounterTransform.position = new Vector3(
                            playerPosition.x + positionOffset.x + healthCounterHorizontalOffset,
                            playerPosition.y + positionOffset.y + healthCounterVerticalOffset,
                            healthCounterTransform.position.z // Mantener la Z original
                        );
                        // Asegurarse de que la escala del HealthCounter sea constante
                        healthCounterTransform.localScale = new Vector3(healthCounterScale, healthCounterScale, healthCounterScale);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("healthCounterTransform es null. Asegúrate de que HealthCounter esté en objectsToHide y que su nombre sea exactamente 'HealthCounter'.");
                    }
                    continue;
                }

                // Actualizar la posición para los otros objetos (como HealthBar y HealthBar_Fill)
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
                // Actualizar el texto del contador
                UpdateHealthCounterText();
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
            // Actualizar el texto del contador
            UpdateHealthCounterText();
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
        // Habilitar los SpriteRenderers
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
        // Habilitar los TMP_Text
        foreach (TMP_Text text in textRenderers)
        {
            if (text != null)
            {
                text.enabled = true;
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
        // Deshabilitar los SpriteRenderers
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
        // Deshabilitar los TMP_Text
        foreach (TMP_Text text in textRenderers)
        {
            if (text != null)
            {
                text.enabled = false;
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

    // Método para actualizar el texto del contador de vida
    private void UpdateHealthCounterText()
    {
        if (healthCounterText != null)
        {
            healthCounterText.text = $"{(int)currentHealth} / {(int)maxHealth}";
        }
    }
}