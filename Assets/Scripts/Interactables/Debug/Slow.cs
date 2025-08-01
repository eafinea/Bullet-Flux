using UnityEngine;

public class Slow : Interactable
{
    [Header("Slow Effect Settings")]
    [SerializeField] private float slowAmount = 0.5f; // 50% speed reduction
    [SerializeField] private float slowDuration = 3f;

    // This method is called when the player interacts with the object
    protected override void Interact()
    {
        // Find the player object
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found! Make sure the player GameObject has the 'Player' tag.");
            return;
        }

        // Check if player has PlayerMotor component
        PlayerMotor playerMotor = player.GetComponent<PlayerMotor>();
        if (playerMotor == null)
        {
            Debug.LogError("PlayerMotor component not found on player!");
            return;
        }

        // Apply slow effect to the player
        ApplySlowToPlayer(player);
        Debug.Log($"Applied Slow Effect to player: {slowAmount * 100}% speed reduction for {slowDuration} seconds");
    }

    private void ApplySlowToPlayer(GameObject player)
    {
        var slowComponent = player.GetComponent<SlowEffect>();
        if (slowComponent == null)
        {
            slowComponent = player.AddComponent<SlowEffect>();
        }
        slowComponent.ApplySlow(slowAmount, slowDuration);
    }
}
