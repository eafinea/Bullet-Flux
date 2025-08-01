using System;
using System.Collections;
using UnityEngine;

[System.Flags]
public enum BulletEffectType
{
    None = 0,
    Bounce = 1 << 0,        // 1
    DamageOverTime = 1 << 1, // 2
    Slow = 1 << 2,          // 4
}

public class BulletEffects : MonoBehaviour
{
    [Header("Bullet Effects")]
    [SerializeField] private BulletEffectType enabledEffects = BulletEffectType.None;

    [Header("Bounce Settings")]
    [SerializeField] private float bounceDistance = 10f;
    [SerializeField] private int maxBounces = 3;

    [Header("Damage Over Time Settings")]
    [SerializeField] private float dotDamage = 2f;
    [SerializeField] private float dotDuration = 5f;
    [SerializeField] private float dotTickRate = 1f;

    [Header("Slow Settings")]
    [SerializeField] private float slowAmount = 0.5f; // 50% speed reduction
    [SerializeField] private float slowDuration = 3f;

    public BulletEffectType CurrentEffects => enabledEffects;

    public bool HasEffect(BulletEffectType effect)
    {
        return (enabledEffects & effect) == effect;
    }

    public IEnumerator HandleBulletTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal,
        bool madeImpact, float bulletSpeed, ObjectPool<TrailRenderer> trailPool,
        ObjectPool<ParticleSystem> impactPool, LayerMask hitLayerMask, int bounceCount = 0, 
        Collider initialHitCollider = null, float damage = 0f)
    {
        Vector3 startPosition = trail.transform.position;
        Vector3 direction = (hitPoint - trail.transform.position).normalized;

        float distance = Vector3.Distance(trail.transform.position, hitPoint);
        float startingDistance = distance;

        // Move trail to hit point
        while (distance > 0)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, 1 - (distance / startingDistance));
            distance -= Time.deltaTime * bulletSpeed;
            yield return null;
        }

        trail.transform.position = hitPoint;

        if (madeImpact)
        {
            // Play impact effect
            ParticleSystem impact = impactPool.Get(hitPoint, Quaternion.LookRotation(hitNormal));
            impact.Play();
            
            // Return impact to pool after its duration
            StartCoroutine(ReturnImpactToPool(impact, impact.main.duration, impactPool));

            // Get hit collider - prioritize initialHitCollider but fallback to raycast
            Collider hitCollider = initialHitCollider;
            if (hitCollider == null)
            {
                hitCollider = GetHitCollider(hitPoint, hitNormal);
            }

            // Apply damage for ALL bullets (direct hits AND bounces)
            if (hitCollider != null && damage > 0f)
            {
                var damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"Bullet dealt {damage} damage to {hitCollider.gameObject.name} (Bounce #{bounceCount})");
                }
            }

            // Apply bullet effects to hit target
            ApplyEffectsToTarget(hitPoint, hitNormal);

            // ✅ Check if we hit an enemy - stop bouncing if we did
            bool hitEnemy = false;
            if (hitCollider != null)
            {
                // Check if this is an enemy (has IDamageable but not PlayerHealth)
                var damageable = hitCollider.GetComponent<IDamageable>();
                var playerHealth = hitCollider.GetComponent<PlayerHealth>();
                hitEnemy = (damageable != null && playerHealth == null);
            }

            // Handle bouncing if enabled, within bounce limit, and didn't hit an enemy
            if (HasEffect(BulletEffectType.Bounce) && bounceDistance > 0 && bounceCount < maxBounces && !hitEnemy)
            {
                yield return StartCoroutine(HandleBounce(trail, hitPoint, hitNormal, direction,
                    bulletSpeed, trailPool, impactPool, hitLayerMask, bounceCount, damage));
            }
            else
            {
                // Enhanced logging for debug
                if (hitEnemy)
                {
                    Debug.Log($"Bouncing stopped: Hit enemy {hitCollider?.gameObject.name}");
                }
                else if (bounceCount >= maxBounces)
                {
                    Debug.Log($"Bouncing stopped: Maximum bounces ({maxBounces}) reached");
                }
                else if (!HasEffect(BulletEffectType.Bounce))
                {
                    Debug.Log("Bouncing stopped: No bounce effect active");
                }
                
                // Return trail to pool if no more bounces
                yield return new WaitForSeconds(trail.time);
                trailPool.ReturnToPool(trail);
            }
        }
        else
        {
            // Return trail to pool if no impact
            yield return new WaitForSeconds(trail.time);
            trailPool.ReturnToPool(trail);
        }
    }

    // Enhanced HandleBounce with damage support
    private IEnumerator HandleBounce(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal,
        Vector3 incomingDirection, float bulletSpeed, ObjectPool<TrailRenderer> trailPool,
        ObjectPool<ParticleSystem> impactPool, LayerMask hitLayerMask, int bounceCount, float damage)
    {
        Vector3 bounceDirection = Vector3.Reflect(incomingDirection, hitNormal);
        
        // Offset the bounce start position slightly away from the surface to prevent clipping
        Vector3 bounceStartPoint = hitPoint + hitNormal * 0.01f;
        
        // Debug rays to visualize bouncing (remove in production)
        Debug.DrawRay(hitPoint, bounceDirection * bounceDistance, Color.blue, 1f);
        Debug.DrawRay(hitPoint, hitNormal * 0.5f, Color.green, 1f);
        Debug.Log($"Bouncing bullet {bounceCount + 1}/{maxBounces} - Direction: {bounceDirection}");

        if (Physics.Raycast(bounceStartPoint, bounceDirection, out RaycastHit bounceHit, bounceDistance, hitLayerMask))
        {
            // Pass damage and hit collider info to bounced bullet
            yield return StartCoroutine(HandleBulletTrail(trail, bounceHit.point, bounceHit.normal,
                true, bulletSpeed, trailPool, impactPool, hitLayerMask, bounceCount + 1, bounceHit.collider, damage));
        }
        else
        {
            // No more surfaces to bounce off, trail continues in bounce direction
            Vector3 finalPoint = bounceStartPoint + bounceDirection * bounceDistance;
            yield return StartCoroutine(HandleBulletTrail(trail, finalPoint, Vector3.zero,
                false, bulletSpeed, trailPool, impactPool, hitLayerMask, bounceCount + 1, null, damage));
        }
    }

    private void ApplyEffectsToTarget(Vector3 hitPoint, Vector3 hitNormal)
    {
        // Get the hit object
        Collider hitCollider = GetHitCollider(hitPoint, hitNormal);
        if (hitCollider == null) return;

        GameObject target = hitCollider.gameObject;

        // Apply Damage Over Time
        if (HasEffect(BulletEffectType.DamageOverTime))
        {
            ApplyDamageOverTime(target);
        }

        // Apply Slow Effect
        if (HasEffect(BulletEffectType.Slow))
        {
            ApplySlowEffect(target);
        }
    }

    // Better hit detection for bouncing bullets
    private Collider GetHitCollider(Vector3 hitPoint, Vector3 hitNormal)
    {
        // Try multiple approaches to find the hit collider
        
        // Method 1: Raycast slightly back from hit point
        if (Physics.Raycast(hitPoint - hitNormal * 0.1f, hitNormal, out RaycastHit hit, 0.2f))
        {
            return hit.collider;
        }
        
        // Method 2: Sphere cast at hit point (catches nearby colliders)
        Collider[] colliders = Physics.OverlapSphere(hitPoint, 0.1f);
        if (colliders.Length > 0)
        {
            // Return the first non-trigger collider
            foreach (var collider in colliders)
            {
                if (!collider.isTrigger)
                {
                    return collider;
                }
            }
        }
        
        return null;
    }

    private void ApplyDamageOverTime(GameObject target)
    {
        // Support both player and enemies
        var dotComponent = target.GetComponent<DamageOverTimeEffect>();
        if (dotComponent == null)
        {
            dotComponent = target.AddComponent<DamageOverTimeEffect>();
        }
        dotComponent.ApplyDOT(dotDamage, dotDuration, dotTickRate);
    }

    private void ApplySlowEffect(GameObject target)
    {
        // Support both player and enemies  
        var slowComponent = target.GetComponent<SlowEffect>();
        if (slowComponent == null)
        {
            slowComponent = target.AddComponent<SlowEffect>();
        }
        slowComponent.ApplySlow(slowAmount, slowDuration);
    }

    private IEnumerator ReturnImpactToPool(ParticleSystem impact, float delay, ObjectPool<ParticleSystem> impactPool)
    {
        yield return new WaitForSeconds(delay);
        impactPool.ReturnToPool(impact);
    }

    public void SetEnabledEffects(BulletEffectType effects)
    {
        enabledEffects = effects;
        Debug.Log($"Bullet effects updated to: {effects}");
    }

    // Optional: Method to add effects (for powerups that stack)
    public void AddEffect(BulletEffectType effect)
    {
        enabledEffects |= effect;
        Debug.Log($"Added bullet effect: {effect}. Current effects: {enabledEffects}");
    }

    public void RemoveEffect(BulletEffectType effect)
    {
        enabledEffects &= ~effect;
        Debug.Log($"Removed bullet effect: {effect}. Current effects: {enabledEffects}");
    }

    // Add method to remove all effects
    public void ClearAllEffects()
    {
        enabledEffects = BulletEffectType.None;
        Debug.Log("Cleared all bullet effects");
    }
}

