using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class StandardAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Gun gun;
    public Transform muzzlePoint;

    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Shooting")]
    public float shootInterval = 0.1f;     // time between shots
    public float firingRange = 12f;      // only fire within this distance
    public LayerMask obstacleMask;         // layers that block LOS

    private NavMeshAgent agent;
    private float lastShotTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null || gun == null) return;

        // 1) Always chase
        agent.SetDestination(player.position);
        FacePlayer();

        // 2) Shoot on LOS & range
        float dist = Vector3.Distance(transform.position, player.position);
        if (Time.time - lastShotTime >= shootInterval
            && dist <= firingRange
            && HasLOS())
        {
            lastShotTime = Time.time;
            Vector3 origin = muzzlePoint != null
                             ? muzzlePoint.position
                             : transform.position + Vector3.up * 1.5f;
            Vector3 dir = (player.position - origin).normalized;
            gun.ShootWithDirection(dir);
        }
    }

    private bool HasLOS()
    {
        Vector3 origin = muzzlePoint != null
            ? muzzlePoint.position
            : transform.position + Vector3.up * 1.5f;
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
}
