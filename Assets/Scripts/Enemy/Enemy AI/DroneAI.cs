using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DroneAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Gun gun;
    public Transform muzzlePoint;

    [Header("Flight")]
    public float hoverRadius = 5f;
    public float hoverSpeed = 4f;
    public float hoverAltitude = 4f;

    [Header("Shooting")]
    public float timeBetweenShots = 2f;
    public float shootSpread = 0.1f;
    public float firingRange = 15f;
    public LayerMask obstacleMask;

    private NavMeshAgent agent;
    private Vector2 offset;
    private float nextShotTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = hoverSpeed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;

        float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        offset = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * hoverRadius;
        nextShotTime = Time.time + timeBetweenShots;
    }

    void Update()
    {
        if (player == null || gun == null) return;

        // 1) Orbit
        float rot = hoverSpeed * Time.deltaTime * 50f;
        offset = Quaternion.Euler(0, rot, 0) * new Vector3(offset.x, 0, offset.y);
        Vector3 desiredXZ = player.position + new Vector3(offset.x, 0, offset.y);

        if (NavMesh.SamplePosition(desiredXZ, out var hit, hoverRadius, NavMesh.AllAreas))
        {
            Vector3 target = hit.position;
            target.y = player.position.y + hoverAltitude;
            agent.SetDestination(target);
        }

        // 2) Maintain altitude
        transform.position = new Vector3(
            agent.nextPosition.x,
            player.position.y + hoverAltitude,
            agent.nextPosition.z
        );

        // 3) Face
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDir),
                Time.deltaTime * 5f
            );

        // 4) Shoot on LOS & range
        float dist = Vector3.Distance(transform.position, player.position);
        if (Time.time >= nextShotTime
            && dist <= firingRange
            && HasLOS())
        {
            nextShotTime = Time.time + timeBetweenShots;
            Vector3 origin = muzzlePoint != null
                             ? muzzlePoint.position
                             : transform.position + Vector3.up * 1.5f;
            Vector3 dir = (player.position - origin).normalized;
            Vector3 spread = new Vector3(
                Random.Range(-shootSpread, shootSpread),
                Random.Range(-shootSpread, shootSpread),
                Random.Range(-shootSpread, shootSpread)
            );
            gun.ShootWithDirection((dir + spread).normalized);
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
}
