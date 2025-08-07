using UnityEngine;
using UnityEngine.SceneManagement;

public class RestPoint : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string arenaSceneName = "Arena";
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("UI Elements")]
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private GameObject menuPrompt;

    private Collider doorTrigger;

    private void Awake()
    {
        doorTrigger = GetComponent<Collider>();
        if (doorTrigger == null)
        {
            Debug.LogError($"[RestPoint] {name}: No Collider component found!");
            return;
        }
        if (!doorTrigger.isTrigger)
        {
            doorTrigger.isTrigger = true;
        }
    }

    private void Start()
    {
        // Show progress information if available
        ShowProgressInfo();
    }

    private void ShowProgressInfo()
    {
        if (GameProgressManager.Instance != null)
        {
            var progress = GameProgressManager.Instance.GetCurrentProgress();
            if (progress != null)
            {
                Debug.Log($"Rest Point - Current Wave: {progress.currentWave}");
                Debug.Log($"Player Health: {progress.playerHealth}/{progress.playerMaxHealth}");
                Debug.Log($"Weapon: {progress.currentWeaponType}");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (CompareTag("ContinueDoor"))
            {
                // Continue playing - return to arena
                Debug.Log("Continuing game - returning to arena");
                SceneManager.LoadScene(arenaSceneName);
            }
            else if (CompareTag("MenuDoor"))
            {
                // Return to main menu and clear progress
                Debug.Log("Returning to main menu");

                if (GameProgressManager.Instance != null)
                {
                    GameProgressManager.Instance.ClearProgress();
                }

                SceneManager.LoadScene(mainMenuSceneName);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}