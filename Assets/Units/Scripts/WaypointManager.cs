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

public class WaypointManager : Singleton<WaypointManager>
{
    [SerializeField]
    private List<WaypointZone> zones;

    public WaypointZone GetZoneForUnit(Unit unit)
    {
        Vector3 unitPosition = unit.transform.position;
        return zones.Find(zone =>
            unit.Health().Type() == zone.unitType && zone.zone.Contains(unitPosition)
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (zones == null)
            return;

        for (int i = 0; i < zones.Count; i++)
        {
            WaypointZone zone = zones[i];

            // Draw zone boundaries with different colors for each zone
            Color zoneColor = GetZoneColor(i);
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

    private Color GetZoneColor(int index)
    {
        // Generate distinct colors for different zones
        Color[] colors =
        {
            Color.blue,
            Color.cyan,
            Color.magenta,
            Color.yellow,
            new Color(1f, 0.5f, 0f), // Orange
            new Color(0.5f, 0f, 1f), // Purple
            new Color(0f, 1f, 0.5f), // Light green
            new Color(1f, 0f, 0.5f), // Pink
        };

        return colors[index % colors.Length];
    }
}
