using UnityEngine;
using UnityEngine.SceneManagement;

public class RestPoint : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string arenaSceneName = "Arena";
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [SerializeField] private bool useGameManager = true;

    [Header("UI Elements")]
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private GameObject menuPrompt;

    private Collider doorTrigger;
    private GameManager gameManager;

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
        if (useGameManager)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("[RestPoint] GameManager not found! Falling back to direct scene loading.");
                useGameManager = false;
            }
        }
    }

    private void Start()
    {
        ShowProgressInfo();
        RestorePlayerStatsInRestPoint();
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

    private void RestorePlayerStatsInRestPoint()
    {
        if (GameProgressManager.Instance != null)
        {
            var progress = GameProgressManager.Instance.LoadProgress();
            if (progress != null)
            {
                // Find and restore player health in Rest Point scene
                var player = FindFirstObjectByType<PlayerHealth>();
                if (player != null)
                {
                    // Set health to saved value
                    float healthToRestore = Mathf.Min(progress.playerHealth, progress.playerMaxHealth);
                    player.ResetHealth();
                    if (healthToRestore < progress.playerMaxHealth)
                    {
                        player.TakeDamage(progress.playerMaxHealth - healthToRestore);
                    }
                    Debug.Log($"[RestPoint] Player health restored to {healthToRestore}/{progress.playerMaxHealth}");
                }

                // Find and restore weapon state in Rest Point scene
                var weaponManager = FindFirstObjectByType<WeaponManager>();
                if (weaponManager != null)
                {
                    // Restore weapon type if it's a powerup
                    if (progress.hasPowerupWeapon && progress.currentWeaponType != GunStats.WeaponType.Pistol)
                    {
                        weaponManager.SetupTimedWeaponPowerup(progress.currentWeaponType, progress.remainingShots, 999f);
                        Debug.Log($"[RestPoint] Weapon restored to {progress.currentWeaponType} with {progress.remainingShots} shots");
                    }

                    // Restore bullet effects
                    if (progress.activeBulletEffects != BulletEffectType.None)
                    {
                        weaponManager.ApplyBulletEffect(progress.activeBulletEffects, 999f);
                        Debug.Log($"[RestPoint] Bullet effects restored: {progress.activeBulletEffects}");
                    }
                }

                Debug.Log($"[RestPoint] Player stats restored from wave {progress.currentWave}");
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

                if (useGameManager && gameManager != null)
                {
                    gameManager.ReturnFromRestPoint();
                }
                else
                {
                    LoadScene(arenaSceneName);
                }
            }
            else if (CompareTag("MenuDoor"))
            {
                // Return to main menu and clear progress
                Debug.Log("Returning to main menu");

                if (GameProgressManager.Instance != null)
                {
                    GameProgressManager.Instance.ClearProgress();
                }

                LoadScene(mainMenuSceneName);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    private void LoadScene(string sceneName)
    {
        if (useGameManager && gameManager != null)
        {
            Debug.Log($"[RestPoint] Using GameManager to load scene: {sceneName}");
            gameManager.GoToSpecificScene(sceneName);
        }
        else
        {
            Debug.Log($"[RestPoint] Using direct scene loading for: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}