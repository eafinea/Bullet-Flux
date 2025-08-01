using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    // Components
    private PlayerUI playerUI;
    [SerializeField] private GunStats gunStats;
    [SerializeField] private ParticleSystem shootingSystem;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private ParticleSystem impactParticleSystem;
    [SerializeField] private TrailRenderer bulletTrail;
    [SerializeField] private float shootDelay;
    [SerializeField] private float bulletSpeed = 100;
    [SerializeField] private LayerMask hitLayerMask;
    [SerializeField] private BulletEffects bulletEffects;
    private Animator animator;

    // Gun state variables
    private bool isOverheated = false;
    private float lastShootTime;
    private float lastHeatTime; // For overheat cooldown delay
    private ObjectPool<TrailRenderer> trailPool;
    private ObjectPool<ParticleSystem> impactPool;
    private Coroutine overheatCoroutine; // Track the overheat coroutine

    // Animator parameter hash for performance
    private int overheatBoolHash;

    private void Awake()
    {
        // Initialize pools only if we have the required prefabs
        if (bulletTrail != null)
            trailPool = new ObjectPool<TrailRenderer>(bulletTrail, 10);
        
        if (impactParticleSystem != null)
            impactPool = new ObjectPool<ParticleSystem>(impactParticleSystem, 10);
        
        // Set shoot delay from GunStats if available
        if (gunStats != null)
            shootDelay = gunStats.FireRate;
        
        animator = GetComponentInChildren<Animator>();

        // Cache animator parameter hash for better performance
        if (animator != null)
        {
            overheatBoolHash = Animator.StringToHash("isOverheat");
        }
        
        // Auto-assign bulletSpawnPoint if not set
        if (bulletSpawnPoint == null)
        {
            bulletSpawnPoint = transform;
            Debug.LogWarning($"[Gun] {gameObject.name}: bulletSpawnPoint not assigned, using transform position");
        }
    }

    private void Update()
    {
        if (bulletSpawnPoint != null)
        {
            Debug.DrawRay(bulletSpawnPoint.position, transform.forward * 100f, Color.red);
        }
    }

    public void Shoot()
    {
        if (!CanShoot()) return;

        if (lastShootTime + shootDelay < Time.time)
        {
            // Check ammo/heat/shots based on weapon type
            if (!ConsumeAmmoOrResource()) return;

            // Fire the weapon
            FireWeapon();
            lastShootTime = Time.time;
        }
    }

    private bool CanShoot()
    {
        // Add null check for gunStats
        if (gunStats == null)
        {
            Debug.LogError($"[Gun] {gameObject.name}: GunStats is null! Cannot shoot.");
            return false;
        }

        // Check both local overheat state AND GunStats overheat state
        if (isOverheated || gunStats.IsOverheated)
        {
            return false;
        }
        return true;
    }

    private bool ConsumeAmmoOrResource()
    {
        if (gunStats == null) return false;

        switch (gunStats.CurrentWeaponType)
        {
            case GunStats.WeaponType.Pistol:
                if (gunStats.CurrentHeat + gunStats.HeatPerShot >= gunStats.MaxHeat)
                {
                    gunStats.SetHeat(gunStats.MaxHeat);
                    StartOverheatCooldown();
                    return false;
                }
                gunStats.AddHeat(gunStats.HeatPerShot);
                lastHeatTime = Time.time;
                break;

            case GunStats.WeaponType.Shotgun:
            case GunStats.WeaponType.SMG:
                if (gunStats.RemainingShots <= 0)
                {
                    return false;
                }
                gunStats.ConsumeFiniteShot();
                break;
        }

        // WeaponManager handles UI updates (only for player weapons)
        var weaponManager = transform.root.GetComponentInChildren<WeaponManager>();
        if (weaponManager != null && weaponManager.CurrentGun == this)
        {
            weaponManager.ForceUIUpdate();
        }
        
        return true;
    }

    private void FireWeapon()
    {
        // ✅ ADD NULL CHECKS FOR AI WEAPONS
        if (shootingSystem != null)
        {
            shootingSystem.Play();
        }
        
        if (animator != null)
        {
            animator.SetTrigger("isShooting");
        }

        Vector3 direction = transform.forward;

        if (gunStats != null)
        {
            switch (gunStats.CurrentWeaponType)
            {
                case GunStats.WeaponType.Shotgun:
                    FireShotgun();
                    break;
                
                default:
                    FireSingleShot(direction);
                    break;
            }
        }
        else
        {
            // Fallback for AI weapons without GunStats
            FireSingleShot(direction);
        }
    }

    private void FireSingleShot(Vector3 direction)
    {
        // Check if we have the required components for trail system
        if (trailPool == null || bulletSpawnPoint == null)
        {
            // Simple raycast without visual effects (for AI)
            FireSimpleRaycast(direction);
            return;
        }

        TrailRenderer trail = trailPool.Get(bulletSpawnPoint.position, Quaternion.identity);
        trail.transform.position = bulletSpawnPoint.position;

        if (Physics.Raycast(bulletSpawnPoint.position, direction, out RaycastHit hit, float.MaxValue, hitLayerMask))
        {
            var dmg = gunStats != null ? gunStats.Damage : 10f; // Default damage for AI
            var damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(dmg);
            }
            ApplyImpactForce(hit, direction);
            
            StartCoroutine(SpawnTrail(trail, hit.point, hit.normal, true, hit.collider, dmg));
        }
        else
        {
            StartCoroutine(SpawnTrail(trail, direction * 100, Vector3.zero, false, null, 0f));
        }
    }

    // ✅ NEW: Simple raycast for AI weapons without visual effects
    private void FireSimpleRaycast(Vector3 direction)
    {
        Vector3 startPos = bulletSpawnPoint != null ? bulletSpawnPoint.position : transform.position;
        
        if (Physics.Raycast(startPos, direction, out RaycastHit hit, float.MaxValue, hitLayerMask))
        {
            var dmg = gunStats != null ? gunStats.Damage : 10f; // Default damage for AI
            var damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(dmg);
            }
            ApplyImpactForce(hit, direction);
        }
    }

    private void FireShotgun()
    {
        if (gunStats == null) return;

        for (int i = 0; i < gunStats.ShotgunPellets; i++)
        {
            Vector3 spread = transform.forward +
                (transform.right * UnityEngine.Random.Range(-gunStats.FireSpread, gunStats.FireSpread)) +
                (transform.up * UnityEngine.Random.Range(-gunStats.FireSpread, gunStats.FireSpread));
            FireSingleShot(spread.normalized);
        }
    }

    private void ApplyImpactForce(RaycastHit hit, Vector3 direction)
    {
        float damage = gunStats != null ? gunStats.Damage : 10f;
        float baseImpulse = damage * 5f;
        
        if (hit.collider.TryGetComponent<IDamageable>(out _))
        {
            baseImpulse *= 0.5f;
        }
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb != null)
        {
            rb.AddForceAtPosition(direction * baseImpulse, hit.point, ForceMode.Impulse);
        }
    }

    public void HandleOverheatCooling()
    {
        if (gunStats == null) return;

        // Only cool down if not overheated and enough time has passed since last shot
        if (!isOverheated && gunStats.CurrentHeat > 0 && Time.time - lastHeatTime > 1f)
        {
            gunStats.CoolDown(gunStats.CooldownRate * Time.deltaTime);
            var weaponManager = transform.root.GetComponentInChildren<WeaponManager>();
            if (weaponManager != null && weaponManager.CurrentGun == this)
            {
                weaponManager.ForceUIUpdate();
            }
        }
    }

    // New method to start overheat cooldown and prevent duplicate coroutines
    private void StartOverheatCooldown()
    {
        // Stop any existing overheat coroutine
        if (overheatCoroutine != null)
        {
            StopCoroutine(overheatCoroutine);
        }
        
        // Start new overheat coroutine
        overheatCoroutine = StartCoroutine(OverheatCooldown());
    }

    // Public method to force stop overheat (called when switching weapons)
    public void ForceStopOverheat()
    {
        if (overheatCoroutine != null)
        {
            StopCoroutine(overheatCoroutine);
            overheatCoroutine = null;
        }
        
        // Reset overheat state
        isOverheated = false;
        
        // Reset animator parameter
        SetOverheatAnimation(false);
    }

    // Add method to check if overheat coroutine should continue
    private bool ShouldContinueOverheatCooldown()
    {
        if (gunStats == null) return false;

        // Check if this gun is still the pistol and still the active weapon
        var weaponManager = transform.root.GetComponentInChildren<WeaponManager>();
        bool isActiveWeapon = weaponManager != null && weaponManager.CurrentGun == this;
        bool isPistol = gunStats.CurrentWeaponType == GunStats.WeaponType.Pistol;
        
        return isPistol && isActiveWeapon && gunStats.CurrentHeat > 0;
    }

    private IEnumerator OverheatCooldown()
    {
        isOverheated = true;
        SetOverheatAnimation(true);

        var weaponManager = transform.root.GetComponentInChildren<WeaponManager>();
        if (weaponManager != null && weaponManager.CurrentGun == this)
        {
            weaponManager.ForceUIUpdate();
        }
        
        // Wait a moment to display the overheated state before starting to cool down
        yield return new WaitForSeconds(1f);
        
        // Check if we should still continue (weapon might have been switched)
        if (!ShouldContinueOverheatCooldown())
        {
            isOverheated = false;
            SetOverheatAnimation(false);
            overheatCoroutine = null;
            yield break;
        }
        
        // Gradually reduce heat during overheat period
        float overheatTimer = 0f;
        while (overheatTimer < gunStats.OverheatedCooldownTime && ShouldContinueOverheatCooldown())
        {
            overheatTimer += Time.deltaTime;
            
            // Gradually reduce heat during overheat period
            float cooldownProgress = overheatTimer / gunStats.OverheatedCooldownTime;
            float targetHeat = gunStats.MaxHeat * (1f - cooldownProgress);
            gunStats.SetHeat(targetHeat);
            
            weaponManager = transform.root.GetComponentInChildren<WeaponManager>();
            if (weaponManager != null && weaponManager.CurrentGun == this)
            {
                weaponManager.ForceUIUpdate();
            }
            
            yield return null;
        }
        
        // Ensure heat is set to 0 at the end (only if we completed the full cooldown)
        if (ShouldContinueOverheatCooldown())
        {
            gunStats.SetHeat(0f);
            isOverheated = false;
            SetOverheatAnimation(false);
            
            weaponManager = transform.root.GetComponentInChildren<WeaponManager>();
            if (weaponManager != null && weaponManager.CurrentGun == this)
            {
                weaponManager.ForceUIUpdate();
            }
        }
        else
        {
            // If we exited early due to weapon switch, just reset the overheated flag
            isOverheated = false;
            SetOverheatAnimation(false);
        }
        
        overheatCoroutine = null;
    }

    private void SetOverheatAnimation(bool isOverheating)
    {
        if (animator != null)
        {
            animator.SetBool(overheatBoolHash, isOverheating);
        }
    }

    // Public method to check if this gun is overheated (for WeaponManager)
    public bool IsOverheated => isOverheated || (gunStats != null && gunStats.IsOverheated);

    // Public method to get last heat time (for WeaponManager)
    public float LastHeatTime => lastHeatTime;

    public void ShootWithDirection(Vector3 direction)
    {
        // Play shooting effects for AI weapons
        if (shootingSystem != null)
        {
            shootingSystem.Play();
        }
        
        // Use existing weapon logic based on type
        if (gunStats != null)
        {
            switch (gunStats.CurrentWeaponType)
            {
                case GunStats.WeaponType.Shotgun:
                    FireShotgunWithDirection(direction);
                    break;
                
                default:
                    FireSingleShot(direction);
                    break;
            }
        }
        else
        {
            // Fallback for AI without GunStats
            FireSingleShot(direction);
        }
    }

    private void FireShotgunWithDirection(Vector3 direction)
    {
        if (gunStats == null) return;

        for (int i = 0; i < gunStats.ShotgunPellets; i++)
        {
            // Apply spread to the custom direction instead of transform.forward
            Vector3 spread = direction +
                (Vector3.Cross(direction, Vector3.up).normalized * UnityEngine.Random.Range(-gunStats.FireSpread, gunStats.FireSpread)) +
                (Vector3.Cross(direction, Vector3.right).normalized * UnityEngine.Random.Range(-gunStats.FireSpread, gunStats.FireSpread));
            
            FireSingleShot(spread.normalized);
        }
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool madeImpact, Collider hitCollider = null, float damage = 0f)
    {
        if (bulletEffects != null)
        {
            yield return StartCoroutine(bulletEffects.HandleBulletTrail(trail, hitPoint, hitNormal,
                madeImpact, bulletSpeed, trailPool, impactPool, hitLayerMask, 0, hitCollider, damage));
        }
        else
        {
            // Fallback to basic trail logic without bouncing
            Vector3 startPosition = trail.transform.position;
            float distance = Vector3.Distance(trail.transform.position, hitPoint);
            float startingDistance = distance;

            while (distance > 0)
            {
                trail.transform.position = Vector3.Lerp(startPosition, hitPoint, 1 - (distance / startingDistance));
                distance -= Time.deltaTime * bulletSpeed;
                yield return null;
            }

            trail.transform.position = hitPoint;

            if (madeImpact && impactPool != null)
            {
                ParticleSystem impact = impactPool.Get(hitPoint, Quaternion.LookRotation(hitNormal));
                impact.Play();
                StartCoroutine(ReturnImpactToPool(impact, impact.main.duration));
            }

            if (trailPool != null)
            {
                StartCoroutine(ReturnTrailToPool(trail, trail.time));
            }
        }
    }

    private IEnumerator ReturnImpactToPool(ParticleSystem impact, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (impactPool != null)
            impactPool.ReturnToPool(impact);
    }

    private IEnumerator ReturnTrailToPool(TrailRenderer trail, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (trailPool != null)
            trailPool.ReturnToPool(trail);
    }
}
