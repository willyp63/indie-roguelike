using System.Collections.Generic;
using UnityEngine;

public class UnitManager : Singleton<UnitManager>
{
    // Spatial partitioning grid
    private Dictionary<Vector2Int, List<Unit>> spatialGrid =
        new Dictionary<Vector2Int, List<Unit>>();
    private float gridCellSize = 4f; // Adjust based on your unit vision ranges

    // Cached lists to avoid allocations
    private List<Unit> allUnits = new List<Unit>();
    private List<Unit> enemyUnits = new List<Unit>();
    private List<Unit> friendlyUnits = new List<Unit>();

    // Priority queue for newly registered units
    private Queue<Unit> newUnitsPriorityQueue = new Queue<Unit>();

    // Update intervals for batched operations
    [SerializeField]
    private float spatialUpdateInterval = 0.1f;

    [SerializeField]
    private float targetingUpdateInterval = 0.15f;

    // Screen bounds buffer - units must be this much inside screen bounds to be considered "on screen"
    [SerializeField]
    private float screenBuffer = 1f;

    private float lastSpatialUpdate;
    private float lastTargetingUpdate;

    // Batch processing
    private int unitsPerFrameForTargeting = 100; // Process 50 units per frame for targeting
    private int currentTargetingIndex = 0;

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

    private bool IsOnScreen(Vector3 worldPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return true; // If no camera, assume on screen

        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(worldPosition);

        // Check if within viewport bounds with buffer
        float bufferNormalized = screenBuffer / Screen.width; // Normalize buffer to viewport space

        return viewportPoint.x >= -bufferNormalized
            && viewportPoint.x <= 1f + bufferNormalized
            && viewportPoint.y >= -bufferNormalized
            && viewportPoint.y <= 1f + bufferNormalized
            && viewportPoint.z > 0; // Must be in front of camera
    }

    // TODO: adjust the magic numbers in this method
    public Vector2 GetMoveDirection(Unit unit, Vector2 targetDirection)
    {
        // If no waypoint direction, no movement needed
        if (targetDirection.magnitude < 0.1f)
            return targetDirection;

        // Check for units ahead in the movement path
        float lookAheadDistance = 0.5f;

        // Get nearby units for collision avoidance
        List<Unit> nearbyUnits = GetNearbyUnits(
            unit.Health(),
            lookAheadDistance,
            new List<UnitType> { unit.Health().Type() }
        );

        // Check if there's a unit directly ahead
        Unit blockingUnit = null;
        float minDistance = float.MaxValue;

        foreach (Unit nearbyUnit in nearbyUnits)
        {
            if (nearbyUnit == unit)
                continue;

            Vector2 toUnit = nearbyUnit.transform.position - unit.transform.position;
            float distance = toUnit.magnitude;

            // Check if this unit is roughly in our movement direction
            float dotProduct = Vector2.Dot(toUnit.normalized, targetDirection.normalized);

            if (dotProduct > 0.7f && distance < minDistance)
            {
                blockingUnit = nearbyUnit;
                minDistance = distance;
            }
        }

        // If no blocking unit, use waypoint direction
        if (blockingUnit == null)
            return targetDirection;

        // Calculate avoidance direction
        Vector2 toBlockingUnit = blockingUnit.transform.position - unit.transform.position;
        Vector2 perpendicularLeft = new Vector2(-targetDirection.y, targetDirection.x);
        Vector2 perpendicularRight = new Vector2(targetDirection.y, -targetDirection.x);

        // Determine which side to avoid towards based on the blocking unit's position
        float leftDot = Vector2.Dot(toBlockingUnit, perpendicularLeft);
        Vector2 avoidanceDirection = leftDot > 0 ? perpendicularRight : perpendicularLeft;

        // Blend waypoint direction with avoidance direction
        Vector2 blendedDirection = (targetDirection + avoidanceDirection * 1.5f).normalized;

        return blendedDirection;
    }

    public List<Unit> GetNearbyUnits(Health health, float radius, List<UnitType> includeTypes)
    {
        List<Unit> nearbyUnits = new List<Unit>();

        // If the source health is off-screen, return no units
        if (!IsOnScreen(health.transform.position))
        {
            return nearbyUnits; // Return empty list
        }

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
                            && IsOnScreen(unit.transform.position) // Only include units that are on screen
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

    public Unit FindNearestVisibleTarget(Unit searcher)
    {
        var nearbyUnits = GetNearbyUnits(
            searcher.Health(),
            searcher.VisionRange(),
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
                if (HasLineOfSight(searcher, unit.transform.position))
                {
                    closestDistanceSqr = dSqrToUnit;
                    closestUnit = unit;
                }
            }
        }

        return closestUnit;
    }

    public bool HasLineOfSight(Unit unit, Vector3 to)
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
