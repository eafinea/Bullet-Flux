using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class RestPointTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string restPointSceneName = "Rest Point";
    [SerializeField] private bool validateSceneExists = true;
    
    [Header("Loading Settings")]
    [SerializeField] private bool useAsyncLoading = true;
    [SerializeField] private float loadingDelay = 0.1f;
    [SerializeField] private bool useGameManager = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    public event Action OnPlayerEntered;

    private Collider triggerCollider;
    private bool isLoading = false;
    private GameManager gameManager;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        
        if (useGameManager)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null && debugMode)
            {
                Debug.LogWarning("[RestPointTrigger] GameManager not found! Falling back to direct scene loading.");
                useGameManager = false;
            }
        }
        
        // Validate scene exists at startup
        if (validateSceneExists)
        {
            ValidateRestPointScene();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isLoading)
        {
            if (debugMode)
            {
                Debug.Log($"[RestPointTrigger] Player entered rest point trigger. Current TimeScale: {Time.timeScale}");
                Debug.Log($"[RestPointTrigger] Loading scene: {restPointSceneName}");
            }

            // Prevent multiple triggers
            isLoading = true;

            StopAllGameSystems();

            OnPlayerEntered?.Invoke();

            LoadSceneImmediately();
        }
    }

    private void StopAllGameSystems()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        
        if (debugMode)
        {
            Debug.Log($"[RestPointTrigger] TimeScale reset from {originalTimeScale} to {Time.timeScale}");
        }

        // Stop all coroutines in the scene
        MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in allMonoBehaviours)
        {
            if (mb != this) // Don't stop our own coroutines
            {
                mb.StopAllCoroutines();
            }
        }

        // Disable pause menu to prevent interference
        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.enabled = false;
        }

        // Disable wave manager to stop spawning
        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.enabled = false;
        }

        if (debugMode)
        {
            Debug.Log("[RestPointTrigger] All game systems stopped");
        }
    }

    private void LoadSceneImmediately()
    {
        // Validate scene exists
        if (!IsSceneInBuildSettings(restPointSceneName))
        {
            Debug.LogError($"[RestPointTrigger] Scene '{restPointSceneName}' not found in Build Settings!");
            return;
        }

        try
        {
            if (debugMode)
            {
                Debug.Log($"[RestPointTrigger] Loading scene immediately: {restPointSceneName}");
            }

            // Use GameManager method for Rest Point transition
            if (useGameManager && gameManager != null)
            {
                gameManager.GoToRestPoint();
            }
            else
            {
                SceneManager.LoadScene(restPointSceneName);
            }

            if (debugMode)
            {
                Debug.Log($"[RestPointTrigger] Scene load initiated for: {restPointSceneName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RestPointTrigger] Error loading scene '{restPointSceneName}': {e.Message}");
            
            // Reset systems if loading failed
            Time.timeScale = 1f;
            isLoading = false;
        }
    }

    private void ValidateRestPointScene()
    {
        if (!IsSceneInBuildSettings(restPointSceneName))
        {
            Debug.LogWarning($"[RestPointTrigger] Scene '{restPointSceneName}' is not in Build Settings. " +
                           "Make sure to add it to File > Build Settings > Scenes In Build.");
        }
        else if (debugMode)
        {
            Debug.Log($"[RestPointTrigger] Scene '{restPointSceneName}' found in Build Settings.");
        }
    }

    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameFromPath.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    // Public method to manually trigger scene load (for testing)
    public void ManualTriggerSceneLoad()
    {
        if (!isLoading)
        {
            Debug.Log("[RestPointTrigger] Manual scene load triggered");
            OnPlayerEntered?.Invoke();
            StopAllGameSystems();
            LoadSceneImmediately();
        }
    }

    // Public method to test if scene exists
    public bool TestSceneExists()
    {
        bool exists = IsSceneInBuildSettings(restPointSceneName);
        Debug.Log($"[RestPointTrigger] Scene '{restPointSceneName}' exists in build settings: {exists}");
        return exists;
    }

    private void OnDrawGizmos()
    {
        if (triggerCollider != null)
        {
            Gizmos.color = isLoading ? Color.red : Color.cyan;
            Gizmos.DrawWireCube(transform.position + triggerCollider.bounds.center, triggerCollider.bounds.size);
            
            // Draw loading indicator
            if (isLoading)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.position + Vector3.up, 0.5f);
            }
            if (useGameManager && gameManager != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
            }
        }
    }

    // Reset loading state (useful for testing)
    public void ResetLoadingState()
    {
        isLoading = false;
        if (debugMode)
        {
            Debug.Log("[RestPointTrigger] Loading state reset");
        }
    }

    // Context menu for testing
    [ContextMenu("Test Scene Load")]
    private void TestSceneLoad()
    {
        if (Application.isPlaying)
        {
            ManualTriggerSceneLoad();
        }
        else
        {
            TestSceneExists();
        }
    }

    [ContextMenu("Reset Loading State")]
    private void ContextResetLoadingState()
    {
        ResetLoadingState();
    }

    [ContextMenu("Find GameManager")]
    private void FindGameManager()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log($"[RestPointTrigger] Found GameManager: {gameManager.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[RestPointTrigger] GameManager not found in scene!");
        }
    }

    [ContextMenu("Check TimeScale")]
    private void CheckTimeScale()
    {
        Debug.Log($"[RestPointTrigger] Current Time.timeScale: {Time.timeScale}");
        Debug.Log($"[RestPointTrigger] PauseMenuManager.Instance exists: {PauseMenuManager.Instance != null}");
        if (PauseMenuManager.Instance != null)
        {
            Debug.Log($"[RestPointTrigger] Is Paused: {PauseMenuManager.Instance.IsPaused}");
            Debug.Log($"[RestPointTrigger] Is Game Over: {PauseMenuManager.Instance.IsGameOver}");
        }
    }

    [ContextMenu("Force Load Rest Point")]
    private void ForceLoadRestPoint()
    {
        Debug.Log("Force loading Rest Point scene...");
        Time.timeScale = 1f;
        SceneManager.LoadScene("Rest Point");
    }
}