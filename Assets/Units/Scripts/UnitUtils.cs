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

    public static List<Vector2> GetSpreadPositions(
        Vector2 center,
        int count,
        float radius = 1f,
        bool flipX = false,
        bool rotate90 = true
    )
    {
        List<Vector2> positions = new();

        if (count <= 0)
            return positions;

        if (count == 1)
        {
            positions.Add(center);
            return positions;
        }

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad + (rotate90 ? Mathf.PI / 2f : 0f);
            float x = center.x + radius * Mathf.Cos(angle) * (flipX ? -1f : 1f);
            float y = center.y + radius * Mathf.Sin(angle);
            positions.Add(new Vector2(x, y));
        }

        return positions;
    }
}
