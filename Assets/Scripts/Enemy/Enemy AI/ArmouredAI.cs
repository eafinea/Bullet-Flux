using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ArmouredAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Gun gun;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Shooting")]
    public float shootInterval = 1.5f;
    public float firingRange = 8f;
    public LayerMask obstacleMask;
    
    [Header("Animation")]
    [SerializeField] private string speedParameterName = "speed";
    [SerializeField] private bool useNormalizedSpeed = true;

    private NavMeshAgent agent;
    private Animator animator;
    private float lastShotTime;
    
    // Animation parameter hashes for better performance
    private int speedHash;
    private int isMovingHash;
    
    // Movement tracking
    private Vector3 lastPosition;
    private float currentSpeed;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        
        agent.speed = moveSpeed;
        
        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;
            
        // Cache animator parameter hashes
        if (animator != null)
        {
            speedHash = Animator.StringToHash(speedParameterName);
        }
        else
        {
            Debug.LogWarning($"[ArmouredAI] {name}: No Animator found! Animation will not work.");
        }
        
        // Initialize position tracking
        lastPosition = transform.position;
    }

    void Update()
    {
        if (player == null || gun == null) return;

        // 1) Always chase
        agent.SetDestination(player.position);
        FacePlayer();
        
        // 2) Update animation parameters
        UpdateAnimation();

        // 3) Shoot on LOS & range
        float dist = Vector3.Distance(transform.position, player.position);
        if (Time.time - lastShotTime >= shootInterval
            && dist <= firingRange
            && HasLOS())
        {
            lastShotTime = Time.time;
            gun.Shoot();  // uses weaponType (shotgun) internally
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        // Calculate current movement speed
        Vector3 currentPosition = transform.position;
        Vector3 deltaPosition = currentPosition - lastPosition;
        currentSpeed = deltaPosition.magnitude / Time.deltaTime;
        
        // Update animator parameters
        if (useNormalizedSpeed)
        {
            // Normalize speed between 0 and 1 (0 = standing, 1 = max speed)
            float normalizedSpeed = Mathf.Clamp01(currentSpeed / moveSpeed);
            animator.SetFloat(speedHash, normalizedSpeed);
        }
        else
        {
            // Use raw speed value
            animator.SetFloat(speedHash, currentSpeed);
        }
        
        // Set boolean for whether AI is moving
        bool isMoving = currentSpeed > 0.1f; // Small threshold to avoid jitter
        animator.SetBool(isMovingHash, isMoving);
        
        // Update last position for next frame
        lastPosition = currentPosition;
        
        // Debug info
        if (Application.isEditor)
        {
            Debug.Log($"[ArmouredAI] {name}: Speed = {currentSpeed:F2}, Normalized = {(currentSpeed/moveSpeed):F2}, IsMoving = {isMoving}");
        }
    }

    private bool HasLOS()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 dir = player.position - origin;
        return !Physics.Raycast(origin, dir.normalized, dir.magnitude, obstacleMask);
    }

    private void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion tgt = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, tgt, Time.deltaTime * 5f
        );
    }
  
    // Public methods for external access
    public float CurrentSpeed => currentSpeed;
    public bool IsMoving => currentSpeed > 0.1f;
    
    // Method to manually set animation parameters (useful for testing)
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(speedHash, speed);
        }
    }
    
    public void SetAnimationMoving(bool moving)
    {
        if (animator != null)
        {
            animator.SetBool(isMovingHash, moving);
        }
    }
}
