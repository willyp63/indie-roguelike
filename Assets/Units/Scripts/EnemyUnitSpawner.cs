using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyUnitWave
{
    public Unit unitPrefab;

    public int count = 1; // How many units to spawn

    public int power = 1; // How strong the wave is

    public int timeBeforeFirstSpawn = 0; // How many seconds to wait before this wave can spawn
}

[System.Serializable]
public class SpawnBounds
{
    public string name = "Spawn Area";
    public Vector2 center;
    public Vector2 size;

    public Vector2 GetRandomPosition()
    {
        float randomX = Random.Range(center.x - size.x / 2f, center.x + size.x / 2f);
        float randomY = Random.Range(center.y - size.y / 2f, center.y + size.y / 2f);
        return new Vector2(randomX, randomY);
    }

    public Rect GetRect()
    {
        return new Rect(center.x - size.x / 2f, center.y - size.y / 2f, size.x, size.y);
    }
}

public class EnemyUnitSpawner : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField]
    private List<SpawnBounds> spawnBounds = new List<SpawnBounds>();

    [SerializeField]
    private List<EnemyUnitWave> unitWaves = new List<EnemyUnitWave>();

    [Header("Weighted Selection")]
    [SerializeField]
    private float weightIncreasePerSkip = 2f; // How much weight increases when not selected

    // Private fields for tracking
    private List<float> waveWeights = new List<float>();
    private List<float> boundsWeights = new List<float>();
    private int totalPowerSpawned = 0;
    private float startTime;

    private void Start()
    {
        InitializeWeights();

        unitWaves.Sort((a, b) => b.timeBeforeFirstSpawn.CompareTo(a.timeBeforeFirstSpawn));

        startTime = Time.time;

        StartCoroutine(SpawnRoutine());
    }

    private void InitializeWeights()
    {
        // Initialize all weights to 1
        waveWeights.Clear();
        boundsWeights.Clear();

        for (int i = 0; i < unitWaves.Count; i++)
        {
            waveWeights.Add(1f);
        }

        for (int i = 0; i < spawnBounds.Count; i++)
        {
            boundsWeights.Add(1f);
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // Keep spawning waves until we hit the power cap
            while (
                GetCurrentPowerPerSecond() < GetTargetPowerPerSecond()
                && unitWaves.Count > 0
                && spawnBounds.Count > 0
            )
            {
                // Get eligible waves (those that have passed their spawn delay)
                List<int> eligibleWaveIndices = GetEligibleWaveIndices();

                if (eligibleWaveIndices.Count == 0)
                {
                    // No waves are eligible yet, wait a bit and try again
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                // Select wave and bounds using weighted selection from eligible waves only
                int selectedWaveIndex = SelectWeightedIndex(waveWeights, eligibleWaveIndices);
                int selectedBoundsIndex = SelectWeightedIndex(boundsWeights);

                EnemyUnitWave selectedWave = unitWaves[selectedWaveIndex];
                SpawnBounds selectedBounds = spawnBounds[selectedBoundsIndex];

                SpawnWave(selectedWave, selectedBounds);

                // Track the spawned wave power
                totalPowerSpawned += selectedWave.power;

                // Increase weights for non-selected items
                //   & reset weights for selected items
                IncreaseWeights();
                waveWeights[selectedWaveIndex] = 1f;
                boundsWeights[selectedBoundsIndex] = 1f;
            }

            // Calculate how long we need to wait to get back under the power cap
            float waitTime = CalculateWaitTime();

            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                // Small delay to prevent infinite tight loops if something goes wrong
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private List<int> GetEligibleWaveIndices()
    {
        List<int> eligibleIndices = new List<int>();
        float elapsedTime = GetElapsedTime();

        // Dictionary to track the highest count wave for each unit prefab
        Dictionary<Unit, int> highestCountWaveIndex = new Dictionary<Unit, int>();

        for (int i = 0; i < unitWaves.Count; i++)
        {
            if (elapsedTime >= unitWaves[i].timeBeforeFirstSpawn)
            {
                Unit unitPrefab = unitWaves[i].unitPrefab;

                // If we haven't seen this unit prefab yet, or this wave has a higher count
                if (
                    !highestCountWaveIndex.ContainsKey(unitPrefab)
                    || unitWaves[i].count > unitWaves[highestCountWaveIndex[unitPrefab]].count
                )
                {
                    highestCountWaveIndex[unitPrefab] = i;
                }
            }
        }

        // Add the highest count wave indices to eligible list
        foreach (var kvp in highestCountWaveIndex)
        {
            eligibleIndices.Add(kvp.Value);
        }

        return eligibleIndices;
    }

    private int SelectWeightedIndex(List<float> weights, List<int> eligibleIndices = null)
    {
        // If no eligible indices provided, create list of all indices
        if (eligibleIndices == null)
        {
            eligibleIndices = new List<int>();
            for (int i = 0; i < weights.Count; i++)
            {
                eligibleIndices.Add(i);
            }
        }

        if (eligibleIndices.Count == 0)
            return -1;

        // Calculate total weight for eligible waves only
        float totalWeight = 0f;
        foreach (int index in eligibleIndices)
        {
            totalWeight += weights[index];
        }

        if (totalWeight <= 0f)
            return eligibleIndices[0]; // Fallback to first eligible wave

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (int index in eligibleIndices)
        {
            currentWeight += weights[index];
            if (randomValue <= currentWeight)
            {
                return index;
            }
        }

        return eligibleIndices[eligibleIndices.Count - 1]; // Fallback to last eligible index
    }

    private float CalculateWaitTime()
    {
        if (totalPowerSpawned == 0)
            return 0f;

        // Calculate the minimum total time needed for our power/sec to equal the target
        float requiredTotalTime = (float)totalPowerSpawned / GetTargetPowerPerSecond();
        float currentElapsedTime = Time.time - startTime;

        // Wait time is the difference between required time and current time
        float waitTime = requiredTotalTime - currentElapsedTime;

        return Mathf.Max(0f, waitTime);
    }

    private float GetCurrentPowerPerSecond()
    {
        float elapsedTime = Time.time - startTime;
        if (elapsedTime <= 0f)
            elapsedTime = 0.01f;

        return (float)totalPowerSpawned / elapsedTime;
    }

    private void IncreaseWeights()
    {
        for (int i = 0; i < waveWeights.Count; i++)
        {
            waveWeights[i] += weightIncreasePerSkip;
        }

        for (int i = 0; i < boundsWeights.Count; i++)
        {
            boundsWeights[i] += weightIncreasePerSkip;
        }
    }

    public void SpawnWave(EnemyUnitWave wave, SpawnBounds bounds)
    {
        Vector2 spawnPosition = bounds.GetRandomPosition();
        float unitRadius = wave.unitPrefab.GetComponent<CircleCollider2D>().radius;
        List<Vector2> spawnPositions = UnitUtils.GetSpreadPositions(
            spawnPosition,
            wave.count,
            unitRadius
        );

        UnitManager.Instance.SpawnUnits(spawnPositions, wave.unitPrefab, UnitType.Enemy);
    }

    public float GetTargetPowerPerSecond()
    {
        float elapsedTime = GetElapsedTime();

        // TODO: move to config variables
        // https://www.wolframalpha.com/input?i=y%3D%2810000%2Bx%5E1.5%29+%2F+10000+from+0+to+1200
        return (10_000 + Mathf.Pow(elapsedTime, 1.5f)) / 5_000;
    }

    public float GetElapsedTime()
    {
        return Time.time - startTime;
    }

    // Gizmos for visualizing spawn bounds in the Scene view
    private void OnDrawGizmosSelected()
    {
        if (spawnBounds == null)
            return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

        foreach (var bounds in spawnBounds)
        {
            Gizmos.DrawCube(bounds.center, bounds.size);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        }
    }
}
