using UnityEngine;
using System.Collections;

public class SpawnPlayer : MonoBehaviour
{
    [Header("Player Spawn Configuration")]
    [SerializeField] private SpawnType playerSpawnType;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float spawnDelay = 0f;
    
    [Header("Player Spawn Settings")]
    [SerializeField] private bool useUpwardRotation = true; // Fix sideways spawning
    [SerializeField] private Vector3 spawnRotation = Vector3.zero; // Custom rotation override
    [SerializeField] private float groundCheckDistance = 2f;
    [SerializeField] private LayerMask groundLayer = -1;
    
    [Header("Fallback Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform fallbackSpawnPoint;
    
    [Header("Post-Spawn Setup")]
    [SerializeField] private bool ensurePlayerComponents = true;
    [SerializeField] private bool positionCameraCorrectly = true;
    [SerializeField] private float postSpawnDelay = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // Store reference to spawned player for debugging
    private GameObject spawnedPlayer;

    private void Start()
    {
        if (spawnOnStart)
        {
            if (spawnDelay > 0f)
            {
                Invoke(nameof(SpawnPlayerDelayed), spawnDelay);
            }
            else
            {
                SpawnPlayerDelayed();
            }
        }
    }

    private void SpawnPlayerDelayed()
    {
        SpawnPlayerUsingSpawnType();
    }

    /// <summary>
    /// Spawns the player using the SpawnType system with proper orientation
    /// </summary>
    public void SpawnPlayerUsingSpawnType()
    {
        if (playerSpawnType == null)
        {
            Debug.LogError("[SpawnPlayer] No SpawnType assigned! Attempting to find one or use fallback...");
            AttemptAutoSpawn();
            return;
        }

        if (playerSpawnType.prefabToSpawn == null)
        {
            Debug.LogError("[SpawnPlayer] SpawnType has no prefab assigned! Using fallback...");
            SpawnPlayerFallback();
            return;
        }

        if (debugMode)
        {
            Debug.Log($"[SpawnPlayer] Spawning player using SpawnType: {playerSpawnType.name}");
        }

        // Start enhanced spawn routine
        StartCoroutine(SpawnWithCorrectOrientation());
    }

    /// <summary>
    /// Enhanced spawning routine that fixes orientation and ensures proper setup
    /// </summary>
    private IEnumerator SpawnWithCorrectOrientation()
    {
        // Find a good spawn position
        Vector3 spawnPosition = FindValidSpawnPosition();
        Quaternion spawnRotationQuat = GetCorrectSpawnRotation();

        // Spawn the player with correct orientation
        spawnedPlayer = Instantiate(playerSpawnType.prefabToSpawn, spawnPosition, spawnRotationQuat);
        
        if (debugMode)
        {
            Debug.Log($"[SpawnPlayer] Player spawned at: {spawnPosition} with rotation: {spawnRotationQuat.eulerAngles}");
        }

        // Wait a frame for physics to settle
        yield return new WaitForFixedUpdate();

        // Ensure player is properly positioned on ground
        CorrectPlayerGroundPosition();

        // Wait for post-spawn delay
        if (postSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(postSpawnDelay);
        }

        // Setup player references and components
        SetupPlayerReferences(spawnedPlayer);

        // Final validation
        ValidateSpawnedPlayer();
    }

    /// <summary>
    /// Finds a valid spawn position using SpawnType logic or fallback
    /// </summary>
    private Vector3 FindValidSpawnPosition()
    {
        // Try to use SpawnType's collision detection logic
        Collider areaCollider = playerSpawnType.GetComponent<Collider>();
        if (areaCollider != null)
        {
            for (int attempt = 0; attempt < playerSpawnType.maxAttempts; attempt++)
            {
                Vector3 candidate = RandomPointInCollider(areaCollider);
                
                // Check if position is clear
                if (!Physics.CheckSphere(candidate, playerSpawnType.checkRadius, playerSpawnType.obstacleMask))
                {
                    return candidate;
                }
            }
        }

        // Fallback to SpawnType transform position
        return playerSpawnType.transform.position;
    }

    /// <summary>
    /// Gets the correct spawn rotation (fixes sideways spawning)
    /// </summary>
    private Quaternion GetCorrectSpawnRotation()
    {
        if (useUpwardRotation)
        {
            // Force upward rotation (prevents sideways spawning)
            return Quaternion.Euler(spawnRotation);
        }
        else
        {
            // Use SpawnType's rotation
            return playerSpawnType.transform.rotation;
        }
    }

    /// <summary>
    /// Ensures player is properly positioned on the ground
    /// </summary>
    private void CorrectPlayerGroundPosition()
    {
        if (spawnedPlayer == null) return;

        Vector3 startPos = spawnedPlayer.transform.position + Vector3.up * 1f;
        
        if (Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            // Position player slightly above ground
            Vector3 correctedPosition = hit.point + Vector3.up * 0.1f;
            spawnedPlayer.transform.position = correctedPosition;
            
            if (debugMode)
            {
                Debug.Log($"[SpawnPlayer] Corrected player position to: {correctedPosition}");
            }
        }
    }

    /// <summary>
    /// Copy of SpawnType's RandomPointInCollider method for direct use
    /// </summary>
    private Vector3 RandomPointInCollider(Collider col)
    {
        if (col is BoxCollider box)
        {
            Vector3 half = box.size * 0.5f;
            Vector3 local = new Vector3(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y),
                Random.Range(-half.z, half.z)
            );
            return box.transform.TransformPoint(box.center + local);
        }
        else if (col is SphereCollider sph)
        {
            Vector3 dir = Random.onUnitSphere * Random.Range(0f, sph.radius);
            return sph.transform.TransformPoint(sph.center + dir);
        }
        else
        {
            var b = col.bounds;
            return new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                Random.Range(b.min.z, b.max.z)
            );
        }
    }

