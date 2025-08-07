using UnityEngine;
using System;

[System.Serializable]
public class GameProgress
{
    public int currentWave;
    public float playerHealth;
    public float playerMaxHealth;
    public GunStats.WeaponType currentWeaponType;
    public int remainingShots;
    public float currentHeat;
    public BulletEffectType activeBulletEffects;
    public bool hasPowerupWeapon;
    public Vector3 playerPosition;
    public string timestamp;

    public GameProgress()
    {
        timestamp = DateTime.Now.ToString();
    }
}

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private bool enableSaving = true;
    [SerializeField] private string saveKey = "GameProgress";

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private GameProgress currentProgress;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveProgress(int currentWave)
    {
        if (!enableSaving) return;

        try
        {
            var progress = new GameProgress();
            progress.currentWave = currentWave;

            // Find and save player data
            var player = FindFirstObjectByType<PlayerHealth>();
            if (player != null)
            {
                progress.playerHealth = player.CurrentHealth;
                progress.playerMaxHealth = player.MaxHealth;
                progress.playerPosition = player.transform.position;
            }

            // Find and save weapon data
            var weaponManager = FindFirstObjectByType<WeaponManager>();
            if (weaponManager != null)
            {
                progress.currentWeaponType = weaponManager.CurrentWeaponType;
                progress.hasPowerupWeapon = weaponManager.HasPowerupWeapon;

                if (weaponManager.CurrentGunStats != null)
                {
                    progress.remainingShots = weaponManager.CurrentGunStats.RemainingShots;
                    progress.currentHeat = weaponManager.CurrentGunStats.CurrentHeat;
                }

                var bulletEffects = weaponManager.GetBulletEffects();
                if (bulletEffects != null)
                {
                    progress.activeBulletEffects = bulletEffects.CurrentEffects;
                }
            }

            // Save to PlayerPrefs
            string json = JsonUtility.ToJson(progress, true);
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();

            currentProgress = progress;

            if (debugMode)
            {
                Debug.Log($"[GameProgressManager] Progress saved for wave {currentWave}");
                Debug.Log($"[GameProgressManager] JSON: {json}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgressManager] Failed to save progress: {e.Message}");
        }
    }

    public GameProgress LoadProgress()
    {
        if (!enableSaving) return null;

        try
        {
            if (PlayerPrefs.HasKey(saveKey))
            {
                string json = PlayerPrefs.GetString(saveKey);
                var progress = JsonUtility.FromJson<GameProgress>(json);

                if (debugMode)
                {
                    Debug.Log($"[GameProgressManager] Progress loaded for wave {progress.currentWave}");
                }

                currentProgress = progress;
                return progress;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgressManager] Failed to load progress: {e.Message}");
        }

        return null;
    }

    public void RestoreProgress(GameProgress progress)
    {
        if (progress == null) return;

        try
        {
            // Restore player health
            var player = FindFirstObjectByType<PlayerHealth>();
            if (player != null)
            {
                // Set health but don't exceed max
                float healthToRestore = Mathf.Min(progress.playerHealth, progress.playerMaxHealth);
                player.ResetHealth();
                if (healthToRestore < progress.playerMaxHealth)
                {
                    player.TakeDamage(progress.playerMaxHealth - healthToRestore);
                }
            }

            // Restore weapon state
            var weaponManager = FindFirstObjectByType<WeaponManager>();
            if (weaponManager != null)
            {
                // Restore weapon type if it's a powerup
                if (progress.hasPowerupWeapon && progress.currentWeaponType != GunStats.WeaponType.Pistol)
                {
                    weaponManager.SetupTimedWeaponPowerup(progress.currentWeaponType, progress.remainingShots, 999f); // Long duration
                }

                // Restore bullet effects
                if (progress.activeBulletEffects != BulletEffectType.None)
                {
                    weaponManager.ApplyBulletEffect(progress.activeBulletEffects, 999f); // Long duration
                }

                // Restore gun stats
                if (weaponManager.CurrentGunStats != null)
                {
                    if (weaponManager.CurrentGunStats.UsesOverheatSystem())
                    {
                        weaponManager.CurrentGunStats.SetHeat(progress.currentHeat);
                    }
                }
            }

            // Restore wave manager state
            var waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.SetCurrentWave(progress.currentWave);
            }

            if (debugMode)
            {
                Debug.Log($"[GameProgressManager] Progress restored for wave {progress.currentWave}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgressManager] Failed to restore progress: {e.Message}");
        }
    }

    public void ClearProgress()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
        }

        currentProgress = null;

        if (debugMode)
        {
            Debug.Log("[GameProgressManager] Progress cleared");
        }
    }

    public bool HasSavedProgress()
    {
        return enableSaving && PlayerPrefs.HasKey(saveKey);
    }

    public GameProgress GetCurrentProgress()
    {
        return currentProgress;
    }
}