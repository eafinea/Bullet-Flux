using UnityEngine;
using System;

[Serializable]
public struct Drop
{
    public GameObject prefab;
    [Range(0f, 1f)]
    public float chance;
}

public class EnemyDropper : MonoBehaviour
{
    [Header("Drops (chance 0–1)")]
    public Drop[] drops;
    
    [Header("Drop Settings")]
    [SerializeField] private bool dropOnlyOne = true; // Only drop one item
    [SerializeField] private float dropHeight = 1f; // Height above ground to drop
    [SerializeField] private float groundCheckDistance = 10f; // How far to raycast down
    [SerializeField] private LayerMask groundLayerMask = -1; // What counts as ground

    public void TryDrop()
    {
        if (drops == null || drops.Length == 0) return;

        if (dropOnlyOne)
        {
            // Drop only one item based on weighted probability
            DropSingleItem();
        }
        else
        {
            // Original behavior - drop multiple items
            DropMultipleItems();
        }
    }

    private void DropSingleItem()
    {
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var drop in drops)
        {
            if (drop.prefab != null)
                totalWeight += drop.chance;
        }

        if (totalWeight <= 0f) return;

        // Select random item based on weighted probability
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;
            
            currentWeight += drop.chance;
            if (randomValue <= currentWeight)
            {
                SpawnDrop(drop.prefab);
                return; // Only drop one item
            }
        }
    }

    private void DropMultipleItems()
    {
        // Original behavior
        foreach (var drop in drops)
        {
            if (UnityEngine.Random.value <= drop.chance && drop.prefab != null)
            {
                SpawnDrop(drop.prefab);
            }
        }
    }

    private void SpawnDrop(GameObject prefab)
    {
        Vector3 dropPosition = CalculateGroundPosition();
        GameObject droppedItem = Instantiate(prefab, dropPosition, Quaternion.identity);
        
        // Configure the powerup for enemy drops
        ConfigureDroppedPowerup(droppedItem);
        
        Debug.Log($"Dropped {prefab.name} at {dropPosition}");
    }

    private Vector3 CalculateGroundPosition()
    {
        Vector3 startPosition = transform.position + Vector3.up * dropHeight;
        
        // Raycast down to find ground
        if (Physics.Raycast(startPosition, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayerMask))
        {
            // Place slightly above the ground
            return hit.point + Vector3.up * 0.1f;
        }
        
        // Fallback to original position if no ground found
        return transform.position;
    }

    private void ConfigureDroppedPowerup(GameObject droppedItem)
    {
        var powerup = droppedItem.GetComponent<Powerup>();
        if (powerup != null)
        {
            // Configure enemy-dropped powerups
            powerup.ConfigureReusability(
                reusable: false,           // Enemy drops are single-use
                cooldown: 0f,             // No cooldown needed
                destroyOnPickup: true     // Destroy after pickup
            );
            
            // Set weapon powerup durations for enemy drops
            if (powerup.GetPowerupType() == PowerupType.ShotgunWeapon)
            {
                powerup.ConfigureWeaponPowerup(
                    weaponType: GunStats.WeaponType.Shotgun,
                    shots: 15,         // Fewer shots for enemy drops
                    duration: 20f      // 20 second duration
                );
            }
            else if (powerup.GetPowerupType() == PowerupType.SMGWeapon)
            {
                powerup.ConfigureWeaponPowerup(
                    weaponType: GunStats.WeaponType.SMG,
                    shots: 25,         // Fewer shots for enemy drops
                    duration: 25f      // 25 second duration
                );
            }
            else if (powerup.GetPowerupType() == PowerupType.BulletEffect)
            {
                powerup.ConfigureBulletEffectPowerup(
                    effect: BulletEffectType.Bounce, // Can be random
                    duration: 10f,     // 10 second duration
                    randomize: true    // Randomize effect
                );
            }
            
            Debug.Log($"Configured enemy-dropped powerup: {powerup.GetPowerupType()}");
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Show ground check
        Gizmos.color = Color.red;
        Vector3 start = transform.position + Vector3.up * dropHeight;
        Vector3 end = start + Vector3.down * groundCheckDistance;
        Gizmos.DrawLine(start, end);
    }
}