    /// <summary>
    /// Attempts to automatically find a SpawnType or use fallback spawning
    /// </summary>
    private void AttemptAutoSpawn()
    {
        SpawnType foundSpawnType = FindFirstObjectByType<SpawnType>();
        
        if (foundSpawnType != null && foundSpawnType.prefabToSpawn != null)
        {
            playerSpawnType = foundSpawnType;
            Debug.LogWarning($"[SpawnPlayer] Auto-assigned SpawnType: {foundSpawnType.name}");
            SpawnPlayerUsingSpawnType();
            return;
        }

        Debug.LogWarning("[SpawnPlayer] No valid SpawnType found. Using fallback spawn...");
        SpawnPlayerFallback();
    }

    /// <summary>
    /// Fallback spawning method with proper orientation
    /// </summary>
    private void SpawnPlayerFallback()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnPlayer] No player prefab assigned for fallback spawning!");
            return;
        }

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotationQuat = GetCorrectSpawnRotation();

        if (fallbackSpawnPoint != null)
        {
            spawnPosition = fallbackSpawnPoint.position;
            if (!useUpwardRotation)
            {
                spawnRotationQuat = fallbackSpawnPoint.rotation;
            }
        }
        else
        {
            spawnPosition = transform.position;
            if (!useUpwardRotation)
            {
                spawnRotationQuat = transform.rotation;
            }
        }

        spawnedPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotationQuat);
        
        if (debugMode)
        {
            Debug.Log($"[SpawnPlayer] Player spawned using fallback at: {spawnPosition}");
        }

        SetupPlayerReferences(spawnedPlayer);
    }

    /// <summary>
    /// Sets up player references and ensures components are working
    /// </summary>
    private void SetupPlayerReferences(GameObject player)
    {
        if (player == null) return;

        // Tag the spawned object as Player
        if (player.tag != "Player")
        {
            player.tag = "Player";
        }

        if (ensurePlayerComponents)
        {
            // Ensure essential components are present and enabled
            EnsurePlayerComponents(player);
        }

        if (positionCameraCorrectly)
        {
            // Setup camera positioning
            SetupPlayerCamera(player);
        }

        if (debugMode)
        {
            Debug.Log($"[SpawnPlayer] Player setup completed for: {player.name}");
        }
    }

    /// <summary>
    /// Ensures all essential player components are present and enabled
    /// </summary>
    private void EnsurePlayerComponents(GameObject player)
    {
        // Check CharacterController
        var characterController = player.GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("[SpawnPlayer] Player is missing CharacterController!");
        }
        else if (!characterController.enabled)
        {
            characterController.enabled = true;
            if (debugMode) Debug.Log("[SpawnPlayer] Enabled CharacterController");
        }

        // Check PlayerMotor
        var playerMotor = player.GetComponent<PlayerMotor>();
        if (playerMotor == null)
        {
            Debug.LogError("[SpawnPlayer] Player is missing PlayerMotor!");
        }
        else if (!playerMotor.enabled)
        {
            playerMotor.enabled = true;
            if (debugMode) Debug.Log("[SpawnPlayer] Enabled PlayerMotor");
        }

        // Check InputManager
        var inputManager = player.GetComponent<InputManager>();
        if (inputManager == null)
        {
            Debug.LogError("[SpawnPlayer] Player is missing InputManager!");
        }
        else if (!inputManager.enabled)
        {
            inputManager.enabled = true;
            if (debugMode) Debug.Log("[SpawnPlayer] Enabled InputManager");
        }

        // Check PlayerInteract
        var playerInteract = player.GetComponent<PlayerInteract>();
        if (playerInteract != null && !playerInteract.enabled)
        {
            playerInteract.enabled = true;
            if (debugMode) Debug.Log("[SpawnPlayer] Enabled PlayerInteract");
        }

        // Check PlayerShoot
        var playerShoot = player.GetComponent<PlayerShoot>();
        if (playerShoot != null && !playerShoot.enabled)
        {
            playerShoot.enabled = true;
            if (debugMode) Debug.Log("[SpawnPlayer] Enabled PlayerShoot");
        }
    }

    /// <summary>
    /// Sets up player camera correctly
    /// </summary>
    private void SetupPlayerCamera(GameObject player)
    {
        var playerLook = player.GetComponent<PlayerLook>();
        if (playerLook != null && playerLook.playerCamera != null)
        {
            // Ensure camera is active
            if (!playerLook.playerCamera.gameObject.activeInHierarchy)
            {
                playerLook.playerCamera.gameObject.SetActive(true);
                if (debugMode) Debug.Log("[SpawnPlayer] Activated player camera");
            }

            // Set camera as main camera
            if (Camera.main != playerLook.playerCamera)
            {
                // Disable other main cameras
                Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (Camera cam in allCameras)
                {
                    if (cam != playerLook.playerCamera && cam.CompareTag("MainCamera"))
                    {
                        cam.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates that the spawned player is working correctly
    /// </summary>
    private void ValidateSpawnedPlayer()
    {
        if (spawnedPlayer == null)
        {
            Debug.LogError("[SpawnPlayer] Player validation failed - spawned player is null!");
            return;
        }

        // Check if player is active
        if (!spawnedPlayer.activeInHierarchy)
        {
            Debug.LogError("[SpawnPlayer] Player validation failed - spawned player is inactive!");
            spawnedPlayer.SetActive(true);
        }

        // Check if player has required components
        var requiredComponents = new System.Type[]
        {
            typeof(CharacterController),
            typeof(PlayerMotor),
            typeof(InputManager),
            typeof(PlayerLook)
        };

        foreach (var componentType in requiredComponents)
        {
            if (spawnedPlayer.GetComponent(componentType) == null)
            {
                Debug.LogError($"[SpawnPlayer] Player validation failed - missing {componentType.Name}!");
            }
        }

        if (debugMode)
        {
            Debug.Log("[SpawnPlayer] Player validation completed successfully!");
        }
    }

    /// <summary>
    /// Public method to manually spawn player
    /// </summary>
    public void ManualSpawnPlayer()
    {
        SpawnPlayerUsingSpawnType();
    }

    /// <summary>
    /// Public method to respawn player with delay
    /// </summary>
    public void RespawnPlayer(float delay = 0f)
    {
        // Destroy existing player if present
        if (spawnedPlayer != null)
        {
            Destroy(spawnedPlayer);
            spawnedPlayer = null;
        }

        if (delay > 0f)
        {
            Invoke(nameof(ManualSpawnPlayer), delay);
        }
        else
        {
            ManualSpawnPlayer();
        }
    }

    /// <summary>
    /// Configure the SpawnType reference at runtime
    /// </summary>
    public void SetSpawnType(SpawnType spawnType)
    {
        playerSpawnType = spawnType;
        
        if (debugMode)
        {
            Debug.Log($"[SpawnPlayer] SpawnType set to: {spawnType?.name ?? "null"}");
        }
    }

    // Validation in the editor
    private void OnValidate()
    {
        if (playerSpawnType != null)
        {
            if (playerSpawnType.prefabToSpawn == null)
            {
                Debug.LogWarning("[SpawnPlayer] SpawnType has no prefab assigned!");
            }
            else if (playerSpawnType.spawnCount != 1)
            {
                Debug.LogWarning("[SpawnPlayer] SpawnType should have spawnCount = 1 for player spawning!");
            }
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (fallbackSpawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(fallbackSpawnPoint.position, 1f);
            Gizmos.DrawLine(fallbackSpawnPoint.position, fallbackSpawnPoint.position + fallbackSpawnPoint.forward * 2f);
        }
        else
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        // Draw ground check visualization
        if (spawnedPlayer != null)
        {
            Gizmos.color = Color.green;
            Vector3 start = spawnedPlayer.transform.position + Vector3.up * 1f;
            Vector3 end = start + Vector3.down * groundCheckDistance;
            Gizmos.DrawLine(start, end);
        }
    }

    // Public getters for debugging
    public GameObject SpawnedPlayer => spawnedPlayer;
    public bool IsPlayerSpawned => spawnedPlayer != null && spawnedPlayer.activeInHierarchy;
}
