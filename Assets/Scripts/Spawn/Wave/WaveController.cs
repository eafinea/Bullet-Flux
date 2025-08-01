using UnityEngine;

public enum EnemyType
{
    Standard = 0,
    Armoured = 1,
    Drone = 2
}

public class WaveController : MonoBehaviour
{
    [Header("General")]
    [Tooltip("How many waves total (1..n).")]
    public int totalWaves = 10;

    [Header("Caps per Wave")]
    [Tooltip("Standards = base + perWave * waveIndex")]
    public int baseStandard = 5;
    public int standardPerWave = 2;
    [Tooltip("Armoured = base + perWave * waveIndex")]
    public int baseArmoured = 1;
    public int armouredPerWave = 1;
    [Tooltip("Drones = base + perWave * waveIndex")]
    public int baseDrone = 0;
    public int dronePerWave = 1;

    [Header("Spawn Weights Over Time")]
    [Tooltip("X=0 (wave1) → X=1 (waveN)")]
    public AnimationCurve standardWeight = AnimationCurve.Linear(0, 1, 1, 1);
    public AnimationCurve armouredWeight = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve droneWeight = AnimationCurve.Linear(0, 0, 1, 0);

    int waveIndex;
    int[] spawnedCount;

    /// <summary>Call at the start of each wave (0-based).</summary>
    public void StartWave(int index)
    {
        waveIndex = Mathf.Clamp(index, 0, totalWaves - 1);
        spawnedCount = new int[3]; // Standard, Armoured, Drone
    }

    /// <summary>
    /// Returns the next type to spawn based on curves & caps,
    /// or null if the wave is exhausted.
    /// </summary>
    public EnemyType? GetNextSpawnType()
    {
        // normalized progress 0→1 across all waves
        float t = totalWaves > 1
            ? (float)waveIndex / (totalWaves - 1)
            : 1f;

        // compute caps for this wave
        int capStd = baseStandard + standardPerWave * waveIndex;
        int capArm = baseArmoured + armouredPerWave * waveIndex;
        int capDr = baseDrone + dronePerWave * waveIndex;

        // sample weights, zero if at cap
        float wStd = spawnedCount[0] < capStd ? standardWeight.Evaluate(t) : 0f;
        float wArm = spawnedCount[1] < capArm ? armouredWeight.Evaluate(t) : 0f;
        float wDr = spawnedCount[2] < capDr ? droneWeight.Evaluate(t) : 0f;

        float totalW = wStd + wArm + wDr;
        if (totalW <= 0f) return null; // all types are exhausted

        // weighted random pick
        float r = Random.value * totalW;
        if (r < wStd) return EnemyType.Standard;
        else if (r < wStd + wArm) return EnemyType.Armoured;
        else return EnemyType.Drone;
    }

    /// <summary>Notify the controller that you spawned one of the given type.</summary>
    public void OnSpawn(EnemyType type)
    {
        int i = (int)type;
        if (i >= 0 && i < spawnedCount.Length)
            spawnedCount[i]++;
    }

    /// <summary>True once each type has reached its cap for this wave.</summary>
    public bool IsWaveComplete
    {
        get
        {
            int capStd = baseStandard + standardPerWave * waveIndex;
            int capArm = baseArmoured + armouredPerWave * waveIndex;
            int capDr = baseDrone + dronePerWave * waveIndex;

            return spawnedCount[0] >= capStd
                && spawnedCount[1] >= capArm
                && spawnedCount[2] >= capDr;
        }
    }
}
