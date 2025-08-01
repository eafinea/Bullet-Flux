using System;
using System.Collections;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Weapon Management")]
    [SerializeField] private WeaponManager weaponManager;
    
    [Header("Weapon Switch Settings")]
    [SerializeField] private float weaponSwitchDelay = 0.5f; // Adjustable delay after weapon switch
    
    private InputManager inputManager;
    private PlayerUI playerUI;
    private float lastBurstTime = 0f;
    private bool isFiring = false;
    
    // Weapon switch timing
    private float lastWeaponSwitchTime = 0f;

    // Dynamic references that update when weapons switch
    private Gun currentGun;
    private GunStats currentGunStats;

    private void Awake()
    {
        playerUI = GetComponent<PlayerUI>();
        
        // Get WeaponManager if not assigned
        if (weaponManager == null)
        {
            weaponManager = GetComponentInChildren<WeaponManager>();
        }
        
        if (weaponManager == null)
        {
            Debug.LogError("WeaponManager not found! Please assign it in the inspector.");
            return;
        }
        
        // Subscribe to weapon change events
        weaponManager.OnWeaponSwitched += OnWeaponChanged;
        
        // Initialize with current weapon
        OnWeaponChanged(weaponManager.CurrentGun);
    }

    void Start()
    {
        inputManager = GetComponent<InputManager>();
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
        currentGun = newGun;
        currentGunStats = weaponManager.CurrentGunStats;
        
        // Record the time of weapon switch
        lastWeaponSwitchTime = Time.time;
        
        // Force stop any ongoing firing when weapon changes
        StopAllFiring();
        
        if (currentGun == null || currentGunStats == null)
        {
            Debug.LogError("Invalid weapon references after weapon switch!");
        }
        
        Debug.Log($"PlayerShoot updated to use {currentGunStats?.CurrentWeaponType} - Shooting disabled for {weaponSwitchDelay}s");
    }

    void Update()
    {
        if (currentGunStats == null) return;
        
        // Check if enough time has passed since last weapon switch
        bool canShoot = Time.time >= lastWeaponSwitchTime + weaponSwitchDelay;
        
        // Handle weapon switching
        if (inputManager.onFootActions.SwitchWeapon.triggered)
        {
            weaponManager.SwitchWeapon();
            return; // Exit early to prevent shooting on the same frame as switching
        }
        
        // Only process shooting if we can shoot
        if (!canShoot)
        {
            // Show remaining delay time for debugging (optional)
            float remainingDelay = (lastWeaponSwitchTime + weaponSwitchDelay) - Time.time;
            if (remainingDelay > 0)
            {
                // Optional: You could display this delay to the player via UI
                // Debug.Log($"Weapon switch cooldown: {remainingDelay:F1}s remaining");
            }
            return;
        }
        
        // Handle shooting based on current weapon
        switch (currentGunStats.CurrentShootingMode)
        {
            case GunStats.ShootingMode.SemiAuto:
                if (inputManager.onFootActions.Shoot.triggered)
                {
                    currentGun.Shoot();
                }
                break;
                
            case GunStats.ShootingMode.Burst:
                if (inputManager.onFootActions.Shoot.triggered)
                {
                    StartCoroutine(BurstFire());
                }
                break;
                
            case GunStats.ShootingMode.FullAuto:
                if (inputManager.onFootActions.Shoot.ReadValue<float>() > 0)
                {
                    if (!isFiring)
                        StartCoroutine(AutoFire());
                }
                else
                {
                    isFiring = false;
                }
                break;
        }
    }

    private void StopAllFiring()
    {
        // Force stop all firing states
        isFiring = false;
        
        // Stop any ongoing coroutines
        StopAllCoroutines();
        
        Debug.Log("All firing stopped due to weapon switch");
    }

    private IEnumerator BurstFire()
    {
        if (Time.time < lastBurstTime + currentGunStats.BurstCooldown)
            yield break;

        lastBurstTime = Time.time;
        int burstCount = 3;
        for (int i = 0; i < burstCount; i++)
        {
            // Check if we're still allowed to shoot (in case weapon switched mid-burst)
            if (Time.time < lastWeaponSwitchTime + weaponSwitchDelay)
            {
                Debug.Log("Burst fire interrupted by weapon switch");
                yield break;
            }
            
            currentGun.Shoot();
            yield return new WaitForSeconds(currentGunStats.FireRate);
        }
    }

    private IEnumerator AutoFire()
    {
        isFiring = true;
        while (inputManager.onFootActions.Shoot.ReadValue<float>() > 0 && isFiring)
        {
            // Check if we're still allowed to shoot (in case weapon switched mid-auto)
            if (Time.time < lastWeaponSwitchTime + weaponSwitchDelay)
            {
                Debug.Log("Auto fire interrupted by weapon switch");
                isFiring = false;
                yield break;
            }
            
            currentGun.Shoot();
            yield return new WaitForSeconds(currentGunStats.FireRate);
        }
        isFiring = false;
    }

    public void GetAmmo()
    {
        if (currentGunStats == null) return;
        
        // Different ammo pickup behavior based on weapon type
        switch (currentGunStats.CurrentWeaponType)
        {
            case GunStats.WeaponType.Pistol:
                Debug.Log("Pistol has infinite ammo - no pickup needed!");
                return;
                
            case GunStats.WeaponType.Shotgun:
            case GunStats.WeaponType.SMG:
                int shotsToAdd = 20;
                currentGunStats.AddFiniteShots(shotsToAdd);
                Debug.Log($"Shots picked up! Remaining: {currentGunStats.RemainingShots}/{currentGunStats.TotalShots}");
                playerUI.UpdateAmmo(currentGunStats.RemainingShots, currentGunStats.TotalShots);
                break;
        }
    }

    // Public method to get current weapon switch delay (for UI or other systems)
    public float WeaponSwitchDelay => weaponSwitchDelay;
    
    // Public method to check if currently in weapon switch cooldown
    public bool IsInWeaponSwitchCooldown => Time.time < lastWeaponSwitchTime + weaponSwitchDelay;
    
    // Public method to get remaining cooldown time
    public float RemainingWeaponSwitchCooldown => Mathf.Max(0f, (lastWeaponSwitchTime + weaponSwitchDelay) - Time.time);
}
