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
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    public event Action OnPlayerEntered;

    private Collider triggerCollider;
    private bool isLoading = false;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
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
                Debug.Log($"[RestPointTrigger] Player entered rest point trigger. Loading scene: {restPointSceneName}");
            }

            // Prevent multiple triggers
            isLoading = true;

            // Notify the wave manager
            OnPlayerEntered?.Invoke();

            // Load rest point scene with delay and validation
            StartCoroutine(LoadRestPointSceneCoroutine());
        }
    }

    private IEnumerator LoadRestPointSceneCoroutine()
    {
        // Optional delay to ensure all systems are notified
        if (loadingDelay > 0f)
        {
            yield return new WaitForSeconds(loadingDelay);
        }

        // Validate scene exists before loading
        if (!IsSceneInBuildSettings(restPointSceneName))
        {
            Debug.LogError($"[RestPointTrigger] Scene '{restPointSceneName}' not found in Build Settings! Please add it to the build settings.");
            isLoading = false;
            yield break;
        }

        if (debugMode)
        {
            Debug.Log($"[RestPointTrigger] Starting scene load: {restPointSceneName}");
        }

            // Handle async loading without try-catch around yield statements
        if (useAsyncLoading)
        {
            AsyncOperation asyncLoad = null;
            
            // Start async loading (can be in try-catch since no yield here)
            try
            {
                asyncLoad = SceneManager.LoadSceneAsync(restPointSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RestPointTrigger] Error starting async load for scene '{restPointSceneName}': {e.Message}");
                isLoading = false;
                yield break;
            }
            
            if (asyncLoad != null)
            {
                // Wait for the scene to load (yield statements outside try-catch)
                while (!asyncLoad.isDone)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[RestPointTrigger] Loading progress: {asyncLoad.progress * 100:F1}%");
                    }
                    yield return null;
                }
                
                if (debugMode)
                {
                    Debug.Log($"[RestPointTrigger] Scene '{restPointSceneName}' loaded successfully!");
                }
            }
            else
            {
                Debug.LogError($"[RestPointTrigger] Failed to start async loading for scene '{restPointSceneName}'");
                isLoading = false;
            }
        }
        else
        {
            // Use synchronous loading (no yield statements here)
            try
            {
                SceneManager.LoadScene(restPointSceneName);
                
                if (debugMode)
                {
                    Debug.Log($"[RestPointTrigger] Scene '{restPointSceneName}' loaded synchronously!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RestPointTrigger] Error loading scene '{restPointSceneName}': {e.Message}");
                isLoading = false;
            }
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
            StartCoroutine(LoadRestPointSceneCoroutine());
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
}