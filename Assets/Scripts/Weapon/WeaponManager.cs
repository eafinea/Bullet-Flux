using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Setup")]
    [SerializeField] private Gun pistolGun;           // Always available
    [SerializeField] private Gun smgGun;              // SMG powerup weapon
    [SerializeField] private Gun shotgunGun;          // Shotgun powerup weapon
    
    [Header("Centralized Bullet Effects")]
    [SerializeField] private BulletEffects bulletEffects; // Single bullet effects for all weapons
    
    [Header("Current Weapon State")]
    [SerializeField] private bool isPistolActive = true;
    [SerializeField] private GunStats.WeaponType currentPowerupType = GunStats.WeaponType.SMG;
    [SerializeField] private bool hasPowerupWeapon = false; // Track if player has picked up a powerup
    
    // Events for notifying other systems
    public System.Action<Gun> OnWeaponSwitched;
    public System.Action<BulletEffects> OnBulletEffectsChanged;
    public System.Action<float> OnWeaponPowerupTimeUpdated; // For UI
    public System.Action OnWeaponPowerupExpired;
    public System.Action<GunStats.WeaponType> OnWeaponIconChanged;
    
    // Current active weapon references
    public Gun CurrentGun { get; private set; }
    public GunStats CurrentGunStats { get; private set; }
    public BulletEffects CurrentBulletEffects { get; private set; }
    
    // Weapon GameObjects for enabling/disabling
    [Header("Weapon GameObjects")]
    [SerializeField] private GameObject pistolObject;
    [SerializeField] private GameObject smgObject;
    [SerializeField] private GameObject shotgunObject;
    
    // Store original bullet effects for restoration
    private BulletEffectType originalBulletEffects = BulletEffectType.None;
    private bool hasOriginalEffectsStored = false;
    
    // NEW: Add duration tracking variables
    private Coroutine powerupDurationCoroutine;
    
    private void Awake()
    {
        // Initialize centralized bullet effects
        if (bulletEffects == null)
        {
            bulletEffects = GetComponent<BulletEffects>();
            if (bulletEffects == null)
            {
                Debug.LogWarning("BulletEffects component not found on WeaponManager!");
            }
        }
        
        // Store original bullet effects
        if (bulletEffects != null)
        {
            originalBulletEffects = bulletEffects.CurrentEffects;
            hasOriginalEffectsStored = true;
        }
        
        // Initialize with pistol
        SwitchToPistol();   
    }
    
    private void Start()
    {
        // Ensure only pistol is active at start
        UpdateWeaponVisibility();
        
        // Force initial bullet effects notification
        NotifyBulletEffectsChanged();
    }

    private void Update()
    {
        // Handle pistol heat cooling regardless of active weapon
        if (pistolGun != null)
        {
            var pistolStats = pistolGun.GetComponent<GunStats>();
            if (pistolStats != null && pistolStats.UsesOverheatSystem())
            {
                // Always handle pistol heat cooling, even when not active
                pistolGun.HandleOverheatCooling();
            }
        }
    }
    
    public void SwitchWeapon()
    {
        // Only allow switching if player has a powerup weapon
        if (!hasPowerupWeapon)
        {
            Debug.Log("No powerup weapon available!");
            return;
        }
        
        if (isPistolActive)
        {
            SwitchToPowerup();
        }
        else
        {
            SwitchToPistol();
        }
    }
    
    public void SwitchToPistol()
    {
        // Stop any ongoing overheat on non-pistol weapons before switching
        if (!isPistolActive && CurrentGun != null)
        {
            CurrentGun.ForceStopOverheat();
        }

        isPistolActive = true;
        CurrentGun = pistolGun;
        CurrentGunStats = pistolGun?.GetComponent<GunStats>();
        CurrentBulletEffects = bulletEffects; // Always use centralized bullet effects
        
        UpdateWeaponVisibility();
        NotifyWeaponChanged();
        
        // Force UI update when switching to pistol
        ForceUIUpdate();
        
        Debug.Log("Switched to Pistol");
    }
    
    public void SwitchToPowerup()
    {
        if (!hasPowerupWeapon)
        {
            Debug.Log("No powerup weapon to switch to!");
            return;
        }
        
        // Stop any ongoing overheat on pistol before switching
        if (isPistolActive && pistolGun != null)
        {
            pistolGun.ForceStopOverheat();
        }
        
        isPistolActive = false;
        
        // Select the appropriate powerup gun based on type
        switch (currentPowerupType)
        {
            case GunStats.WeaponType.SMG:
                CurrentGun = smgGun;
                break;
            case GunStats.WeaponType.Shotgun:
                CurrentGun = shotgunGun;
                break;
            default:
                Debug.LogError($"Invalid powerup type: {currentPowerupType}");
                return;
        }
        
        CurrentGunStats = CurrentGun?.GetComponent<GunStats>();
        CurrentBulletEffects = bulletEffects; // Always use centralized bullet effects
        
        UpdateWeaponVisibility();
        NotifyWeaponChanged();
        
        // Force UI update when switching to powerup
        ForceUIUpdate();
        
        Debug.Log($"Switched to {currentPowerupType}");
    }
    
    // NEW METHODS FOR POWERUP SYSTEM
    
    // Method to setup timed weapon powerup
    public void SetupTimedWeaponPowerup(GunStats.WeaponType weaponType, int shotCount)
    {
        currentPowerupType = weaponType;
        hasPowerupWeapon = true;

        Gun targetGun = weaponType == GunStats.WeaponType.SMG ? smgGun : shotgunGun;
        var gunStats = targetGun?.GetComponent<GunStats>();
        if (gunStats != null)
        {
            gunStats.SetWeaponType(weaponType);
            // Set the shot count
            if (gunStats.UsesFiniteShots())
            {
                gunStats.AddFiniteShots(shotCount - gunStats.RemainingShots);
            }
        }
        
        Debug.Log($"Setup {weaponType} powerup with {shotCount} shots!");
    }
    
    // Method to setup timed weapon powerup WITH duration support
    public void SetupTimedWeaponPowerup(GunStats.WeaponType weaponType, int shotCount, float duration = 30f)
    {
        currentPowerupType = weaponType;
        hasPowerupWeapon = true;

        Gun targetGun = weaponType == GunStats.WeaponType.SMG ? smgGun : shotgunGun;
        var gunStats = targetGun?.GetComponent<GunStats>();
        if (gunStats != null)
        {
            gunStats.SetWeaponType(weaponType);
            // Set the shot count
            if (gunStats.UsesFiniteShots())
            {
                gunStats.AddFiniteShots(shotCount - gunStats.RemainingShots);
            }
        }
        
        // Start duration timer in WeaponManager
        if (powerupDurationCoroutine != null)
        {
            StopCoroutine(powerupDurationCoroutine);
        }
        powerupDurationCoroutine = StartCoroutine(PowerupDurationTimer(duration));
        
        // Auto-switch to the new powerup weapon immediately
        if (!isPistolActive || (isPistolActive && hasPowerupWeapon))
        {
            // Force switch to the new powerup weapon
            SwitchToPowerup();
            Debug.Log($"Auto-switched to {weaponType} powerup with {shotCount} shots for {duration} seconds!");
        }
        else
        {
            Debug.Log($"Setup {weaponType} powerup with {shotCount} shots for {duration} seconds!");
        }
    }
    
    // Method to expire weapon powerup
    public void ExpireWeaponPowerup()
    {
        Debug.Log("Weapon powerup expired!");
        
        // Stop duration timer
        if (powerupDurationCoroutine != null)
        {
            StopCoroutine(powerupDurationCoroutine);
            powerupDurationCoroutine = null;
        }
        
        hasPowerupWeapon = false;
        
        // Switch back to pistol if using powerup weapon
        if (!isPistolActive)
        {
            SwitchToPistol();
        }
        
        // Notify systems
        OnWeaponPowerupExpired?.Invoke();
        OnWeaponPowerupTimeUpdated?.Invoke(0f);
    }
    
    // Method to check if ammo depleted
    public bool IsWeaponAmmoDepleted()
    {
        if (!hasPowerupWeapon || isPistolActive || CurrentGunStats == null)
            return false;
            
        return CurrentGunStats.UsesFiniteShots() && CurrentGunStats.RemainingShots <= 0;
    }
    
    // Method to apply bullet effect
    public void ApplyBulletEffect(BulletEffectType effect)
    {
        if (bulletEffects == null) return;
        
        // Store original effects if this is the first effect applied
        if (!hasOriginalEffectsStored)
        {
            originalBulletEffects = bulletEffects.CurrentEffects;
            hasOriginalEffectsStored = true;
        }
        
        bulletEffects.AddEffect(effect);
        NotifyBulletEffectsChanged();
        
        Debug.Log($"Applied bullet effect: {effect}. Current effects: {bulletEffects.CurrentEffects}");
    }
    
    // Method to apply bullet effect WITH duration support
    public void ApplyBulletEffect(BulletEffectType effect, float duration = 15f)
    {
        if (bulletEffects == null) return;
        
        // Store original effects if this is the first effect applied
        if (!hasOriginalEffectsStored)
        {
            originalBulletEffects = bulletEffects.CurrentEffects;
            hasOriginalEffectsStored = true;
        }
        
        bulletEffects.AddEffect(effect);
        NotifyBulletEffectsChanged();
        
        StartCoroutine(BulletEffectTimer(effect, duration));
        
        Debug.Log($"Applied bullet effect: {effect} for {duration} seconds. Current effects: {bulletEffects.CurrentEffects}");
    }
    
    private IEnumerator BulletEffectTimer(BulletEffectType effect, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        // Remove the specific effect
        RemoveBulletEffect(effect);
        
        Debug.Log($"Bullet effect {effect} expired after {duration} seconds");
    }
    
    // Method to remove bullet effect
    public void RemoveBulletEffect(BulletEffectType effect)
    {
        if (bulletEffects == null) return;
        
        // Remove the specific effect
        var currentEffects = bulletEffects.CurrentEffects;
        var newEffects = currentEffects & ~effect; // Remove the specific effect
        
        bulletEffects.SetEnabledEffects(newEffects);
        NotifyBulletEffectsChanged();
        
        Debug.Log($"Removed bullet effect: {effect}. Remaining effects: {newEffects}");
    }
    
    // Method to restore original bullet effects
    public void RestoreOriginalBulletEffects()
    {
        if (bulletEffects == null || !hasOriginalEffectsStored) return;
        
        bulletEffects.SetEnabledEffects(originalBulletEffects);
        NotifyBulletEffectsChanged();
        
        Debug.Log($"Restored original bullet effects: {originalBulletEffects}");
    }
    
    // EXISTING METHODS (keep these for compatibility)
    
    public void PickupPowerupWeapon(GunStats.WeaponType weaponType)
    {
        currentPowerupType = weaponType;
        hasPowerupWeapon = true;

        Gun targetGun = weaponType == GunStats.WeaponType.SMG ? smgGun : shotgunGun;
        var gunStats = targetGun?.GetComponent<GunStats>();
        if (gunStats != null)
        {
            gunStats.SetWeaponType(weaponType);
        }
        
        Debug.Log($"Picked up {weaponType} powerup! Press switch key to use it.");
    }
    
    public void SetBulletEffects(BulletEffectType effects)
    {
        if (bulletEffects != null)
        {
            bulletEffects.SetEnabledEffects(effects);
            NotifyBulletEffectsChanged();
        }
    }
    
    private void UpdateWeaponVisibility()
    {
        // Enable/disable weapon GameObjects
        if (pistolObject != null) pistolObject.SetActive(isPistolActive);
        
        if (isPistolActive)
        {
            // Disable both powerup weapons
            if (smgObject != null) smgObject.SetActive(false);
            if (shotgunObject != null) shotgunObject.SetActive(false);
        }
        else
        {
            // Enable the appropriate powerup weapon
            switch (currentPowerupType)
            {
                case GunStats.WeaponType.SMG:
                    if (smgObject != null) smgObject.SetActive(true);
                    if (shotgunObject != null) shotgunObject.SetActive(false);
                    break;
                    
                case GunStats.WeaponType.Shotgun:
                    if (smgObject != null) smgObject.SetActive(false);
                    if (shotgunObject != null) shotgunObject.SetActive(true);
                    break;
            }
        }
    }
    
    private void NotifyWeaponChanged()
    {
        // Notify other systems about the weapon change
        OnWeaponSwitched?.Invoke(CurrentGun);
        
           OnWeaponIconChanged?.Invoke(CurrentWeaponType);
        
        // Don't notify bullet effects here since they don't change with weapon switch
    }
    
    private void NotifyBulletEffectsChanged()
    {
        // Notify systems about bullet effects (for initial setup and when effects change)
        OnBulletEffectsChanged?.Invoke(CurrentBulletEffects);
    }
    
    // Getters for current weapon info
    public bool IsPistolActive => isPistolActive;
    public GunStats.WeaponType CurrentWeaponType => isPistolActive ? GunStats.WeaponType.Pistol : currentPowerupType;
    public bool HasPowerupWeapon => hasPowerupWeapon;
    
    // Public method to get bullet effects (for external access)
    public BulletEffects GetBulletEffects() => bulletEffects;

    // Add method to force UI update with correct weapon stats
    public void ForceUIUpdate()
    {
        if (CurrentGun == null || CurrentGunStats == null) return;
        
        // Get PlayerUI and force update with current weapon's stats
        var playerUI = GetComponentInParent<PlayerUI>();
        if (playerUI == null)
            playerUI = transform.root.GetComponent<PlayerUI>();
        
        if (playerUI != null)
        {
            switch (CurrentGunStats.CurrentWeaponType)
            {
                case GunStats.WeaponType.Pistol:
                    int coolness = Mathf.RoundToInt(((CurrentGunStats.MaxHeat - CurrentGunStats.CurrentHeat) / CurrentGunStats.MaxHeat) * 100);
                    playerUI.UpdateAmmo(coolness, 100);
                    break;
                    
                case GunStats.WeaponType.Shotgun:
                case GunStats.WeaponType.SMG:
                    playerUI.UpdateAmmo(CurrentGunStats.RemainingShots, CurrentGunStats.TotalShots);
                    break;
            }
        }
    }
    
    // NEW: Duration timer in WeaponManager
    private IEnumerator PowerupDurationTimer(float duration)
    {
        float timeRemaining = duration;
        
        while (timeRemaining > 0 && hasPowerupWeapon)
        {
            // Notify time updates
            OnWeaponPowerupTimeUpdated?.Invoke(timeRemaining);
            
            // Check for ammo depletion
            if (IsWeaponAmmoDepleted())
            {
                Debug.Log("Weapon powerup expired: ammo depleted!");
                yield return new WaitForSeconds(1f);
                ExpireWeaponPowerup();
                yield break;
            }
            
            // Warning when time is low
            if (timeRemaining <= 5f && timeRemaining > 4f)
            {
                Debug.Log("Weapon powerup expiring soon!");
            }
            
            timeRemaining -= Time.deltaTime;
            yield return null;
        }
        
        // Time expired
        if (hasPowerupWeapon)
        {
            Debug.Log("Weapon powerup expired: time limit reached!");
            ExpireWeaponPowerup();
        }
    }
}
