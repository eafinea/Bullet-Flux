using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Configuration")]
    public WaveController waveController;

    [Header("Spawning")]
    public SpawnArea[] spawnZones;
    public GameObject standardPrefab;
    public GameObject armouredPrefab;
    public GameObject dronePrefab;

    [Header("UI Integration")]
    [SerializeField] private PlayerUI playerUI;

    [Header("Rest Point System")]
    [SerializeField] private int restPointInterval = 5; // Every 5th wave
    [SerializeField] private GameObject restPointDoor;
    [SerializeField] private Billboard restPointArrow;
    [SerializeField] private RestPointTrigger restPointTrigger;

    // Events
    public UnityEvent<int> OnWaveStarted = new UnityEvent<int>();
    public UnityEvent OnWaveCompleted = new UnityEvent();
    public UnityEvent OnEnemyKilled = new UnityEvent();
    public UnityEvent<int> OnRestPointTriggered = new UnityEvent<int>();

    // Wave tracking
    private int currentWaveIndex = 0;
    private int totalEnemiesInCurrentWave = 0;
    private bool isRestPointActive = false;
    private bool gameCompleted = false;

    void Start()
    {
        if (waveController == null)
        {
            Debug.LogError("WaveController not assigned!");
            return;
        }

        if (spawnZones == null || spawnZones.Length == 0)
        {
            Debug.LogError("No spawn zones assigned!");
            return;
        }

        // Get PlayerUI if not assigned
        if (playerUI == null)
        {
            playerUI = FindFirstObjectByType<PlayerUI>();
        }

        // Initialize rest point system
        InitializeRestPointSystem();

        // Check if we should load progress
        CheckForSavedProgress();
    }

    private void InitializeRestPointSystem()
    {
        if (restPointDoor != null)
        {
            restPointDoor.SetActive(false);
        }

        if (restPointArrow != null)
        {
            restPointArrow.gameObject.SetActive(false);
        }

        if (restPointTrigger != null)
        {
            restPointTrigger.gameObject.SetActive(false);
            restPointTrigger.OnPlayerEntered += OnRestPointEntered;
        }
    }

    private void CheckForSavedProgress()
    {
        if (GameProgressManager.Instance != null && GameProgressManager.Instance.HasSavedProgress())
        {
            var progress = GameProgressManager.Instance.LoadProgress();
            if (progress != null)
            {
                currentWaveIndex = progress.currentWave - 1; // Convert to 0-based index
                StartCoroutine(DelayedProgressRestore(progress));
                return;
            }
        }

        // No saved progress, start normally
        StartCoroutine(RunWaves());
    }

    private IEnumerator DelayedProgressRestore(GameProgress progress)
    {
        // Wait a frame for everything to initialize
        yield return new WaitForFixedUpdate();

        // Restore progress
        GameProgressManager.Instance.RestoreProgress(progress);

        // Start waves from the saved point
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        while (currentWaveIndex < waveController.totalWaves && !gameCompleted)
        {
            // Check if this is a rest point wave
            if (ShouldTriggerRestPoint())
            {
                yield return StartCoroutine(HandleRestPoint());
                if (gameCompleted) break; // Player might have quit during rest point
            }

            waveController.StartWave(currentWaveIndex);
            totalEnemiesInCurrentWave = GetTotalEnemiesForWave();

            // Notify UI and events
            OnWaveStarted?.Invoke(currentWaveIndex + 1);
            if (playerUI != null)
            {
                playerUI.OnWaveStarted(currentWaveIndex + 1, totalEnemiesInCurrentWave);
            }

            Debug.Log($"Starting Wave {currentWaveIndex + 1}");

            // Spawn enemies until wave is complete
            while (!waveController.IsWaveComplete)
            {
                var nextType = waveController.GetNextSpawnType();
                if (nextType.HasValue)
                {
                    SpawnEnemy(nextType.Value);
                    waveController.OnSpawn(nextType.Value);
                    yield return new WaitForSeconds(0.5f);
                }
                yield return null;
            }

            // Wait for all enemies to be defeated
            while (RemainingEnemies > 0)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log($"Wave {currentWaveIndex + 1} completed!");
            OnWaveCompleted?.Invoke();
            if (playerUI != null)
            {
                playerUI.OnWaveCompleted();
            }

            currentWaveIndex++;
            yield return new WaitForSeconds(2f);
        }

        if (!gameCompleted)
        {
            Debug.Log("All waves completed! Game won!");
            // Handle game completion
        }
    }

    private bool ShouldTriggerRestPoint()
    {
        int waveNumber = currentWaveIndex + 1;
        return waveNumber > 0 && waveNumber % restPointInterval == 0 && waveNumber < waveController.totalWaves;
    }

    private IEnumerator HandleRestPoint()
    {
        Debug.Log($"Triggering Rest Point after wave {currentWaveIndex + 1}");

        isRestPointActive = true;

        // Save current progress
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.SaveProgress(currentWaveIndex + 1);
        }

        // Activate rest point elements
        ActivateRestPoint();

        // Notify systems
        OnRestPointTriggered?.Invoke(currentWaveIndex + 1);

        // Wait for player to enter rest point or continue
        while (isRestPointActive && !gameCompleted)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Deactivate rest point elements
        DeactivateRestPoint();
    }

    private void ActivateRestPoint()
    {
        if (restPointDoor != null)
        {
            restPointDoor.SetActive(true);

            // Open the door if it has a Door component
            var doorComponent = restPointDoor.GetComponent<Door>();
            if (doorComponent != null)
            {
                doorComponent.OpenDoor();
            }
        }

        if (restPointArrow != null)
        {
            restPointArrow.gameObject.SetActive(true);
        }

        if (restPointTrigger != null)
        {
            restPointTrigger.gameObject.SetActive(true);
        }

        Debug.Log("Rest Point activated - door opened and arrow enabled");
    }

    private void DeactivateRestPoint()
    {
        if (restPointDoor != null)
        {
            restPointDoor.SetActive(false);
        }

        if (restPointArrow != null)
        {
            restPointArrow.gameObject.SetActive(false);
        }

        if (restPointTrigger != null)
        {
            restPointTrigger.gameObject.SetActive(false);
        }
    }

    private void OnRestPointEntered()
    {
        Debug.Log("Player entered Rest Point trigger");
        isRestPointActive = false;

        // Save progress again before scene transition
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.SaveProgress(currentWaveIndex + 1);
        }
    }

    private int GetTotalEnemiesForWave()
    {
        // Calculate total enemies for current wave based on wave controller
        // This is a simplified calculation - adjust based on your wave controller logic
        return 10 + (currentWaveIndex * 2); // Example: increasing enemies per wave
    }

    void SpawnEnemy(EnemyType type)
    {
        GameObject prefab = type switch
        {
            EnemyType.Standard => standardPrefab,
            EnemyType.Armoured => armouredPrefab,
            EnemyType.Drone => dronePrefab,
            _ => standardPrefab
        };

        if (prefab == null)
        {
            Debug.LogError($"No prefab assigned for enemy type: {type}");
            return;
        }

        if (spawnZones.Length == 0)
        {
            Debug.LogError("No spawn zones available!");
            return;
        }

        SpawnArea zone = spawnZones[Random.Range(0, spawnZones.Length)];
        zone.Spawn(prefab, 1);

        StartCoroutine(SubscribeToEnemyDeath());
    }

    private IEnumerator SubscribeToEnemyDeath()
    {
        yield return null;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            var enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath -= OnEnemyDied;
                enemyHealth.OnDeath += OnEnemyDied;
            }
        }
    }

    private void OnEnemyDied(EnemyHealth enemyHealth)
    {
        enemyHealth.OnDeath -= OnEnemyDied;

        OnEnemyKilled?.Invoke();
        if (playerUI != null)
        {
            playerUI.OnEnemyKilled();
        }
    }

    // Public methods for external access
    public void SetCurrentWave(int waveIndex)
    {
        currentWaveIndex = waveIndex - 1; // Convert to 0-based index
    }

    public void ForceRestPoint()
    {
        if (!isRestPointActive)
        {
            StartCoroutine(HandleRestPoint());
        }
    }

    public void SkipRestPoint()
    {
        isRestPointActive = false;
    }

    // Public getters
    public int CurrentWave => currentWaveIndex + 1;
    public int TotalWaves => waveController.totalWaves;
    public int RemainingEnemies => GameObject.FindGameObjectsWithTag("Enemy").Length;
    public int TotalEnemiesInCurrentWave => totalEnemiesInCurrentWave;
    public bool IsRestPointActive => isRestPointActive;
}