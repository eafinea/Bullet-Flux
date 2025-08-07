using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Scene Management")]
    [SerializeField] private bool debugMode = false;
    
    // Static context tracking for progress management
    private static bool isGoingToRestPoint = false;
    private static bool isComingFromRestPoint = false;

    public void PlayScene()
    {
        // Reset any rest point flags when starting fresh
        ResetRestPointFlags();
        SceneManager.LoadScene("Arena");
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
    
    public void LoadMainMenu()
    {
        ResetRestPointFlags();
        SceneManager.LoadScene("Main Menu");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void GoToSpecificScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            // Handle progress saving based on scene transition
            HandleSceneTransition(sceneName);
            
            SceneManager.LoadScene($"{sceneName}");
        }
        else
        {
            Debug.LogError($"Scene '{sceneName}' does not exist.");
        }
    }
    
    public void GoToRestPoint()
    {
        if (debugMode)
        {
            Debug.Log("[GameManager] Going to Rest Point - saving current progress");
        }
        
        // Mark that we're going to rest point
        isGoingToRestPoint = true;
        isComingFromRestPoint = false;
        
        // Save current progress before leaving for Rest Point
        SaveCurrentProgress();
        
        GoToSpecificScene("Rest Point");
    }
    
    public void ReturnFromRestPoint()
    {
        if (debugMode)
        {
            Debug.Log("[GameManager] Returning from Rest Point - preserving current state");
        }

        isComingFromRestPoint = true;
        isGoingToRestPoint = false;
        

        
        GoToSpecificScene("Arena");
    }
    
    private void HandleSceneTransition(string targetScene)
    {
        if (targetScene == "Rest Point")
        {
            // Going to Rest Point
            isGoingToRestPoint = true;
            isComingFromRestPoint = false;
            SaveCurrentProgress();
        }
        else if (targetScene == "Arena" && !isComingFromRestPoint)
        {
            // Fresh Arena load (not from Rest Point)
            ResetRestPointFlags();
        }
        else if (targetScene == "Main Menu")
        {
            // Going to main menu
            ResetRestPointFlags();
        }
    }
    
    private void SaveCurrentProgress()
    {
        if (GameProgressManager.Instance != null)
        {
            // Get current wave from WaveManager if available
            var waveManager = FindFirstObjectByType<WaveManager>();
            int currentWave = waveManager != null ? waveManager.CurrentWave : 1;
            
            GameProgressManager.Instance.SaveProgress(currentWave);
            
            if (debugMode)
            {
                Debug.Log($"[GameManager] Progress saved for wave {currentWave}");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] GameProgressManager not found - cannot save progress");
        }
    }
    
    private void ResetRestPointFlags()
    {
        isGoingToRestPoint = false;
        isComingFromRestPoint = false;
        
        if (debugMode)
        {
            Debug.Log("[GameManager] Rest Point flags reset");
        }
    }
    
    public static bool IsGoingToRestPoint => isGoingToRestPoint;
    public static bool IsComingFromRestPoint => isComingFromRestPoint;
    
    public static bool ShouldRestoreProgress()
    {
        return GameProgressManager.Instance?.HasSavedProgress() == true;
    }
    
    [ContextMenu("Debug Rest Point State")]
    private void DebugRestPointState()
    {
        Debug.Log($"[GameManager] Rest Point State:");
        Debug.Log($"  - isGoingToRestPoint: {isGoingToRestPoint}");
        Debug.Log($"  - isComingFromRestPoint: {isComingFromRestPoint}");
        Debug.Log($"  - ShouldRestoreProgress: {ShouldRestoreProgress()}");
        
        if (GameProgressManager.Instance != null)
        {
            var progress = GameProgressManager.Instance.GetCurrentProgress();
            if (progress != null)
            {
                Debug.Log($"  - Current saved wave: {progress.currentWave}");
                Debug.Log($"  - Current saved health: {progress.playerHealth}");
            }
        }
    }
}
