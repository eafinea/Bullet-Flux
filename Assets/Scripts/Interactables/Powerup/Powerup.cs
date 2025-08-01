using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public enum PowerupType
{
    ShotgunWeapon,
    SMGWeapon,
    BulletEffect
}

public class Powerup : MonoBehaviour
{
    [Header("Powerup Configuration")]
    [SerializeField] private PowerupType powerupType = PowerupType.BulletEffect;
    
    [Header("Weapon Powerup Settings")]
    [SerializeField] private GunStats.WeaponType weaponType = GunStats.WeaponType.Shotgun;
    [SerializeField] private int shotCount = 30;
    [SerializeField] private float weaponDuration = 30f;
    
    [Header("Bullet Effect Settings")]
    [SerializeField] private bool randomizeEffect = true;
    [SerializeField] private BulletEffectType specificEffect = BulletEffectType.Bounce;
    [SerializeField] private float effectDuration = 15f;
    
    [Header("Reusability Settings")]
    [SerializeField] private bool isReusable = true;
    [SerializeField] private float pickupCooldown = 5f;
    [SerializeField] private bool destroyAfterPickup = false;
    
    [Header("Expiration Settings")]
    [SerializeField] private bool enableExpiration = true;
    [SerializeField] private float expirationTime = 60f;
    [SerializeField] private bool onlyExpireIfNotReusable = true;
    [SerializeField] private float warningTime = 10f;
    
    [Header("Visual & Audio")]
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.5f;
    
    [Header("Expiration Visual Effects")]
    [SerializeField] private bool enableWarningEffects = true;
    [SerializeField] private float blinkSpeed = 3f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Renderer powerupRenderer;
    [SerializeField] private Collider powerupCollider;
    
    [Header("Debug")]
    [SerializeField] private bool debugRendererSearch = false;
    
    private static readonly BulletEffectType[] availableEffects = 
    {
        BulletEffectType.Bounce,
        BulletEffectType.DamageOverTime,
        BulletEffectType.Slow
    };
    
    private Vector3 startPosition;
    private bool collected = false;
    private bool onCooldown = false;
    
    // Individual timing offsets to prevent synchronized movement
    private float timeOffset;
    private float bobOffset;
    
    // Powerup management variables
    private WeaponManager weaponManager;
    private Coroutine weaponPowerupCoroutine;
    private Coroutine pickupCooldownCoroutine;
    private Coroutine expirationCoroutine;
    private Coroutine warningEffectsCoroutine;
    private Dictionary<BulletEffectType, Coroutine> activeEffectCoroutines = new Dictionary<BulletEffectType, Coroutine>();
    
    // Expiration tracking
    private bool isExpiring = false;
    private bool hasExpired = false;
    private float spawnTime;
    
    // Enhanced renderer detection
    private Renderer[] allRenderers;

    public PowerupType GetPowerupType()
    {
        return powerupType;
    }

    public void ConfigureAsEnemyDrop()
    {
        // Configure for enemy drops - single use, destroy on pickup
        ConfigureReusability(
            reusable: false,
            cooldown: 0f,
            destroyOnPickup: true
        );
        
        ConfigureExpiration(
            enableExpiration: true,
            expirationTime: 45f,
            warningTime: 10f,
            onlyExpireIfNotReusable: true
        );
        
        // Reduce duration and shot count for enemy drops
        switch (powerupType)
        {
            case PowerupType.ShotgunWeapon:
                weaponDuration = 20f;
                shotCount = 15;
                break;
                
            case PowerupType.SMGWeapon:
                weaponDuration = 25f;
                shotCount = 25;
                break;
                
            case PowerupType.BulletEffect:
                effectDuration = 10f;
                break;
        }
    }

    // Enhanced Start method with improved renderer detection
    private void Start()
    {
        // Record spawn time for expiration tracking
        spawnTime = Time.time;
        
        // Find proper ground position
        PositionOnGround();
        
        startPosition = transform.position;
        
        // Add random offsets to prevent synchronized movement between different powerups
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        bobOffset = Random.Range(0f, 2f * Mathf.PI);
        
        // Enhanced renderer detection
        FindRenderers();
        
        // Auto-assign collider if not set
        if (powerupCollider == null)
            powerupCollider = GetComponent<Collider>();
        
        // Auto-detect powerup type based on tag if not manually set
        AutoDetectPowerupType();
        
        // Start expiration timer if enabled
        StartExpirationTimer();
    }

