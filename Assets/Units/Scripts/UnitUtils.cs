using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum UnitType
{
    Friend,
    Enemy,
    Neutral,
}

public enum Direction
{
    Right,
    Up,
    Left,
    Down,
}

public static class UnitUtils
{
    private const float UNIT_SPACING_MULTIPLIER = 2.5f;

    public static bool IsWithinRange(Health health1, Health health2, float range)
    {
        float sqrDistance = (health1.transform.position - health2.transform.position).sqrMagnitude;
        float adjustedRange = range + health1.HitBoxRadius() + health2.HitBoxRadius(); // range is distance to `health`'s colliders
        return sqrDistance <= adjustedRange * adjustedRange;
    }

    public static bool IsWithinRange(Vector3 position, Health health, float range)
    {
        float sqrDistance = (position - health.transform.position).sqrMagnitude;
        float adjustedRange = range + health.HitBoxRadius(); // range is distance to `health collider
        return sqrDistance <= adjustedRange * adjustedRange;
    }

    public static UnitType GetOppositeUnitType(UnitType unitType)
    {
        if (unitType == UnitType.Neutral)
            return UnitType.Neutral;

        return unitType == UnitType.Enemy ? UnitType.Friend : UnitType.Enemy;
    }

    public static Direction GetDirection(Vector2 startPos, Vector2 endPos)
    {
        // Calculate direction vector
        Vector3 direction = endPos - startPos;

        // Determine which axis has the larger absolute value
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            // Vertical movement is dominant
            return direction.y > 0 ? Direction.Up : Direction.Down;
        }

        // Horizontal movement is dominant
        return direction.x > 0 ? Direction.Right : Direction.Left;
    }

    public static bool IsValidTarget(Health attacker, Health target)
    {
        if (attacker == target || target.IsDead())
            return false;

        return IsValidTarget(attacker.Type(), target.Type());
    }

    public static bool IsValidTarget(UnitType attackerType, UnitType targetType)
    {
        return attackerType == UnitType.Neutral
            || targetType == UnitType.Neutral
            || (attackerType != targetType);
    }

    public static List<Vector2> GetSpreadPositions(Vector2 center, int count, float unitRadius)
    {
        List<Vector2> positions = new List<Vector2>();

        if (count <= 0)
            return positions;

        // Single unit - place at center
        if (count == 1)
        {
            positions.Add(center);
            return positions;
        }

        // Two units - place side by side
        if (count == 2)
        {
            float unitSpacing = unitRadius * UNIT_SPACING_MULTIPLIER; // Slight gap between units
            positions.Add(center + Vector2.up * unitSpacing * 0.5f);
            positions.Add(center + Vector2.down * unitSpacing * 0.5f);
            return positions;
        }

        // Small groups (3-8 units) - use circular formation
        if (count <= 8)
        {
            float radius = Mathf.Max(unitRadius * 2f, unitRadius * count * 0.5f);

            for (int i = 0; i < count; i++)
            {
                float angle = (2f * Mathf.PI * i) / count - Mathf.PI / 2f; // Rotate by -90 degrees for y-axis symmetry
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                positions.Add(center + offset);
            }
            return positions;
        }

        // Large groups (9+ units) - use grid formation
        int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / columns);
        float spacing = unitRadius * UNIT_SPACING_MULTIPLIER;

        // Calculate grid offset to center the formation
        Vector2 gridSize = new Vector2((columns - 1) * spacing, (rows - 1) * spacing);
        Vector2 gridStart = center - gridSize * 0.5f;

        int unitIndex = 0;
        for (int row = 0; row < rows && unitIndex < count; row++)
        {
            // Calculate how many units will be in this row
            int unitsInThisRow = Mathf.Min(columns, count - unitIndex);

            // Calculate offset to center incomplete rows
            float rowCenterOffset = (columns - unitsInThisRow) * spacing * 0.5f;

            for (int col = 0; col < unitsInThisRow; col++)
            {
                Vector2 position =
                    gridStart + new Vector2(col * spacing + rowCenterOffset, row * spacing);
                positions.Add(position);
                unitIndex++;
            }
        }

        return positions;
    }
}
