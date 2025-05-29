using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashAttack : AttackBehaviour
{
    [SerializeField]
    private Collider2D downAttackCollider;

    [SerializeField]
    private Collider2D sideAttackCollider;

    [SerializeField]
    private Collider2D upAttackCollider;

    [SerializeField]
    private float minAttackRange = 2.0f;

    [SerializeField]
    private float dashDuration = 0.3f;

    [SerializeField]
    private float extraDashLength = 1.0f;

    private Rigidbody2D rb;
    private HashSet<Health> alreadyHitTargets = new();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override Health GetTarget(Unit targetUnit)
    {
        if (targetUnit == null)
            return null;

        var nearbyUnits = UnitManager.Instance.GetNearbyUnits(
            health,
            attackRange + health.HitBoxRadius(),
            new List<UnitType> { health.OppositeType() }
        );

        Unit furthestUnit = null;
        float furthestDistanceSqr = Mathf.NegativeInfinity;

        foreach (Unit unit in nearbyUnits)
        {
            float dSqrToUnit = (transform.position - unit.transform.position).sqrMagnitude;
            if (
                dSqrToUnit > furthestDistanceSqr
                && !UnitUtils.IsWithinRange(health, unit.Health(), minAttackRange)
            )
            {
                furthestDistanceSqr = dSqrToUnit;
                furthestUnit = unit;
            }
        }

        return furthestUnit?.Health();
    }

    protected override void PerformAttack(Health target)
    {
        StartCoroutine(PerformDash(target, dashDuration));
    }

    private IEnumerator PerformDash(Health target, float duration)
    {
        Vector2 targetPosition = target.transform.position;
        Vector2 startPosition = rb.position;
        Vector2 dashDirection = (targetPosition - startPosition).normalized;
        float dashDistance = Vector2.Distance(startPosition, targetPosition) + extraDashLength;

        Collider2D areaCollider = GetAttackCollider(target);

        // Calculate required velocity to reach the end point
        Vector2 dashVelocity = dashDirection * (dashDistance / duration);

        // Clear the set of already hit targets for this dash
        alreadyHitTargets.Clear();

        // Temporarily ignore collisions with other units
        SetUnitCollisionsEnabled(false);

        float elapsedTime = 0f;
        while (elapsedTime < duration && !health.IsDead())
        {
            // Apply dash velocity
            rb.velocity = dashVelocity;

            // Check for collisions during dash
            CheckForDashCollisions(areaCollider);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // maintain a tiny bit of momentum
        rb.velocity *= 0.025f;

        // Re-enable unit collisions
        SetUnitCollisionsEnabled(true);

        // Clear the hit targets after dash is complete
        alreadyHitTargets.Clear();
    }

    private void CheckForDashCollisions(Collider2D areaCollider)
    {
        if (areaCollider == null)
            return;

        // Get all colliders overlapping with our area collider
        ContactFilter2D contactFilter = new();
        contactFilter.useTriggers = false; // We want to hit actual unit colliders, not other triggers

        List<Collider2D> overlappingColliders = new();
        areaCollider.OverlapCollider(contactFilter, overlappingColliders);

        foreach (Collider2D collider in overlappingColliders)
        {
            Health targetHealth = collider.GetComponent<Health>();

            if (
                targetHealth != null
                && !alreadyHitTargets.Contains(targetHealth)
                && UnitUtils.IsValidTarget(health, targetHealth)
            )
            {
                // Apply push force
                if (pushForce != 0f)
                {
                    Rigidbody2D targetRigidbody = targetHealth.GetComponent<Rigidbody2D>();
                    if (targetRigidbody != null)
                    {
                        Vector2 pushDirection = (
                            targetHealth.transform.position - transform.position
                        ).normalized;
                        targetRigidbody.AddForce(pushDirection * pushForce, ForceMode2D.Impulse);
                    }
                }

                targetHealth.Damage(AttackDamage());
                alreadyHitTargets.Add(targetHealth);
            }
        }
    }

    private void SetUnitCollisionsEnabled(bool enabled)
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider == null)
            return;

        // Get all unit colliders in the scene and ignore/restore collisions
        string[] unitTags = { "Enemy", "Friend", "Neutral" };

        foreach (string tag in unitTags)
        {
            GameObject[] units = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject unit in units)
            {
                if (unit == gameObject) // Skip self
                    continue;

                Collider2D unitCollider = unit.GetComponent<Collider2D>();
                if (unitCollider != null)
                {
                    Physics2D.IgnoreCollision(myCollider, unitCollider, !enabled);
                }
            }
        }
    }

    private Collider2D GetAttackCollider(Health target)
    {
        switch (UnitUtils.GetDirection(transform.position, target.transform.position))
        {
            case Direction.Down:
                return downAttackCollider;
            case Direction.Up:
                return upAttackCollider;
            default:
                return sideAttackCollider;
        }
    }
}
