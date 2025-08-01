using UnityEngine;

[System.Serializable]
public class GunStats : MonoBehaviour
{
    public enum WeaponType 
    { 
        Pistol,     // Infinite ammo + overheat
        Shotgun,    // Finite shots, no reload
        SMG         // Finite shots, no reload
    }

    public enum ShootingMode { SemiAuto, FullAuto, Burst }
    //Weapon Type
    [SerializeField] private WeaponType weaponType = WeaponType.Pistol;

    // General weapon stats
    [SerializeField] private float damage = 10f;
    [SerializeField] private float fireRate = 0.5f;

    // Overheat system properties
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float currentHeat = 0f;
    [SerializeField] private float heatPerShot = 15f;
    [SerializeField] private float cooldownRate = 25f; // Heat lost per second
    [SerializeField] private float overheatedCooldownTime = 3f; // Time before cooling starts

    // Finite shot properties
    [SerializeField] private int totalShots = 50; // Total shots available
    [SerializeField] private int remainingShots = 50;

    // Shotgun properties
    [SerializeField] private int shotgunPellets = 8;
    [SerializeField] private float fireSpread = 0.1f;

    // Shooting mode properties
    [SerializeField] private ShootingMode shootingMode = ShootingMode.SemiAuto;
    [SerializeField] private float burstCooldown = 1f;

    // Properties
    public WeaponType CurrentWeaponType => weaponType;
    public float Damage => damage;
    public float FireRate => fireRate;

    // Overheat properties
    public float MaxHeat => maxHeat;
    public float CurrentHeat => currentHeat;
    public float HeatPerShot => heatPerShot;
    public float CooldownRate => cooldownRate;
    public float OverheatedCooldownTime => overheatedCooldownTime;
    public bool IsOverheated => weaponType == WeaponType.Pistol && currentHeat >= maxHeat;

    // Finite shot properties
    public int TotalShots => totalShots;
    public int RemainingShots => remainingShots;

    // Shotgun properties
    public int ShotgunPellets => shotgunPellets;
    public float FireSpread => fireSpread;

    // Shooting mode properties
    public float BurstCooldown => burstCooldown;
    public ShootingMode CurrentShootingMode => shootingMode;

    // Legacy properties for backward compatibility
    public bool IsShotgun => weaponType == WeaponType.Shotgun;

    // Heat management methods
    public void AddHeat(float amount)
    {
        if (weaponType == WeaponType.Pistol)
        {
            currentHeat = Mathf.Clamp(currentHeat + amount, 0f, maxHeat);
        }
    }

    public void CoolDown(float amount)
    {
        if (weaponType == WeaponType.Pistol)
        {
            currentHeat = Mathf.Max(currentHeat - amount, 0f);
        }
    }

    public void SetHeat(float heatValue)
    {
        if (weaponType == WeaponType.Pistol)
        {
            currentHeat = Mathf.Clamp(heatValue, 0f, maxHeat);
        }
    }

    public void ConsumeFiniteShot()
    {
        remainingShots = Mathf.Max(remainingShots - 1, 0);
    }

    public void AddFiniteShots(int amount)
    {
        if (UsesFiniteShots())
        {
            remainingShots = Mathf.Min(remainingShots + amount, totalShots);
            Debug.Log($"Added {amount} shots. Remaining: {remainingShots}/{totalShots}");
        }
    }

    public bool IsOutOfAmmo()
    {
        return UsesFiniteShots() && remainingShots <= 0;
    }

    // Helper methods
    public bool UsesOverheatSystem() => weaponType == WeaponType.Pistol;
    public bool UsesFiniteShots() => weaponType == WeaponType.Shotgun || weaponType == WeaponType.SMG;
    public bool UsesPellets() => weaponType == WeaponType.Shotgun;

    // Method to set weapon type and reset stats
    public void SetWeaponType(WeaponType newWeaponType)
    {
        weaponType = newWeaponType;
        
        // Reset/initialize stats based on weapon type
        switch (weaponType)
        {
            case WeaponType.Pistol:
                // Reset pistol stats if needed
                currentHeat = 0f;
                break;
                
            case WeaponType.SMG:
                // Initialize SMG with appropriate values
                remainingShots = totalShots;
                Debug.Log($"SMG initialized with {remainingShots} shots");
                break;
                
            case WeaponType.Shotgun:
                // Initialize Shotgun with appropriate values
                remainingShots = totalShots;
                Debug.Log($"Shotgun initialized with {remainingShots} shots");
                break;
        }
        
        Debug.Log($"Weapon type changed to {weaponType}");
    }

    // Enhanced method to reset overheat state when switching weapons
    public void ResetOverheatState()
    {
        if (weaponType == WeaponType.Pistol)
        {
            // Don't reset heat completely, but ensure we're not stuck in overheated state
            Debug.Log("Overheat state reset for pistol");
        }
    }

    // Method to check if weapon can shoot (considering overheat)
    public bool CanShoot()
    {
        switch (weaponType)
        {
            case WeaponType.Pistol:
                return !IsOverheated;
                
            case WeaponType.Shotgun:
            case WeaponType.SMG:
                return remainingShots > 0;
                
            default:
                return true;
        }
    }
}