    private void FindRenderers()
    {
        // Try to find renderer in multiple ways
        if (powerupRenderer == null)
        {
            // 1. Try to get renderer on this GameObject
            powerupRenderer = GetComponent<Renderer>();
            
            if (debugRendererSearch)
                Debug.Log($"[Powerup] {name}: Direct renderer search result: {(powerupRenderer != null ? "Found" : "Not found")}");
        }
        
        if (powerupRenderer == null)
        {
            // 2. Try to get renderer from children
            powerupRenderer = GetComponentInChildren<Renderer>();
            
            if (debugRendererSearch)
                Debug.Log($"[Powerup] {name}: Child renderer search result: {(powerupRenderer != null ? "Found" : "Not found")}");
        }
        
        // 3. Get all renderers for comprehensive blinking
        allRenderers = GetComponentsInChildren<Renderer>();
        
        if (debugRendererSearch)
        {
            Debug.Log($"[Powerup] {name}: Found {allRenderers.Length} total renderers");
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Debug.Log($"[Powerup] {name}: Renderer {i}: {allRenderers[i].name} on {allRenderers[i].gameObject.name}");
            }
        }
        
        // Use the first renderer we found as the primary one
        if (powerupRenderer == null && allRenderers.Length > 0)
        {
            powerupRenderer = allRenderers[0];
            if (debugRendererSearch)
                Debug.Log($"[Powerup] {name}: Using first renderer as primary: {powerupRenderer.name}");
        }
        