// Helper components for effects
// Enhanced DOT component with better enemy support
public class DamageOverTimeEffect : MonoBehaviour
{
    private IDamageable damageable;
    private bool isActive = false;

    public void ApplyDOT(float damage, float duration, float tickRate)
    {
        if (!isActive)
        {
            StartCoroutine(DOTCoroutine(damage, duration, tickRate));
        }
        else
        {
            Debug.Log("DOT effect already active on " + gameObject.name);
        }
    }

    private IEnumerator DOTCoroutine(float damage, float duration, float tickRate)
    {
        isActive = true;
        
        // Better component detection for enemies
        damageable = GetComponent<IDamageable>();
        if (damageable == null)
        {
            // Try to find IDamageable in parent objects (for complex enemy hierarchies)
            damageable = GetComponentInParent<IDamageable>();
        }
        if (damageable == null)
        {
            // Try to find IDamageable in child objects
            damageable = GetComponentInChildren<IDamageable>();
        }
        
        if (damageable == null)
        {
            Debug.LogWarning($"No IDamageable found on {gameObject.name} or its hierarchy - DOT effect cancelled");
            isActive = false;
            Destroy(this);
            yield break;
        }
        
        Debug.Log($"Starting DOT on {gameObject.name}: {damage} damage every {tickRate}s for {duration}s");
        
        float elapsed = 0f;
        while (elapsed < duration && damageable != null)
        {
            // Apply damage to any IDamageable
            try
            {
                damageable.TakeDamage(damage);
                Debug.Log($"DOT dealt {damage} damage to {gameObject.name}. Remaining duration: {duration - elapsed:F1}s");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error applying DOT damage to {gameObject.name}: {e.Message}");
                break;
            }

            yield return new WaitForSeconds(tickRate);
            elapsed += tickRate;
        }

        Debug.Log($"DOT effect completed on {gameObject.name}");
        isActive = false;
        Destroy(this); // Remove component when done
    }
}

