using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaypointZone
{
    public UnitType unitType;

    public Bounds zone;

    public List<Waypoint> waypoints;
}

[System.Serializable]
public struct WaypointGridCell
{
    public Vector2 friendDirection;
    public Vector2 enemyDirection;
    public bool hasFriendWaypoint;
    public bool hasEnemyWaypoint;

    public WaypointGridCell(Vector2 friendDir, Vector2 enemyDir, bool hasFriend, bool hasEnemy)
    {
        friendDirection = friendDir;
        enemyDirection = enemyDir;
        hasFriendWaypoint = hasFriend;
        hasEnemyWaypoint = hasEnemy;
    }
}

public class WaypointManager : Singleton<WaypointManager>
{
    [Header("Zone Configuration")]
    [SerializeField]
    private List<WaypointZone> zones;

    [Header("Grid Configuration")]
    [SerializeField]
    private float cellSize = 1f;

    [SerializeField]
    private Vector2 gridOrigin = Vector2.zero;

    [SerializeField]
    private Vector2Int gridSize = new Vector2Int(100, 100);

    [Header("Visualization")]
    [SerializeField]
    private bool showFriendZones = false;

    [SerializeField]
    private bool showEnemyZones = false;

    [SerializeField]
    private bool showFriendArrows = false;

    [SerializeField]
    private bool showEnemyArrows = false;

    [SerializeField]
    private float arrowSize = 0.4f;

    // Grid data storage
    private Dictionary<Vector2Int, WaypointGridCell> grid =
        new Dictionary<Vector2Int, WaypointGridCell>();

    private void Start()
    {
        ComputeWaypointGrid();
    }

    public float GetCellSize() => cellSize;

    public Vector2Int GetGridSize() => gridSize;

    public Vector2 GetGridOrigin() => gridOrigin;

    public WaypointZone GetZone(UnitType unitType, Vector2 unitPosition)
    {
        return zones.Find(zone => unitType == zone.unitType && zone.zone.Contains(unitPosition));
    }

