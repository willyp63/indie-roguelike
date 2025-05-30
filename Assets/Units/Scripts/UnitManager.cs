using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class UnitManager : Singleton<UnitManager>
{
    private static readonly float WAYPOINT_LOOKBACK_DISTANCE = 3f;

    // Spatial partitioning grid
    private Dictionary<Vector2Int, List<Unit>> spatialGrid =
        new Dictionary<Vector2Int, List<Unit>>();
    private float gridCellSize = 4f; // Adjust based on your unit vision ranges

    // Cached lists to avoid allocations
    private List<Unit> allUnits = new List<Unit>();
    private List<Unit> enemyUnits = new List<Unit>();
    private List<Unit> friendlyUnits = new List<Unit>();
    private List<GameObject> cachedWaypoints;

    // Priority queue for newly registered units
    private Queue<Unit> newUnitsPriorityQueue = new Queue<Unit>();

    // Update intervals for batched operations
    public float spatialUpdateInterval = 0.1f;
    public float targetingUpdateInterval = 0.2f;

    private float lastSpatialUpdate;
    private float lastTargetingUpdate;

    // Batch processing
    private int unitsPerFrameForTargeting = 50; // Process 50 units per frame for targeting
    private int currentTargetingIndex = 0;

    void Start()
    {
        var waypoints = GameObject.FindGameObjectsWithTag("Waypoint");
        cachedWaypoints = new List<GameObject>(waypoints);
    }

    void Update()
    {
        // Update spatial grid less frequently
        if (Time.time - lastSpatialUpdate > spatialUpdateInterval)
        {
            UpdateSpatialGrid();
            lastSpatialUpdate = Time.time;
        }

        // Batch process targeting
        if (Time.time - lastTargetingUpdate > targetingUpdateInterval)
        {
            BatchProcessTargeting();
            lastTargetingUpdate = Time.time;
        }
    }

    public void RegisterUnit(Unit unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);

            if (unit.Health().IsEnemy())
                enemyUnits.Add(unit);
            else if (unit.Health().IsFriend())
                friendlyUnits.Add(unit);

            // Add to priority queue for immediate targeting update
            newUnitsPriorityQueue.Enqueue(unit);
        }
    }

    public void UnregisterUnit(Unit unit)
    {
        allUnits.Remove(unit);
        enemyUnits.Remove(unit);
        friendlyUnits.Remove(unit);

        // Remove from spatial grid
        RemoveFromSpatialGrid(unit);
    }

    public void SpawnUnits(Vector2 spawnPosition, Unit unitPrefab, UnitType unitType, int count)
    {
        List<Vector2> spawnPositions = UnitUtils.GetSpreadPositions(spawnPosition, count);
        string unitTypeName = unitType == UnitType.Friend ? "Friendly" : "Enemy";

        foreach (Vector2 pos in spawnPositions)
        {
            // Instantiate the unit at each position
            GameObject spawnedUnit = Instantiate(unitPrefab.gameObject, pos, Quaternion.identity);

            // Set the unit type on the Health component
            Health healthComponent = spawnedUnit.GetComponent<Health>();
            healthComponent?.SetUnitType(unitType);

            Debug.Log($"Spawned {unitTypeName} {unitPrefab.gameObject.name} at {pos}");
        }
    }

    private void UpdateSpatialGrid()
    {
        // Clear and rebuild spatial grid
        spatialGrid.Clear();

        foreach (var unit in allUnits)
        {
            if (unit != null && !unit.Health().IsDead())
            {
                Vector2Int gridPos = WorldToGrid(unit.transform.position);

                if (!spatialGrid.ContainsKey(gridPos))
                    spatialGrid[gridPos] = new List<Unit>();

                spatialGrid[gridPos].Add(unit);
            }
        }
    }

    private void RemoveFromSpatialGrid(Unit unit)
    {
        if (unit == null)
            return;

        Vector2Int gridPos = WorldToGrid(unit.transform.position);
        if (spatialGrid.ContainsKey(gridPos))
        {
            spatialGrid[gridPos].Remove(unit);
            if (spatialGrid[gridPos].Count == 0)
                spatialGrid.Remove(gridPos);
        }
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / gridCellSize),
            Mathf.FloorToInt(worldPos.y / gridCellSize)
        );
    }

    public List<Unit> GetNearbyUnits(Health health, float radius, List<UnitType> includeTypes)
    {
        List<Unit> nearbyUnits = new List<Unit>();
        Vector2Int centerGrid = WorldToGrid(health.transform.position);

        // Check surrounding grid cells
        int cellRadius = Mathf.CeilToInt(radius / gridCellSize) + 1;

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                Vector2Int checkGrid = centerGrid + new Vector2Int(x, y);

                if (spatialGrid.ContainsKey(checkGrid))
                {
                    foreach (var unit in spatialGrid[checkGrid])
                    {
                        if (
                            unit != null
                            && !unit.Health().IsDead()
                            && includeTypes.Contains(unit.Health().Type())
                            && UnitUtils.IsWithinRange(health, unit.Health(), radius)
                        )
                        {
                            nearbyUnits.Add(unit);
                        }
                    }
                }
            }
        }

        return nearbyUnits;
    }

    public Unit FindNearestVisibleTarget(Unit searcher, float visionRange)
    {
        var nearbyUnits = GetNearbyUnits(
            searcher.Health(),
            visionRange,
            new List<UnitType> { searcher.Health().OppositeType() }
        );

        Unit closestUnit = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (Unit unit in nearbyUnits)
        {
            if (unit.Health().OppositeType() != searcher.Health().Type())
                continue;

            float dSqrToUnit = (searcher.transform.position - unit.transform.position).sqrMagnitude;
            if (dSqrToUnit < closestDistanceSqr)
            {
                // Only check line of sight for the closest candidate to save performance
                if (HasLineOfSightFast(searcher, unit.transform.position))
                {
                    closestDistanceSqr = dSqrToUnit;
                    closestUnit = unit;
                }
            }
        }

        return closestUnit;
    }

    private bool HasLineOfSightFast(Unit unit, Vector3 to)
    {
        Vector3 direction = to - unit.transform.position;
        float distance = direction.magnitude;
        Vector3 normalizedDir = direction.normalized;

        // Calculate perpendicular offset
        Vector3 perpendicular =
            new Vector3(-normalizedDir.y, normalizedDir.x, 0) * unit.Health().HitBoxRadius();

        // Cast two rays offset perpendicular to direction
        RaycastHit2D hit1 = Physics2D.Raycast(
            unit.transform.position + perpendicular,
            normalizedDir,
            distance,
            LayerMask.GetMask("Wall")
        );

        RaycastHit2D hit2 = Physics2D.Raycast(
            unit.transform.position - perpendicular,
            normalizedDir,
            distance,
            LayerMask.GetMask("Wall")
        );

        return hit1.collider == null && hit2.collider == null;
    }

    public GameObject FindBestWaypoint(Unit unit)
    {
        if (cachedWaypoints.Count == 0)
            return null;

        GameObject bestWaypoint = null;
        float bestDistanceScore = 0f;
        bool isEnemy = unit.Health().IsEnemy();

        foreach (GameObject waypoint in cachedWaypoints)
        {
            if (waypoint == null)
                continue;

            Vector2 waypointPos = waypoint.transform.position;

            // Check direction
            if (isEnemy)
            {
                if (waypointPos.x >= unit.transform.position.x + WAYPOINT_LOOKBACK_DISTANCE) // WAYPOINT_LOOKBACK_DISTANCE
                    continue;
            }
            else
            {
                if (waypointPos.x <= unit.transform.position.x - WAYPOINT_LOOKBACK_DISTANCE)
                    continue;
            }

            // Calculate distance score
            float distanceScore = (waypointPos.x - unit.transform.position.x) * (isEnemy ? -1 : 1);

            if (
                bestWaypoint == null
                || distanceScore > bestDistanceScore
                || (
                    Mathf.Approximately(distanceScore, bestDistanceScore)
                    && Vector3.Distance(unit.transform.position, waypointPos)
                        < Vector3.Distance(unit.transform.position, bestWaypoint.transform.position)
                )
            )
            {
                if (HasLineOfSightFast(unit, waypointPos))
                {
                    bestWaypoint = waypoint;
                    bestDistanceScore = distanceScore;
                }
            }
        }

        return bestWaypoint;
    }

    private void BatchProcessTargeting()
    {
        if (allUnits.Count == 0 && newUnitsPriorityQueue.Count == 0)
            return;

        // Process priority queue first (newly registered units)
        int processedFromQueue = 0;
        int maxQueueProcessing = unitsPerFrameForTargeting; // Allow processing all queue items up to the frame limit

        while (newUnitsPriorityQueue.Count > 0 && processedFromQueue < maxQueueProcessing)
        {
            var unit = newUnitsPriorityQueue.Dequeue();
            if (unit != null && !unit.Health().IsDead())
            {
                unit.UpdateTargetFromManager();
                processedFromQueue++;
            }
        }

        // Process remaining units from regular batch if we have processing capacity left
        int remainingCapacity = unitsPerFrameForTargeting - processedFromQueue;
        if (remainingCapacity > 0 && allUnits.Count > 0)
        {
            int endIndex = Mathf.Min(currentTargetingIndex + remainingCapacity, allUnits.Count);

            for (int i = currentTargetingIndex; i < endIndex; i++)
            {
                var unit = allUnits[i];
                if (unit != null && !unit.Health().IsDead())
                {
                    unit.UpdateTargetFromManager();
                }
            }

            currentTargetingIndex = endIndex;
            if (currentTargetingIndex >= allUnits.Count)
                currentTargetingIndex = 0;
        }
    }
}
