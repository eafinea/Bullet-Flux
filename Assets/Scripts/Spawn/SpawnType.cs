using System.Collections;
using UnityEngine;

public class SpawnType : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject prefabToSpawn;   // assign your Player or Enemy prefab here
    public int spawnCount = 1;
    public float checkRadius = 0.5f;        // how big a “no-spawn” radius around each spawn
    public int maxAttempts = 10;         // tries per instance
    public LayerMask obstacleMask;              // layers we consider “blocking”

    Collider areaCollider;

    void Awake()
    {
        areaCollider = GetComponent<Collider>();
        areaCollider.isTrigger = true;  // can be trigger or not, we only use its bounds
    }

    /// <summary>
    /// Call this to spawn spawnCount copies of prefabToSpawn in this area.
    /// </summary>
    public void Spawn()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            bool placed = false;
            for (int a = 0; a < maxAttempts; a++)
            {
                Vector3 candidate = RandomPointInCollider(areaCollider);
                // look for any colliders in the way
                if (!Physics.CheckSphere(candidate, checkRadius, obstacleMask))
                {
                    Instantiate(prefabToSpawn, candidate, Random.rotation);
                    placed = true;
                    break;
                }
                yield return null; // wait a frame before next attempt
            }
            if (!placed)
                Debug.LogWarning($"[{name}] failed to place {prefabToSpawn.name} #{i + 1}");
        }
    }

    Vector3 RandomPointInCollider(Collider col)
    {
        if (col is BoxCollider box)
        {
            // pick a random point within local box extents
            Vector3 half = box.size * 0.5f;
            Vector3 local = new Vector3(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y),
                Random.Range(-half.z, half.z)
            );
            // transform to world
            return box.transform.TransformPoint(box.center + local);
        }
        else if (col is SphereCollider sph)
        {
            Vector3 dir = Random.onUnitSphere * Random.Range(0f, sph.radius);
            return sph.transform.TransformPoint(sph.center + dir);
        }

        // fallback: uniform in bounds
        var b = col.bounds;
        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            Random.Range(b.min.z, b.max.z)
        );
    }
}