        // Final check
        if (powerupRenderer == null)
        {
            Debug.LogWarning($"[Powerup] {name}: No Renderer found! Blinking effects will not work. " +
                           $"Make sure the powerup or its children have a Renderer component.");
        }
        else
        {
            if (debugRendererSearch)
                Debug.Log($"[Powerup] {name}: Successfully found renderer: {powerupRenderer.name}");
        }
    }

    private void PositionOnGround()
    {
        // Raycast down to find ground
        Vector3 rayStart = transform.position + Vector3.up * 5f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f))
        {
            // Position slightly above the ground
            transform.position = hit.point + Vector3.up * 0.2f;
        }
        else
        {
            // Fallback - just ensure we're not underground
            Vector3 pos = transform.position;
            pos.y = Mathf.Max(pos.y, 0.2f);
            transform.position = pos;
        }
    }

    private void Update()
    {
        if (!collected && !onCooldown && !hasExpired)
        {
            // Rotate powerup with individual timing
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
            
            // Bob up and down with individual timing
            float bobTime = Time.time * bobSpeed + bobOffset;
            float newY = startPosition.y + Mathf.Sin(bobTime) * bobHeight;
            
            // Ensure we never go below ground
            newY = Mathf.Max(newY, 0.1f);
            
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !collected && !onCooldown && !hasExpired)
        {
            // Apply powerup effect
            if (ApplyPowerup(other.gameObject))
            {
                // Stop expiration timer when picked up
                StopExpirationTimer();
                
                // Play pickup effects
                PlayPickupEffects();
                
                // Handle powerup state after successful pickup
                HandlePostPickup();
            }
        }
    }

    private void HandlePostPickup()
    {
        if (destroyAfterPickup)
        {
            Destroy(gameObject);
        }
        else if (isReusable)
        {
            StartPickupCooldown();
        }
        else
        {
            collected = true;
            DisablePowerupVisuals();
        }
    }

    // Expiration system
    private void StartExpirationTimer()
    {
        if (!enableExpiration) return;
        
        // Only start expiration timer for non-reusable powerups if the setting is enabled
        if (onlyExpireIfNotReusable && isReusable) return;
        
        if (expirationCoroutine != null)
        {
            StopCoroutine(expirationCoroutine);
        }
        
        expirationCoroutine = StartCoroutine(ExpirationTimer());
    }

    private void StopExpirationTimer()
    {
        if (expirationCoroutine != null)
        {
            StopCoroutine(expirationCoroutine);
            expirationCoroutine = null;
        }
        
        // Also stop warning effects
        StopWarningEffects();
    }

    private IEnumerator ExpirationTimer()
    {
        float timeUntilWarning = expirationTime - warningTime;
        
        // Wait until warning time
        if (timeUntilWarning > 0)
        {
            yield return new WaitForSeconds(timeUntilWarning);
        }
        
        // Start warning effects (just blinking)
        if (enableWarningEffects && warningTime > 0)
        {
            StartWarningEffects();
            
            // Wait for remaining time until expiration
            yield return new WaitForSeconds(warningTime);
        }
        else
        {
            // No warning effects, just wait until expiration
            yield return new WaitForSeconds(expirationTime);
        }
        
        // Expire the powerup
        ExpirePowerup();
    }

    private void StartWarningEffects()
    {
        isExpiring = true;
        
        if (warningEffectsCoroutine != null)
        {
            StopCoroutine(warningEffectsCoroutine);
        }
        
        warningEffectsCoroutine = StartCoroutine(WarningEffects());
    }

    private void StopWarningEffects()
    {
        isExpiring = false;
        
        if (warningEffectsCoroutine != null)
        {
            StopCoroutine(warningEffectsCoroutine);
            warningEffectsCoroutine = null;
        }
        
        // Ensure all renderers are enabled after warning
        SetRenderersEnabled(true);
    }

    private IEnumerator WarningEffects()
    {
        float blinkInterval = 1f / blinkSpeed;
        bool renderersEnabled = true;
        
        while (isExpiring && !hasExpired)
        {
            // Toggle all renderers visibility to create blinking effect
            renderersEnabled = !renderersEnabled;
            SetRenderersEnabled(renderersEnabled);
            
            yield return new WaitForSeconds(blinkInterval / 2f);
        }
        
        // Ensure renderers are enabled after warning (if not expired)
        if (!hasExpired)
        {
            SetRenderersEnabled(true);
        }
        
        warningEffectsCoroutine = null;
    }

    private void SetRenderersEnabled(bool enabled)
    {
        // Set primary renderer
        if (powerupRenderer != null)
        {
            powerupRenderer.enabled = enabled;
        }
        
        // Set all child renderers for comprehensive blinking
        if (allRenderers != null)
        {
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null)
                {
                    allRenderers[i].enabled = enabled;
                }
            }
        }
    }

    private void ExpirePowerup()
    {
        if (hasExpired) return;
        
        hasExpired = true;
        isExpiring = false;
        
        // Stop all coroutines
        StopExpirationTimer();
        
        // No expiration effects - just disable and destroy
        DisablePowerupVisuals();
        
        // Destroy the powerup
        Destroy(gameObject);
    }

    private void StartPickupCooldown()
    {
        if (pickupCooldownCoroutine != null)
        {
            StopCoroutine(pickupCooldownCoroutine);
        }
        
        pickupCooldownCoroutine = StartCoroutine(PickupCooldownTimer());
    }

    private IEnumerator PickupCooldownTimer()
    {
        onCooldown = true;
        collected = true;
        
        // Visual feedback for cooldown
        DisablePowerupVisuals();
        
        yield return new WaitForSeconds(pickupCooldown);
        
        // Re-enable the powerup
        onCooldown = false;
        collected = false;
        EnablePowerupVisuals();
        
        // Restart expiration timer for reusable powerups if needed
        if (isReusable && enableExpiration && !onlyExpireIfNotReusable)
        {
            StartExpirationTimer();
        }
        
        pickupCooldownCoroutine = null;
    }

    private void DisablePowerupVisuals()
    {
        SetRenderersEnabled(false);
        
        if (powerupCollider != null)
            powerupCollider.enabled = false;
    }

    private void EnablePowerupVisuals()
    {
        SetRenderersEnabled(true);
        
        if (powerupCollider != null)
            powerupCollider.enabled = true;
    }

    private void AutoDetectPowerupType()
    {
        // Auto-detect based on GameObject tag
        switch (gameObject.tag)
        {
            case "Shotgun_Pickup":
                powerupType = PowerupType.ShotgunWeapon;
                weaponType = GunStats.WeaponType.Shotgun;
                break;
                
            case "SMG_Pickup":
                powerupType = PowerupType.SMGWeapon;
                weaponType = GunStats.WeaponType.SMG;
                break;
                
            case "Powerup_Pickup":
                powerupType = PowerupType.BulletEffect;
                break;
        }
    }

    private bool ApplyPowerup(GameObject player)
    {
        weaponManager = player.GetComponentInChildren<WeaponManager>();
        
        if (weaponManager == null)
        {
            Debug.LogError("WeaponManager not found on player!");
            return false;
        }

        switch (powerupType)
        {
            case PowerupType.ShotgunWeapon:
                return ApplyWeaponPowerup(GunStats.WeaponType.Shotgun);
                
            case PowerupType.SMGWeapon:
                return ApplyWeaponPowerup(GunStats.WeaponType.SMG);
                
            case PowerupType.BulletEffect:
                return ApplyBulletEffectPowerup();
                
            default:
                Debug.LogError($"Unknown powerup type: {powerupType}");
                return false;
        }
    }

    private bool ApplyWeaponPowerup(GunStats.WeaponType weaponType)
    {
        try
        {
            // Check if player already has a weapon powerup active
            if (weaponManager.HasPowerupWeapon)
            {
                // If same weapon type, refresh ammo and extend duration
                if (weaponManager.CurrentWeaponType == weaponType)
                {
                    // Add more shots to existing weapon
                    if (weaponManager.CurrentGunStats != null && weaponManager.CurrentGunStats.UsesFiniteShots())
                    {
                        weaponManager.CurrentGunStats.AddFiniteShots(shotCount);
                    }
                    
                    // Always call SetupTimedWeaponPowerup to trigger auto-switch
                    weaponManager.SetupTimedWeaponPowerup(weaponType, weaponManager.CurrentGunStats.RemainingShots, weaponDuration);
                    return true;
                }
            }
            
            //  Always call SetupTimedWeaponPowerup which now handles auto-switching
            weaponManager.SetupTimedWeaponPowerup(weaponType, shotCount, weaponDuration);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to apply weapon powerup: {e.Message}");
            return false;
        }
    }

    private bool ApplyBulletEffectPowerup()
    {
        try
        {
            // Determine which effect to apply
            BulletEffectType effectToApply;
            if (randomizeEffect)
            {
                effectToApply = availableEffects[Random.Range(0, availableEffects.Length)];
            }
            else
            {
                effectToApply = specificEffect;
            }

            weaponManager.ApplyBulletEffect(effectToApply, effectDuration);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to apply bullet effect powerup: {e.Message}");
            return false;
        }
    }

    private void PlayPickupEffects()
    {
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, transform.rotation);
        }
        
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
    }

    // Configuration methods
    public void ConfigureWeaponPowerup(GunStats.WeaponType weaponType, int shots, float duration)
    {
        this.powerupType = weaponType == GunStats.WeaponType.Shotgun ? PowerupType.ShotgunWeapon : PowerupType.SMGWeapon;
        this.weaponType = weaponType;
        this.shotCount = shots;
        this.weaponDuration = duration;
    }

    public void ConfigureBulletEffectPowerup(BulletEffectType effect, float duration, bool randomize = false)
    {
        this.powerupType = PowerupType.BulletEffect;
        this.specificEffect = effect;
        this.effectDuration = duration;
        this.randomizeEffect = randomize;
    }

    public void ConfigureReusability(bool reusable, float cooldown, bool destroyOnPickup = false)
    {
        this.isReusable = reusable;
        this.pickupCooldown = cooldown;
        this.destroyAfterPickup = destroyOnPickup;
    }

    public void ConfigureExpiration(bool enableExpiration, float expirationTime, float warningTime = 10f, bool onlyExpireIfNotReusable = true)
    {
        this.enableExpiration = enableExpiration;
        this.expirationTime = expirationTime;
        this.warningTime = warningTime;
        this.onlyExpireIfNotReusable = onlyExpireIfNotReusable;
        
        // Restart expiration timer with new settings if already running
        if (expirationCoroutine != null)
        {
            StartExpirationTimer();
        }
    }

    // Testing and utility methods
    public void ResetPowerup()
    {
        if (pickupCooldownCoroutine != null)
        {
            StopCoroutine(pickupCooldownCoroutine);
            pickupCooldownCoroutine = null;
        }
        
        StopExpirationTimer();
        
        collected = false;
        onCooldown = false;
        hasExpired = false;
        isExpiring = false;
        EnablePowerupVisuals();
        
        // Restart expiration timer
        StartExpirationTimer();
    }

    public void ForceExpire()
    {
        ExpirePowerup();
    }

    // Debug method to test blinking manually
    [ContextMenu("Test Blinking")]
    public void TestBlinking()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(TestBlinkingCoroutine());
        }
    }

    private IEnumerator TestBlinkingCoroutine()
    {
        Debug.Log($"[Powerup] {name}: Testing blinking for 5 seconds...");
        
        isExpiring = true;
        StartWarningEffects();
        
        yield return new WaitForSeconds(5f);
        
        StopWarningEffects();
        Debug.Log($"[Powerup] {name}: Blinking test complete");
    }

    // Getters
    public float GetRemainingTime()
    {
        if (!enableExpiration || hasExpired) return 0f;
        return Mathf.Max(0f, expirationTime - (Time.time - spawnTime));
    }

    public bool IsExpiring => isExpiring;
    public bool HasExpired => hasExpired;

    private void OnDestroy()
    {
        if (pickupCooldownCoroutine != null)
        {
            StopCoroutine(pickupCooldownCoroutine);
        }
        
        StopExpirationTimer();
    }
}
