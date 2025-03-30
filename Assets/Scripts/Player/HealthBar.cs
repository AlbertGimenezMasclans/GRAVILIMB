using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [Tooltip("The transform of the bar that will scale and move (the fill)")]
    public Transform barFill; // El sprite que representa la barra de vida (HealthBar_Fill)

    [Header("Max Health Position and Size for Bar Fill")]
    [Tooltip("Local position of the Bar Fill when health is at maximum")]
    public Vector2 maxHealthPosition = new Vector2(-1f, 0f); // Posición inicial de HealthBar_Fill
    [Tooltip("Length of the Bar Fill when health is at maximum")]
    public float maxHealthLength = 2f;
    [Tooltip("Height of the Bar Fill when health is at maximum")]
    public float maxHealthHeight = 0.2f;

    [Header("Zero Health Position and Size for Bar Fill")]
    [Tooltip("Local position of the Bar Fill when health is at 0 (usually only the X changes to shrink left)")]
    public Vector2 zeroHealthPosition = new Vector2(-2f, 0f); // Posición de HealthBar_Fill cuando la vida es 0
    [Tooltip("Length of the Bar Fill when health is at 0 (usually 0)")]
    public float zeroHealthLength = 0f;
    [Tooltip("Height of the Bar Fill when health is at 0 (usually same as max)")]
    public float zeroHealthHeight = 0.2f;

    private float maxHealth; // Vida máxima para calcular el porcentaje
    private float lengthRange; // Rango de longitud entre maxHealthLength y zeroHealthLength
    private float heightRange; // Rango de altura entre maxHealthHeight y zeroHealthHeight
    private Vector2 positionRange; // Rango de posición entre maxHealthPosition y zeroHealthPosition

    void Start()
    {
        // Calcular los rangos para la interpolación
        lengthRange = maxHealthLength - zeroHealthLength;
        heightRange = maxHealthHeight - zeroHealthHeight;
        positionRange = maxHealthPosition - zeroHealthPosition;

        // Asegurarse de que barFill esté asignado
        if (barFill == null)
        {
            Debug.LogError("Bar Fill no está asignado en HealthBar.", this);
        }
        else
        {
            // Establecer la posición y tamaño inicial de HealthBar_Fill
            barFill.localPosition = maxHealthPosition;
            barFill.localScale = new Vector3(maxHealthLength, maxHealthHeight, 1f);
        }
    }

    public void SetMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
        // Asegurarse de que la barra esté al máximo al inicio
        UpdateHealth(maxHealth);
    }

    public void UpdateHealth(float currentHealth)
    {
        if (barFill == null || maxHealth <= 0f) return;

        // Calcular el porcentaje de vida
        float healthPercentage = currentHealth / maxHealth;

        // Calcular la nueva longitud, altura y posición de HealthBar_Fill basadas en el porcentaje
        float newLength = zeroHealthLength + (lengthRange * healthPercentage);
        float newHeight = zeroHealthHeight + (heightRange * healthPercentage);
        Vector2 newPosition = zeroHealthPosition + (positionRange * healthPercentage);

        // Actualizar la posición de HealthBar_Fill
        barFill.localPosition = newPosition;

        // Escalar HealthBar_Fill (encogiéndose hacia la izquierda)
        barFill.localScale = new Vector3(newLength, newHeight, 1f);
    }
}