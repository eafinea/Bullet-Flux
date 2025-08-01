using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("UI Text Elements")]
    [SerializeField]
    private TextMeshProUGUI promptText;
    
    [Header("Ammo Display - Visual Bars")]
    [SerializeField] private Image frontAmmoBar;        // Main ammo bar (like frontHealthBar)
    [SerializeField] private TextMeshProUGUI ammoText;  // Optional text overlay (e.g., "15/30")
    
    [Header("Heat Display - Visual Bar (Pistol)")]
    [SerializeField] private Image heatBar;             // Heat bar for pistol
    [SerializeField] private TextMeshProUGUI heatText;  // Optional heat text overlay
    
    [Header("Weapon Icon System")]
    [SerializeField] private Image pistolIcon;          // Pistol weapon icon
    [SerializeField] private Image smgIcon;             // SMG weapon icon
    [SerializeField] private Image shotgunIcon;         // Shotgun weapon icon
    
    [Header("Wave Indicator System")]
    [SerializeField] private Image frontEnemyBar;       // Front bar showing remaining enemies
    [SerializeField] private Image backEnemyBar;        // Back bar for smooth animation
    [SerializeField] private Text waveText;  // Wave number display
    [SerializeField] private TextMeshProUGUI enemyCountText; // Optional enemy count text
    
    [Header("Wave Display Settings")]
    [SerializeField] private bool showEnemyCount = true;
    
    [Header("Manager References")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private WaveManager waveManager;
    
    // Animation settings similar to health system
    [Header("Animation Settings")]
    [SerializeField] private float barAnimationSpeed = 2f;
    [SerializeField] private float enemyBarAnimationSpeed = 1.5f;
    
    private GunStats currentGunStats;
    private float ammoLerpTimer;
    private float heatLerpTimer;
    private float enemyBarLerpTimer;
    
    // Wave tracking variables
    private int currentWave = 0;
    private int totalWaves = 0;
    private int currentEnemyCount = 0;
    private int totalEnemiesInWave = 0;

    private void Start()
    {
        // Get WeaponManager if not assigned
        if (weaponManager == null)
        {
            weaponManager = GetComponentInChildren<WeaponManager>();
            if (weaponManager == null)
            {
                Debug.LogWarning("WeaponManager not found! Ammo display may not work correctly.");
            }
        }

        // Get WaveManager if not assigned
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager == null)
            {
                Debug.LogWarning("WaveManager not found! Wave display will not work correctly.");
            }
        }

        // Subscribe to weapon change events
        if (weaponManager != null)
        {
            weaponManager.OnWeaponSwitched += OnWeaponChanged;
            // Initialize with current weapon
            OnWeaponChanged(weaponManager.CurrentGun);
        }

        // Initialize wave display
        InitializeWaveDisplay();
    }

    private void Update()
    {
        // Update visual bars with smooth animation
        UpdateAmmoBarAnimation();
        UpdateHeatBarAnimation();
        UpdateEnemyBarAnimation();
        
        // Update wave display
        UpdateWaveDisplay();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (weaponManager != null)
        {
            weaponManager.OnWeaponSwitched -= OnWeaponChanged;
        }
    }

    private void OnWeaponChanged(Gun newGun)
    {
        if (weaponManager != null)
        {
            currentGunStats = weaponManager.CurrentGunStats;
            
            // Initialize the ammo display for the new weapon
            InitializeAmmoDisplay();
            
            // Update weapon icon display
            UpdateWeaponIconDisplay();
        }
    }

    private void InitializeAmmoDisplay()
    {
        if (currentGunStats == null) return;

        // Set initial ammo display based on weapon type
        switch (currentGunStats.CurrentWeaponType)
        {
            case GunStats.WeaponType.Pistol:
                ShowHeatDisplay();
                int initialCoolness = Mathf.RoundToInt(((currentGunStats.MaxHeat - currentGunStats.CurrentHeat) / currentGunStats.MaxHeat) * 100);
                UpdateHeatDisplay(initialCoolness, 100);
                break;
                
            case GunStats.WeaponType.Shotgun:
            case GunStats.WeaponType.SMG:
                ShowAmmoDisplay();
                UpdateAmmoDisplay(currentGunStats.RemainingShots, currentGunStats.TotalShots);
                break;
        }
    }

    private void UpdateWeaponIconDisplay()
    {
        if (currentGunStats == null) return;

        // Get current weapon type (either active weapon or powerup weapon)
        GunStats.WeaponType currentWeaponType = weaponManager.CurrentWeaponType;

        // Hide all weapon icons first
        if (pistolIcon != null) pistolIcon.gameObject.SetActive(false);
        if (smgIcon != null) smgIcon.gameObject.SetActive(false);
        if (shotgunIcon != null) shotgunIcon.gameObject.SetActive(false);

        // Show the appropriate weapon icon
        switch (currentWeaponType)
        {
            case GunStats.WeaponType.Pistol:
                if (pistolIcon != null) pistolIcon.gameObject.SetActive(true);
                break;
                
            case GunStats.WeaponType.SMG:
                if (smgIcon != null) smgIcon.gameObject.SetActive(true);
                break;
                
            case GunStats.WeaponType.Shotgun:
                if (shotgunIcon != null) shotgunIcon.gameObject.SetActive(true);
                break;
        }
    }

    // === WAVE DISPLAY METHODS ===
    
    private void InitializeWaveDisplay()
    {
        if (waveManager != null && waveManager.waveController != null)
        {
            totalWaves = waveManager.waveController.totalWaves;
            UpdateWaveText(1); // Start with wave 1
            UpdateEnemyDisplay(0, 0); // Start with no enemies
        }
        else
        {
            // Fallback values
            UpdateWaveText(1);
            UpdateEnemyDisplay(0, 0);
        }
    }

    private void UpdateWaveDisplay()
    {
        if (waveManager == null) return;

        // Get current wave information from WaveManager
        int newCurrentWave = waveManager.CurrentWave;
        int enemyCount = GetCurrentEnemyCount();
        int totalEnemies = waveManager.TotalEnemiesInCurrentWave;

        // Update wave number if changed
        if (newCurrentWave != currentWave)
        {
            currentWave = newCurrentWave;
            UpdateWaveText(currentWave);
        }

        // Update enemy count if changed
        if (enemyCount != currentEnemyCount || totalEnemies != totalEnemiesInWave)
        {
            currentEnemyCount = enemyCount;
            totalEnemiesInWave = totalEnemies;
            UpdateEnemyDisplay(currentEnemyCount, totalEnemiesInWave);
        }
    }

    private int GetCurrentEnemyCount()
    {
        // Count all enemies currently in the scene
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies.Length;
    }

    public void UpdateWaveText(int waveNumber)
    {
        if (waveText != null)
        {
            waveText.text = $"WAVE: {waveNumber}";
        }
    }

    public void UpdateEnemyDisplay(int remainingEnemies, int totalEnemies)
    {
        if (frontEnemyBar == null) return;

        // Calculate enemy percentage (inverted - full bar means more enemies)
        float enemyPercentage = totalEnemies > 0 ? (float)remainingEnemies / totalEnemies : 0f;
        
        // Set target fill amount (will be animated in Update)
        frontEnemyBar.fillAmount = enemyPercentage;
        
        

        if (backEnemyBar != null)
        {
            // Back bar shows the "drain" effect

        }
        
        // Update text overlay if available
        if (enemyCountText != null && showEnemyCount)
        {
            if (remainingEnemies == 0 && totalEnemies > 0)
            {
                enemyCountText.text = "<color=green>WAVE CLEARED!</color>";
            }
            else if (totalEnemies == 0)
            {
                enemyCountText.text = "NO ENEMIES";
            }
            else
            {
                enemyCountText.text = $"{remainingEnemies}/{totalEnemies}";
            }
        }
        
        // Reset animation timer for smooth transition
        enemyBarLerpTimer = 0f;
    }

    private void UpdateEnemyBarAnimation()
    {
        if (frontEnemyBar == null || backEnemyBar == null) return;
        
        // Smooth animation similar to health bar
        enemyBarLerpTimer += Time.deltaTime;
        float percentageComplete = enemyBarLerpTimer / enemyBarAnimationSpeed;
        percentageComplete *= percentageComplete; // Squared for smoother transition
        
        // Animate back bar to catch up with front bar
        float frontFill = frontEnemyBar.fillAmount;
        float backFill = backEnemyBar.fillAmount;
        
        if (backFill > frontFill)
        {
            // Enemies were killed - animate back bar down
            backEnemyBar.fillAmount = Mathf.Lerp(backFill, frontFill, percentageComplete);
        }
        else if (backFill < frontFill)
        {
            // More enemies spawned - snap back bar to front bar
            backEnemyBar.fillAmount = frontFill;
        }
    }

    // === PUBLIC METHODS FOR WAVE MANAGER INTEGRATION ===
    
    public void OnWaveStarted(int waveNumber, int totalEnemiesInWave)
    {
        currentWave = waveNumber;
        this.totalEnemiesInWave = totalEnemiesInWave;
        UpdateWaveText(waveNumber);
        UpdateEnemyDisplay(totalEnemiesInWave, totalEnemiesInWave);
    }

    public void OnEnemyKilled()
    {

        int enemyCount = GetCurrentEnemyCount();
        UpdateEnemyDisplay(enemyCount, totalEnemiesInWave);
    }

    public void OnWaveCompleted()
    {
        UpdateEnemyDisplay(0, totalEnemiesInWave);
    }

    // === EXISTING METHODS (keep unchanged) ===

    private void ShowAmmoDisplay()
    {
        // Show ammo bar, hide heat bar
        if (frontAmmoBar != null) frontAmmoBar.transform.parent.gameObject.SetActive(true);
        if (heatBar != null) heatBar.transform.parent.gameObject.SetActive(false);
    }

    private void ShowHeatDisplay()
    {
        // Show heat bar, hide ammo bar
        if (frontAmmoBar != null) frontAmmoBar.transform.parent.gameObject.SetActive(false);
        if (heatBar != null) heatBar.transform.parent.gameObject.SetActive(true);
    }

    public void UpdateText(string promptMessage)
    {
        promptText.text = promptMessage;
    }

    public void UpdateAmmo(int currentAmmo, int maxAmmo)
    {
        if (currentGunStats == null)
        {
            // Fallback to basic display if GunStats not found
            UpdateAmmoDisplay(currentAmmo, maxAmmo);
            return;
        }

        // Display format based on weapon type
        switch (currentGunStats.CurrentWeaponType)
        {
            case GunStats.WeaponType.Pistol:
                UpdateHeatDisplay(currentAmmo, maxAmmo);
                break;
                
            case GunStats.WeaponType.Shotgun:
            case GunStats.WeaponType.SMG:
                UpdateAmmoDisplay(currentAmmo, maxAmmo);
                break;
                
            default:
                // Fallback
                UpdateAmmoDisplay(currentAmmo, maxAmmo);
                break;
        }
    }

    private void UpdateAmmoDisplay(int remainingShots, int totalShots)
    {
        if (frontAmmoBar == null) return;

        // Calculate ammo percentage
        float ammoPercentage = totalShots > 0 ? (float)remainingShots / totalShots : 0f;
        
        // Set target fill amount (will be animated in Update)
        frontAmmoBar.fillAmount = ammoPercentage;
        
        // Update color based on ammo level
        if (remainingShots == 0)
        {
            frontAmmoBar.color = Color.red;
        }
        else if (ammoPercentage <= 0.2f) // 20% or less
        {
            frontAmmoBar.color = Color.red;
        }
        else if (ammoPercentage <= 0.5f) // 50% or less
        {
            frontAmmoBar.color = new Color(1f, 0.6f, 0f); // Orange
        }
        else
        {
            frontAmmoBar.color = Color.white;
        }
        
        // Update text overlay if available
        if (ammoText != null)
        {
            if (remainingShots == 0)
            {
                ammoText.text = "<color=red>NO AMMO</color>";
            }
            else
            {
                ammoText.text = $"{remainingShots}/{totalShots}";
            }
        }
        
        // Reset animation timer for smooth transition
        ammoLerpTimer = 0f;
    }

    private void UpdateHeatDisplay(int coolness, int maxCoolness)
    {
        if (heatBar == null) return;

        // Ensure we're actually dealing with a pistol
        if (currentGunStats == null || currentGunStats.CurrentWeaponType != GunStats.WeaponType.Pistol)
        {
            return; // Don't update if not pistol
        }
        
        // For heat, we want to show heat level (inverse of coolness)
        float heatPercentage = (float)(maxCoolness - coolness) / maxCoolness;
        
        // Set target fill amount (will be animated in Update)
        heatBar.fillAmount = heatPercentage;
        
        // Update color based on heat level
        if (currentGunStats.IsOverheated)
        {
            heatBar.color = Color.red;
        }
        else if (heatPercentage >= 0.8f)
        {
            heatBar.color = new Color(1f, 0.6f, 0f); // Orange
        }
        else if (heatPercentage >= 0.5f)
        {
            heatBar.color = Color.yellow;
        }
        else
        {
            heatBar.color = Color.green;
        }
        
        // Update text overlay if available
        if (heatText != null)
        {
            if (currentGunStats.IsOverheated)
            {
                heatText.text = "<color=red>OVERHEATED</color>";
            }
            else
            {
                heatText.text = $"{heatPercentage:P0}"; // Show as percentage
            }
        }
        
        // Reset animation timer for smooth transition
        heatLerpTimer = 0f;
    }

    private void UpdateAmmoBarAnimation()
    {
        if (frontAmmoBar == null || currentGunStats == null) return;
        
        // Only animate if we're showing ammo (not heat)
        if (currentGunStats.CurrentWeaponType == GunStats.WeaponType.Pistol) return;
        
        // Smooth animation similar to health bar (but simpler since no back bar)
        ammoLerpTimer += Time.deltaTime;
        float percentageComplete = ammoLerpTimer / barAnimationSpeed;
        percentageComplete *= percentageComplete; // Squared for smoother transition
        
        // You can add more sophisticated animation here if needed
        // For now, the fill amount is set directly in UpdateAmmoDisplay()
    }

    private void UpdateHeatBarAnimation()
    {
        if (heatBar == null || currentGunStats == null) return;
        
        // Only animate if we're showing heat (not ammo)
        if (currentGunStats.CurrentWeaponType != GunStats.WeaponType.Pistol) return;
        
        // Smooth animation for heat changes
        heatLerpTimer += Time.deltaTime;
        float percentageComplete = heatLerpTimer / barAnimationSpeed;
        percentageComplete *= percentageComplete; // Squared for smoother transition
        
        // You can add more sophisticated animation here if needed
        // For now, the fill amount is set directly in UpdateHeatDisplay()
    }
}
