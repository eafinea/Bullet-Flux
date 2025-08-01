using UnityEngine;

public class DOT : Interactable
{
    [Header("DOT Settings")]
    [SerializeField] private float dotDamage = 2f;
    [SerializeField] private float dotDuration = 5f;
    [SerializeField] private float dotTickRate = 1f;

    protected override void Interact()
    {
        // Find the player object
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found! Make sure the player GameObject has the 'Player' tag.");
            return;
        }

        // Check if player has PlayerHealth component
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth component not found on player!");
            return;
        }

        // Apply DOT effect to the player
        ApplyDOTToPlayer(player);
        Debug.Log($"Applied DOT Effect to player: {dotDamage} damage every {dotTickRate} seconds for {dotDuration} seconds");
    }

    private void ApplyDOTToPlayer(GameObject player)
    {
        var dotComponent = player.GetComponent<DamageOverTimeEffect>();
        if (dotComponent == null)
        {
            dotComponent = player.AddComponent<DamageOverTimeEffect>();
        }
        dotComponent.ApplyDOT(dotDamage, dotDuration, dotTickRate);
    }
}
