using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedUnitPrefab
{
    public Unit unitPrefab;

    [Range(0.1f, 10f)]
    public float weight = 1f;
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
    private List<WeightedUnitPrefab> unitPrefabs = new List<WeightedUnitPrefab>();

    [SerializeField]
    private List<SpawnBounds> spawnBounds = new List<SpawnBounds>();

    [Header("Timing")]
    [SerializeField]
    private float spawnInterval = 3f;

    [SerializeField]
    private float initialDelay = 1f;

    [SerializeField]
    private bool spawnOnStart = true;

    [Header("Limits")]
    [SerializeField]
    private int maxActiveUnits = 20;

    [SerializeField]
    private bool respectMaxUnits = true;

    private float totalWeight;
    private List<GameObject> spawnedUnits = new List<GameObject>();
    private Coroutine spawningCoroutine;

    private void Start()
    {
        CalculateTotalWeight();

        if (spawnOnStart)
        {
            StartSpawning();
        }
    }

    private void CalculateTotalWeight()
    {
        totalWeight = 0f;
        foreach (var weightedPrefab in unitPrefabs)
        {
            if (weightedPrefab.unitPrefab != null)
            {
                totalWeight += weightedPrefab.weight;
            }
        }
    }

    public void StartSpawning()
    {
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
        }

        spawningCoroutine = StartCoroutine(SpawnCoroutine());
    }

    public void StopSpawning()
    {
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }
    }

    private IEnumerator SpawnCoroutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // Clean up destroyed units from our tracking list
            CleanupDestroyedUnits();

            // Check if we should spawn more units
            if (!respectMaxUnits || spawnedUnits.Count < maxActiveUnits)
            {
                SpawnRandomUnit();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void CleanupDestroyedUnits()
    {
        for (int i = spawnedUnits.Count - 1; i >= 0; i--)
        {
            if (spawnedUnits[i] == null)
            {
                spawnedUnits.RemoveAt(i);
            }
        }
    }

    public void SpawnRandomUnit()
    {
        if (unitPrefabs.Count == 0 || spawnBounds.Count == 0)
        {
            Debug.LogWarning("EnemyUnitSpawner: No unit prefabs or spawn bounds configured!");
            return;
        }

        Unit selectedPrefab = GetRandomWeightedPrefab();
        if (selectedPrefab == null)
        {
            Debug.LogWarning("EnemyUnitSpawner: Failed to select a valid prefab!");
            return;
        }

        SpawnBounds selectedBounds = GetRandomSpawnBounds();
        Vector2 spawnPosition = selectedBounds.GetRandomPosition();

        GameObject spawnedUnit = Instantiate(
            selectedPrefab.gameObject,
            spawnPosition,
            Quaternion.identity,
            transform
        );
        Health health = spawnedUnit.GetComponent<Health>();
        health.SetUnitType(UnitType.Enemy);
        spawnedUnits.Add(spawnedUnit);
        Debug.Log(
            $"Spawned {selectedPrefab.name} at {spawnPosition} in bounds '{selectedBounds.name}'"
        );
    }

    private Unit GetRandomWeightedPrefab()
    {
        if (totalWeight <= 0f)
        {
            CalculateTotalWeight();
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var weightedPrefab in unitPrefabs)
        {
            if (weightedPrefab.unitPrefab == null)
                continue;

            currentWeight += weightedPrefab.weight;
            if (randomValue <= currentWeight)
            {
                return weightedPrefab.unitPrefab;
            }
        }

        // Fallback: return the last valid prefab
        for (int i = unitPrefabs.Count - 1; i >= 0; i--)
        {
            if (unitPrefabs[i].unitPrefab != null)
            {
                return unitPrefabs[i].unitPrefab;
            }
        }

        return null;
    }

    private SpawnBounds GetRandomSpawnBounds()
    {
        int randomIndex = Random.Range(0, spawnBounds.Count);
        return spawnBounds[randomIndex];
    }

    public void ClearAllSpawnedUnits()
    {
        foreach (var unit in spawnedUnits)
        {
            if (unit != null)
            {
                Destroy(unit);
            }
        }
        spawnedUnits.Clear();
    }

    public int GetActiveUnitCount()
    {
        CleanupDestroyedUnits();
        return spawnedUnits.Count;
    }

    public void SetSpawnInterval(float newInterval)
    {
        spawnInterval = newInterval;
    }

    public void SetMaxActiveUnits(int newMax)
    {
        maxActiveUnits = newMax;
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
