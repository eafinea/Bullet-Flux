using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpawnArea : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("How many attempts to find a free spot per instance")]
    public int maxAttempts = 10;
    [Tooltip("Radius to check for existing colliders")]
    public float checkRadius = 0.5f;
    [Tooltip("Layers considered blocking (Player/Enemy/World)")]
    public LayerMask obstacleMask;

    Collider areaCollider;

    void Awake()
    {
        areaCollider = GetComponent<Collider>();
        areaCollider.isTrigger = true;
    }

    /// Spawns 'count' copies of 'prefab' randomly within this collider’s volume,
    /// avoiding overlaps with obstacleMask.
    public void Spawn(GameObject prefab, int count = 1)
    {
        StartCoroutine(SpawnRoutine(prefab, count));
    }

    IEnumerator SpawnRoutine(GameObject prefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 candidate = RandomPointInCollider(areaCollider);
                if (!Physics.CheckSphere(candidate, checkRadius, obstacleMask))
                {
                    Instantiate(prefab, candidate, Quaternion.identity);
                    placed = true;
                    break;
                }
                yield return null; // wait a frame and try again
            }
            if (!placed)
                Debug.LogWarning($"[{name}] could not place {prefab.name} #{i + 1} after {maxAttempts} attempts");
        }
    }

    Vector3 RandomPointInCollider(Collider col)
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
}