// Enhanced Slow component with better enemy support
public class SlowEffect : MonoBehaviour
{
    private PlayerMotor playerMotor;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private float originalPlayerSpeed;
    private float originalAgentSpeed;
    private bool isSlowActive = false;

    public void ApplySlow(float slowAmount, float duration)
    {
        if (!isSlowActive)
        {
            StartCoroutine(SlowCoroutine(slowAmount, duration));
        }
        else
        {
            Debug.Log("Slow effect already active on " + gameObject.name);
        }
    }

    private IEnumerator SlowCoroutine(float slowAmount, float duration)
    {
        isSlowActive = true;
        
        // Better component detection for enemies
        playerMotor = GetComponent<PlayerMotor>();
        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        // Try parent objects if not found on this object
        if (playerMotor == null)
            playerMotor = GetComponentInParent<PlayerMotor>();
        if (navMeshAgent == null)
            navMeshAgent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
            
        // Try child objects if still not found
        if (playerMotor == null)
            playerMotor = GetComponentInChildren<PlayerMotor>();
        if (navMeshAgent == null)
            navMeshAgent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
        
        bool slowApplied = false;
        
        if (playerMotor != null)
        {
            // Slow player
            originalPlayerSpeed = playerMotor.speed;
            float newPlayerSpeed = originalPlayerSpeed * (1f - slowAmount);
            playerMotor.speed = newPlayerSpeed;
            slowApplied = true;
            Debug.Log($"Player speed reduced from {originalPlayerSpeed} to {newPlayerSpeed} for {duration} seconds");
        }
        else if (navMeshAgent != null && navMeshAgent.enabled)
        {
            // ✅ FIXED: Slow enemy with safety checks
            originalAgentSpeed = navMeshAgent.speed;
            float newAgentSpeed = originalAgentSpeed * (1f - slowAmount);
            navMeshAgent.speed = newAgentSpeed;
            slowApplied = true;
            Debug.Log($"Enemy {gameObject.name} speed reduced from {originalAgentSpeed} to {newAgentSpeed} for {duration} seconds");
        }
        
        if (!slowApplied)
        {
            Debug.LogWarning($"No PlayerMotor or NavMeshAgent found on {gameObject.name} or its hierarchy - slow effect cancelled");
            isSlowActive = false;
            Destroy(this);
            yield break;
        }

        yield return new WaitForSeconds(duration);

        // Restore original speeds with safety checks
        try
        {
            if (playerMotor != null)
            {
                playerMotor.speed = originalPlayerSpeed;
                Debug.Log($"Player speed restored to {originalPlayerSpeed}");
            }
            else if (navMeshAgent != null && navMeshAgent.enabled)
            {
                navMeshAgent.speed = originalAgentSpeed;
                Debug.Log($"Enemy {gameObject.name} speed restored to {originalAgentSpeed}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error restoring speed for {gameObject.name}: {e.Message}");
        }

        isSlowActive = false;
        Destroy(this);
    }
}