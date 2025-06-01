using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BasicMovement : MovementBehaviour
{
    private Rigidbody2D rb;
    private Health health;

    static readonly float UNIT_AVOIDANCE_FORCE = 2f;
    static readonly float UNIT_AVOIDANCE_RADIUS = 0.5f;
    static readonly float AVOIDANCE_UPDATE_INTERVAL = 0.3f;

    private Vector2 cachedAvoidanceForce = Vector2.zero;
    private float lastAvoidanceUpdateTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
    }

    public override void Move(GameObject target)
    {
        if (rb == null)
            return;

        Vector2 targetPosition = target.transform.position;

        // Calculate movement force towards target
        Vector2 moveForce = (targetPosition - (Vector2)transform.position).normalized;

        // Update avoidance force only every AVOIDANCE_UPDATE_INTERVAL seconds
        // if (Time.time - lastAvoidanceUpdateTime >= AVOIDANCE_UPDATE_INTERVAL)
        // {
        //     cachedAvoidanceForce = CalculateAvoidanceForce();
        //     lastAvoidanceUpdateTime = Time.time;
        // }

        // Combine forces using cached avoidance force
        Vector2 totalMoveForce = moveForce;

        // Apply the force
        float adjustedSpeed = GetSpeed() * 0.066f;
        Vector2 force = totalMoveForce.normalized * adjustedSpeed * rb.mass / Time.fixedDeltaTime;
        rb.AddForce(force);
    }

    private Vector2 CalculateAvoidanceForce()
    {
        Vector2 avoidanceForce = Vector2.zero;
        var avoidanceRadius = UNIT_AVOIDANCE_RADIUS + health.HitBoxRadius();

        List<Unit> nearbyUnits = UnitManager.Instance.GetNearbyUnits(
            health,
            avoidanceRadius,
            new List<UnitType> { health.Type() }
        );

        foreach (Unit unit in nearbyUnits)
        {
            if (unit.gameObject == gameObject) // Skip self
                continue;

            Vector2 unitPosition = unit.transform.position;
            Vector2 directionAway = (Vector2)transform.position - unitPosition;
            float distance = directionAway.magnitude;

            if (distance < avoidanceRadius && distance > 0)
            {
                // Calculate repulsion force (stronger when closer)
                float repulsionStrength = (avoidanceRadius - distance) / avoidanceRadius;
                Vector2 repulsionForce =
                    directionAway.normalized * repulsionStrength * UNIT_AVOIDANCE_FORCE;
                avoidanceForce += repulsionForce;
            }
        }

        return avoidanceForce;
    }
}
