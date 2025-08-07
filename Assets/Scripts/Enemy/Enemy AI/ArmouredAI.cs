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
    [SerializeField] private string isMovingParameterName = "isMoving";
    [SerializeField] private bool useNormalizedSpeed = true;
    [SerializeField] private bool enableAnimationDebug = false; // Debug toggle

    private NavMeshAgent agent;
    private Animator animator;
    private float lastShotTime;
    
    // Movement tracking
    private Vector3 lastPosition;
    private float currentSpeed;
    
    // Animation parameter validation
    private bool hasSpeedParameter = false;
    private bool hasIsMovingParameter = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        
        agent.speed = moveSpeed;
        
        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;
            
        // Validate animator parameters
        ValidateAnimatorParameters();
        
        // Initialize position tracking
        lastPosition = transform.position;
    }

    private void ValidateAnimatorParameters()
    {
        if (animator == null)
        {
            Debug.LogWarning($"[ArmouredAI] {name}: No Animator found! Animation will not work.");
            return;
        }

        // Check if the speed parameter exists
        if (HasAnimatorParameter(speedParameterName, AnimatorControllerParameterType.Float))
        {
            hasSpeedParameter = true;
            if (enableAnimationDebug)
                Debug.Log($"[ArmouredAI] {name}: Found speed parameter '{speedParameterName}'");
        }
        else
        {
            hasSpeedParameter = false;
            Debug.LogWarning($"[ArmouredAI] {name}: Speed parameter '{speedParameterName}' not found in Animator!");
        }

        // Check if the isMoving parameter exists
        if (HasAnimatorParameter(isMovingParameterName, AnimatorControllerParameterType.Bool))
        {
            hasIsMovingParameter = true;
            if (enableAnimationDebug)
                Debug.Log($"[ArmouredAI] {name}: Found isMoving parameter '{isMovingParameterName}'");
        }
        else
        {
            hasIsMovingParameter = false;
            Debug.LogWarning($"[ArmouredAI] {name}: IsMoving parameter '{isMovingParameterName}' not found in Animator!");
        }
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType paramType)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == paramType)
                return true;
        }
        return false;
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
        
        // Update speed parameter
        if (hasSpeedParameter)
        {
            try
            {
                if (useNormalizedSpeed)
                {
                    // Normalize speed between 0 and 1 (0 = standing, 1 = max speed)
                    float normalizedSpeed = Mathf.Clamp01(currentSpeed / moveSpeed);
                    animator.SetFloat(speedParameterName, normalizedSpeed);
                    
                    if (enableAnimationDebug)
                        Debug.Log($"[ArmouredAI] {name}: Set speed to {normalizedSpeed:F2} (normalized)");
                }
                else
                {
                    // Use raw speed value
                    animator.SetFloat(speedParameterName, currentSpeed);
                    
                    if (enableAnimationDebug)
                        Debug.Log($"[ArmouredAI] {name}: Set speed to {currentSpeed:F2} (raw)");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ArmouredAI] {name}: Error setting speed parameter: {e.Message}");
                hasSpeedParameter = false; // Disable to prevent spam
            }
        }
        
        // Update isMoving parameter if it exists
        if (hasIsMovingParameter)
        {
            try
            {
                bool isMoving = currentSpeed > 0.1f; // Small threshold to avoid jitter
                animator.SetBool(isMovingParameterName, isMoving);
                
                if (enableAnimationDebug)
                    Debug.Log($"[ArmouredAI] {name}: Set isMoving to {isMoving}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ArmouredAI] {name}: Error setting isMoving parameter: {e.Message}");
                hasIsMovingParameter = false; // Disable to prevent spam
            }
        }
        
        // Update last position for next frame
        lastPosition = currentPosition;
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
        if (animator != null && hasSpeedParameter)
        {
            try
            {
                animator.SetFloat(speedParameterName, speed);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ArmouredAI] {name}: Error in SetAnimationSpeed: {e.Message}");
            }
        }
    }
    
    public void SetAnimationMoving(bool moving)
    {
        if (animator != null && hasIsMovingParameter)
        {
            try
            {
                animator.SetBool(isMovingParameterName, moving);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ArmouredAI] {name}: Error in SetAnimationMoving: {e.Message}");
            }
        }
    }

    // Method to refresh parameter validation (useful if animator controller changes)
    [ContextMenu("Refresh Animator Parameters")]
    public void RefreshAnimatorParameters()
    {
        ValidateAnimatorParameters();
    }
}
