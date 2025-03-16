using UnityEngine;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance; // Singleton para acceso global
    [SerializeField] private GameObject projectilePrefab; // Prefab del proyectil
    private const int poolSize = 6; // Tamaño fijo del pool (6 proyectiles)
    private List<GameObject> projectilePool; // Lista de proyectiles en el pool
    private int activeProjectiles = 0; // Contador de proyectiles activos
    private Queue<GameObject> activeProjectilesQueue; // Cola para rastrear el orden de los proyectiles activos
    private bool isFull = false; // Indica si el pool está lleno (6 activos)

    void Awake()
    {
        // Configurar el Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Inicializar el pool con tamaño fijo
        projectilePool = new List<GameObject>();
        activeProjectilesQueue = new Queue<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile = Instantiate(projectilePrefab);
            projectile.SetActive(false); // Desactivar inicialmente
            projectilePool.Add(projectile);
        }
    }

    public bool CanShoot()
    {
        return activeProjectiles < poolSize; // Permitir disparar si hay menos de 6 activos
    }

    public GameObject GetProjectile()
    {
        if (activeProjectiles >= poolSize)
        {
            return null; // No hay proyectiles disponibles
        }

        // Buscar un proyectil desactivado en el pool
        foreach (GameObject projectile in projectilePool)
        {
            if (!projectile.activeInHierarchy)
            {
                activeProjectiles++; // Incrementar el contador de activos
                activeProjectilesQueue.Enqueue(projectile); // Añadir a la cola para rastrear el orden
                if (activeProjectiles == poolSize)
                {
                    isFull = true; // Marcar como lleno cuando se alcanzan los 6
                }
                return projectile;
            }
        }
        return null; // No hay proyectiles disponibles (pool lleno)
    }

    public void ReturnProjectile(GameObject projectile)
    {
        // Devolver el proyectil al pool desactivándolo
        projectile.SetActive(false);
        activeProjectiles--; // Decrementar el contador de activos

        // Si el pool estaba lleno y el sexto proyectil (el más reciente) se devuelve, liberar
        if (isFull && activeProjectilesQueue.Count > 0 && projectile == activeProjectilesQueue.Peek())
        {
            isFull = false; // Liberar el pool para nuevos disparos
            activeProjectilesQueue.Dequeue(); // Quitar el proyectil más antiguo de la cola
        }
        else if (activeProjectilesQueue.Contains(projectile))
        {
            // Crear una nueva cola temporal para mantener el orden
            Queue<GameObject> tempQueue = new Queue<GameObject>();
            while (activeProjectilesQueue.Count > 0)
            {
                GameObject queuedProjectile = activeProjectilesQueue.Dequeue();
                if (queuedProjectile != projectile)
                {
                    tempQueue.Enqueue(queuedProjectile);
                }
            }
            activeProjectilesQueue = tempQueue;
        }
    }
}