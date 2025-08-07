using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Pause Menu UI")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI resumeButtonText;
    
    [Header("Pause Settings")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    
    [Header("Game Over Debug")]
    [SerializeField] private bool disableGameOver = false; // Debug option to prevent game over
    
    [Header("Progress Management")]
    [SerializeField] private bool clearProgressOnRetry = true;
    [SerializeField] private bool useGameManager = true;
    
    // State management
    private bool isPaused = false;
    private bool isGameOver = false;
    private bool wasApplicationFocused = true;
    
    // Static instance for easy access
    public static PauseMenuManager Instance { get; private set; }
    
    private GameManager gameManager;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"PauseMenuManager instance created on GameObject: {gameObject.name}");
        }
        else
        {
            Debug.Log($"PauseMenuManager duplicate destroyed on GameObject: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        if (useGameManager)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("[PauseMenuManager] GameManager not found! Falling back to direct scene loading.");
                useGameManager = false;
            }
        }
        
        // Setup UI
        SetupUI();
    }
    
    private void Start()
    {
        // Ensure pause menu is hidden at start
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        // Subscribe to player death event
        Debug.Log("Subscribing to PlayerHealth.OnPlayerDied event...");
        PlayerHealth.OnPlayerDied += OnPlayerDied;
        Debug.Log("PauseMenuManager subscribed to PlayerHealth.OnPlayerDied event. DisableGameOver: " + disableGameOver);
        
        Debug.Log("Event subscription completed successfully");
    }
    
    private void Update()
    {
        // Check for pause input
        if (Input.GetKeyDown(pauseKey) && !isGameOver)
        {
            TogglePause();
        }
        
        // Check for application focus changes
        CheckApplicationFocus();
    }
    
    private void SetupUI()
    {
        // Setup button listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
        
        // Get button text component if not assigned
        if (resumeButtonText == null && resumeButton != null)
        {
            resumeButtonText = resumeButton.GetComponentInChildren<TextMeshProUGUI>();
        }
        
        Debug.Log("PauseMenuManager UI setup complete. ResumeButton: " + (resumeButton != null) + 
                  ", QuitButton: " + (quitButton != null) + 
                  ", ResumeButtonText: " + (resumeButtonText != null));
    }
    
    private void CheckApplicationFocus()
    {
        bool currentFocus = Application.isFocused;
        
        // If application lost focus and wasn't already paused
        if (wasApplicationFocused && !currentFocus && !isPaused && !isGameOver)
        {
            PauseGame();
        }
        
        wasApplicationFocused = currentFocus;
    }
    
    private void OnPlayerDied()
    {
        Debug.Log("=== PauseMenuManager.OnPlayerDied() called! ===");
        Debug.Log("DisableGameOver: " + disableGameOver);
        Debug.Log("Current GameObject: " + gameObject.name);
        Debug.Log("Instance reference: " + (Instance != null ? Instance.gameObject.name : "NULL"));
        
        if (!disableGameOver)
        {
            Debug.Log("Triggering game over...");
            TriggerGameOver();
        }
        else
        {
            Debug.Log("Player died, but game over is disabled for testing");
        }
    }
    
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    public void PauseGame()
    {
        if (isGameOver) return; // Don't allow normal pause during game over
        
        isPaused = true;
        Time.timeScale = 0f;
        
        // Show pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        // Update button text for normal pause
        UpdateButtonText(false);
        
        // Show and unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("Game Paused");
    }
    
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        // Hide pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        // Lock and hide cursor (restore game state)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("Game Resumed");
    }
    
    public void TriggerGameOver()
    {
        Debug.Log("=== TriggerGameOver() called! ===");
        
        isGameOver = true;
        isPaused = true;
        Time.timeScale = 0f;
        
        // Show pause menu with game over state
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            Debug.Log("Pause menu panel activated for game over");
        }
        else
        {
            Debug.LogError("Pause menu panel is null!");
        }
        
        // Update button text for game over
        UpdateButtonText(true);
        
        // Show and unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("Game Over triggered successfully");
    }
    
    private void UpdateButtonText(bool isGameOverState)
    {
        if (resumeButtonText != null)
        {
            string newText = isGameOverState ? "Retry" : "Resume";
            resumeButtonText.text = newText;
            Debug.Log("Button text updated to: " + newText);
        }
        else
        {
            Debug.LogWarning("ResumeButtonText is null - cannot update button text");
        }
    }
    
    private void OnResumeClicked()
    {
        Debug.Log("Resume button clicked. IsGameOver: " + isGameOver);
        
        if (isGameOver)
        {
            // Retry - reload the arena scene
            RetryGame();
        }
        else
        {
            // Normal resume
            ResumeGame();
        }
    }
    
    private void OnQuitClicked()
    {
        Debug.Log("Quit button clicked");
        QuitToMainMenu();
    }
    
    public void RetryGame()
    {
        Debug.Log("Retrying game...");
        
        if (clearProgressOnRetry && GameProgressManager.Instance != null)
        {
            Debug.Log("Clearing progress on retry...");
            GameProgressManager.Instance.ClearProgress();
        }
        
        // Reset time scale before scene change
        Time.timeScale = 1f;
        
        // Load arena scene using preferred method
        LoadScene("Arena");
        
        Debug.Log("Retrying game - reloading Arena scene");
    }
    
    public void QuitToMainMenu()
    {
        Debug.Log("Returning to main menu...");
        
        // Reset time scale before scene change
        Time.timeScale = 1f;
        
        // Load main menu scene
        LoadScene("Main Menu");
        
        // Ensure cursor is visible in main menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("Returning to Main Menu");
    }
    
    private void LoadScene(string sceneName)
    {
        if (useGameManager && gameManager != null)
        {
            Debug.Log($"[PauseMenuManager] Using GameManager to load scene: {sceneName}");
            gameManager.GoToSpecificScene(sceneName);
        }
        else
        {
            Debug.Log($"[PauseMenuManager] Using direct scene loading for: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
    
    // Public properties for external access
    public bool IsPaused => isPaused;
    public bool IsGameOver => isGameOver;
    
    // Method to force game over (for testing)
    [ContextMenu("Force Game Over")]
    public void ForceGameOver()
    {
        Debug.Log("Force Game Over triggered from context menu");
        TriggerGameOver();
    }
    
    // Method to test event subscription
    [ContextMenu("Test Event Subscription")]
    public void TestEventSubscription()
    {
        Debug.Log("Testing event subscription...");
        Debug.Log("Event subscription test completed. To test death, use PlayerHealth 'Test Player Death' context menu or DOT interactable.");
    }
    
    // Method to reset game over state (for testing)
    [ContextMenu("Reset Game Over")]
    public void ResetGameOver()
    {
        Debug.Log("Reset Game Over triggered from context menu");
        isGameOver = false;
        if (isPaused)
        {
            ResumeGame();
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        PlayerHealth.OnPlayerDied -= OnPlayerDied;
        
        // Clean up singleton reference
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Ensure time scale is reset
        Time.timeScale = 1f;
        
        Debug.Log("PauseMenuManager destroyed and cleaned up");
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // Handle mobile platform pause/resume
        if (pauseStatus && !isPaused && !isGameOver)
        {
            PauseGame();
        }
    }
}