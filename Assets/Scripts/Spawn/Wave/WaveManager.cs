using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Controller & Timing")]
    public WaveController waveController;
    [Tooltip("Delay after each wave is cleared")]
    public float timeBetweenWaves = 5f;
    [Tooltip("Seconds between individual spawns")]
    public float spawnInterval = 0.5f;

    [Header("Enemy Prefabs")]
    public GameObject standardPrefab;
    public GameObject armouredPrefab;
    public GameObject dronePrefab;

    [Header("Spawn Zones")]
    public SpawnArea[] spawnAreas;  // your SpawnArea components
    
    [Header("UI Integration")]
    [SerializeField] private PlayerUI playerUI;
    
    // Wave tracking
    private int currentWaveIndex = 0;
    private int totalEnemiesInCurrentWave = 0;
    
    // Events for UI and other systems
    public System.Action<int, int> OnWaveStarted; // waveNumber, totalEnemies
    public System.Action OnWaveCompleted;
    public System.Action OnAllWavesCompleted;
    public System.Action OnEnemyKilled;

    void Start()
    {
        // Get PlayerUI if not assigned
        if (playerUI == null)
        {
            playerUI = FindFirstObjectByType<PlayerUI>();
        }
        
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        int total = waveController.totalWaves;
        
        for (int w = 0; w < total; w++)
        {
            currentWaveIndex = w;
            int waveNumber = w + 1;
            
            Debug.Log($"--- Preparing Wave {waveNumber}/{total} ---");
            
            // Calculate total enemies for this wave
            totalEnemiesInCurrentWave = CalculateTotalEnemiesForWave(w);
            
            // Start wave in controller
            waveController.StartWave(w);
            
            // Notify UI and other systems
            OnWaveStarted?.Invoke(waveNumber, totalEnemiesInCurrentWave);
            if (playerUI != null)
            {
                playerUI.OnWaveStarted(waveNumber, totalEnemiesInCurrentWave);
            }

            // 1) Spawn loop: spawnInterval ticks until caps reached
            while (true)
            {
                var next = waveController.GetNextSpawnType();
                if (!next.HasValue)
                    break;  // all caps reached

                SpawnEnemy(next.Value);
                waveController.OnSpawn(next.Value);
                yield return new WaitForSeconds(spawnInterval);
            }

            Debug.Log($"Wave {waveNumber} spawned. Waiting for all enemies to die...");

            // 2) Wait until no more enemies in scene
            yield return new WaitUntil(() =>
                GameObject.FindGameObjectsWithTag("Enemy").Length == 0
            );

            Debug.Log($"Wave {waveNumber} cleared!");
            
            // Notify wave completed
            OnWaveCompleted?.Invoke();
            if (playerUI != null)
            {
                playerUI.OnWaveCompleted();
            }

            // 3) Delay before next wave (skip after last)
            if (w < total - 1)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        Debug.Log("*** All waves complete! ***");
        OnAllWavesCompleted?.Invoke();
    }

    private int CalculateTotalEnemiesForWave(int waveIndex)
    {
        int totalStandard = waveController.baseStandard + waveController.standardPerWave * waveIndex;
        int totalArmoured = waveController.baseArmoured + waveController.armouredPerWave * waveIndex;
        int totalDrone = waveController.baseDrone + waveController.dronePerWave * waveIndex;
        
        return totalStandard + totalArmoured + totalDrone;
    }

    void SpawnEnemy(EnemyType type)
    {
        if (spawnAreas == null || spawnAreas.Length == 0)
        {
            Debug.LogError("WaveManager: No spawn areas assigned!");
            return;
        }

        // pick a random zone
        var zone = spawnAreas[Random.Range(0, spawnAreas.Length)];

        // select prefab
        GameObject prefab = type switch
        {
            EnemyType.Standard => standardPrefab,
            EnemyType.Armoured => armouredPrefab,
            EnemyType.Drone => dronePrefab,
            _ => null
        };

        if (prefab == null)
        {
            Debug.LogError($"WaveManager: prefab for {type} is null!");
            return;
        }

        // Spawn enemy and subscribe to death event for UI updates
        zone.Spawn(prefab, 1);
        
        // Subscribe to enemy death events to update UI
        StartCoroutine(SubscribeToEnemyDeath());
    }
    
    private IEnumerator SubscribeToEnemyDeath()
    {
        // Wait a frame for enemy to be instantiated
        yield return null;
        
        // Find all enemies and subscribe to their death events
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            var enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Unsubscribe first to avoid duplicates
                enemyHealth.OnDeath -= OnEnemyDied;
                // Subscribe to death event
                enemyHealth.OnDeath += OnEnemyDied;
            }
        }
    }
    
    private void OnEnemyDied(EnemyHealth enemyHealth)
    {
        // Unsubscribe from this enemy's death event
        enemyHealth.OnDeath -= OnEnemyDied;
        
        // Notify UI of enemy death
        OnEnemyKilled?.Invoke();
        if (playerUI != null)
        {
            playerUI.OnEnemyKilled();
        }
    }
    
    // Public getters for UI and other systems
    public int CurrentWave => currentWaveIndex + 1;
    public int TotalWaves => waveController.totalWaves;
    public int RemainingEnemies => GameObject.FindGameObjectsWithTag("Enemy").Length;
    public int TotalEnemiesInCurrentWave => totalEnemiesInCurrentWave;
}