    public void ComputeWaypointGrid()
    {
        grid.Clear();

        // Calculate grid bounds
        Vector2 minBounds =
            gridOrigin - new Vector2(gridSize.x * cellSize * 0.5f, gridSize.y * cellSize * 0.5f);

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector2 worldPos =
                    minBounds
                    + new Vector2(x * cellSize + cellSize * 0.5f, y * cellSize + cellSize * 0.5f);

                // Compute best waypoint for each unit type
                Vector2 friendDirection = ComputeBestWaypointDirection(worldPos, UnitType.Friend);
                Vector2 enemyDirection = ComputeBestWaypointDirection(worldPos, UnitType.Enemy);

                WaypointGridCell cell = new WaypointGridCell(
                    friendDirection,
                    enemyDirection,
                    friendDirection != Vector2.zero,
                    enemyDirection != Vector2.zero
                );

                grid[gridPos] = cell;
            }
        }

        Debug.Log($"Computed waypoint grid with {grid.Count} cells");
    }

    private Vector2 ComputeBestWaypointDirection(Vector2 worldPosition, UnitType unitType)
    {
        // Find the best waypoint using existing logic
        WaypointZone zone = GetZone(unitType, worldPosition);
        Vector2 bestDirection = Vector2.zero;

        if (zone != null)
        {
            Waypoint bestWaypoint = null;
            int highestPriority = int.MinValue;
            float closestDistance = float.MaxValue;

            foreach (Waypoint waypoint in zone.waypoints)
            {
                if (waypoint == null)
                    continue;

                // Check line of sight (simplified - just check for walls)
                if (!HasLineOfSight(worldPosition, waypoint.transform.position))
                    continue;

                // Check if this waypoint has higher priority
                if (waypoint.Priority() > highestPriority)
                {
                    bestWaypoint = waypoint;
                    highestPriority = waypoint.Priority();
                    closestDistance = Vector3.Distance(worldPosition, waypoint.transform.position);
                }
                // If same priority, choose the closer one
                else if (waypoint.Priority() == highestPriority)
                {
                    float distance = Vector3.Distance(worldPosition, waypoint.transform.position);
                    if (distance < closestDistance)
                    {
                        bestWaypoint = waypoint;
                        closestDistance = distance;
                    }
                }
            }

            if (bestWaypoint != null)
            {
                bestDirection = (
                    bestWaypoint.transform.position - (Vector3)worldPosition
                ).normalized;
            }
        }

        return bestDirection;
    }

    private bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        Vector2 direction = to - from;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(
            from,
            direction.normalized,
            distance,
            LayerMask.GetMask("Wall")
        );
        return hit.collider == null;
    }

    public Vector2 GetWaypointDirection(Vector3 worldPosition, UnitType unitType)
    {
        Vector2Int gridPos = WorldToGrid(worldPosition);

        if (grid.TryGetValue(gridPos, out WaypointGridCell cell))
        {
            switch (unitType)
            {
                case UnitType.Friend:
                    return cell.hasFriendWaypoint ? cell.friendDirection : Vector2.zero;
                case UnitType.Enemy:
                    return cell.hasEnemyWaypoint ? cell.enemyDirection : Vector2.zero;
                default:
                    return Vector2.zero;
            }
        }

        return Vector2.zero;
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector2 minBounds =
            gridOrigin - new Vector2(gridSize.x * cellSize * 0.5f, gridSize.y * cellSize * 0.5f);
        Vector2 relativePos = (Vector2)worldPos - minBounds;

        return new Vector2Int(
            Mathf.FloorToInt(relativePos.x / cellSize),
            Mathf.FloorToInt(relativePos.y / cellSize)
        );
    }

    private Vector2 GridToWorld(Vector2Int gridPos)
    {
        Vector2 minBounds =
            gridOrigin - new Vector2(gridSize.x * cellSize * 0.5f, gridSize.y * cellSize * 0.5f);
        return minBounds
            + new Vector2(
                gridPos.x * cellSize + cellSize * 0.5f,
                gridPos.y * cellSize + cellSize * 0.5f
            );
    }

    [ContextMenu("Recompute Grid")]
    public void RecomputeGrid()
    {
        ComputeWaypointGrid();
    }

    private void OnDrawGizmosSelected()
    {
        DrawZones();
        DrawGridArrows();
    }

    private void DrawGridArrows()
    {
        Camera cam = Camera.main;
        Vector3 cameraPos = cam != null ? cam.transform.position : Vector3.zero;

        foreach (var kvp in grid)
        {
            Vector2Int gridPos = kvp.Key;
            WaypointGridCell cell = kvp.Value;
            Vector2 worldPos = GridToWorld(gridPos);

            // Draw friend direction arrow
            if (showFriendArrows && cell.hasFriendWaypoint)
            {
                Gizmos.color = Color.green;
                DrawArrow(worldPos, cell.friendDirection, arrowSize * 0.5f);
            }

            // Draw enemy direction arrow
            if (showEnemyArrows && cell.hasEnemyWaypoint)
            {
                Gizmos.color = Color.red;
                DrawArrow(worldPos, cell.enemyDirection, arrowSize * 0.5f);
            }
        }
    }

    private void DrawArrow(Vector2 position, Vector2 direction, float size)
    {
        if (direction == Vector2.zero)
            return;

        Vector2 arrowHead = position + direction * size;

        // Draw main line
        Gizmos.DrawLine(position, arrowHead);

        // Draw arrowhead
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        Vector2 arrowTip1 = arrowHead - direction * size * 0.3f + perpendicular * size * 0.2f;
        Vector2 arrowTip2 = arrowHead - direction * size * 0.3f - perpendicular * size * 0.2f;

        Gizmos.DrawLine(arrowHead, arrowTip1);
        Gizmos.DrawLine(arrowHead, arrowTip2);
    }

    private void DrawZones()
    {
        if (zones == null)
            return;

        for (int i = 0; i < zones.Count; i++)
        {
            WaypointZone zone = zones[i];

            if (zone.unitType == UnitType.Enemy && !showEnemyZones)
                continue;
            if (zone.unitType == UnitType.Friend && !showFriendZones)
                continue;

            // Draw zone boundaries with different colors for each zone
            Color zoneColor = zone.unitType == UnitType.Friend ? Color.green : Color.red;
            Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
            Gizmos.DrawCube(zone.zone.center, zone.zone.size);

            // Draw zone wireframe
            Gizmos.color = zoneColor;
            Gizmos.DrawWireCube(zone.zone.center, zone.zone.size);

            // Draw waypoints within the zone
            if (zone.waypoints != null)
            {
                foreach (Waypoint waypoint in zone.waypoints)
                {
                    if (waypoint == null)
                        continue;

                    // Color waypoints based on priority (higher priority = more red)
                    float priorityNormalized = Mathf.Clamp01(waypoint.Priority() / 10f);
                    Gizmos.color = Color.Lerp(Color.green, Color.red, priorityNormalized);

                    Vector3 waypointPos = new Vector3(
                        waypoint.transform.position.x,
                        waypoint.transform.position.y,
                        0
                    );

                    // Draw waypoint as sphere
                    Gizmos.DrawSphere(waypointPos, 0.5f);

                    // Draw wireframe for better visibility
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(waypointPos, 0.5f);
                }
            }
        }
    }
}
