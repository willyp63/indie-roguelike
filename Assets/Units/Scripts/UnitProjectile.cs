using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitProjectile : MonoBehaviour
{
    [SerializeField]
    private float hitRadius = 0.1f;

    [SerializeField]
    private bool isAOE = false;

    [SerializeField]
    private ParticleSystem effect;

    [SerializeField]
    private float effectDuration = 0f;

    private float fireAngle; // 0-90 degrees, controls parabola height
    private Vector2 firePosition;
    private Vector2 targetPosition;
    private UnitType type;
    private int damage;
    private float pushForce;
    private float speed;
    private bool isInFlight = false;

    // Parabolic path variables
    private float flightDuration;
    private float elapsedTime;
    private float maxParabolaHeight;
    private Vector2 perpendicularDirection;

    private static readonly float SHOT_LEADING_FACTOR = 0.8f;

    public void Initialize(
        UnitType unitType,
        Health targetHealth,
        int attackDamage,
        float attackPushForce,
        float projectileSpeed,
        float projectileFireAngle
    )
    {
        if (targetHealth == null)
        {
            Destroy(gameObject);
            return;
        }

        // Store initial positions and parameters
        type = unitType;
        firePosition = transform.position;
        var adjustedTargetVelocity =
            targetHealth.GetComponent<Rigidbody2D>().velocity * SHOT_LEADING_FACTOR;
        targetPosition =
            CalculateInterceptPoint(
                transform.position,
                targetHealth.transform.position,
                adjustedTargetVelocity,
                projectileSpeed
            ) ?? targetHealth.transform.position;
        damage = attackDamage;
        pushForce = attackPushForce;
        speed = projectileSpeed;
        fireAngle = projectileFireAngle;

        // Calculate flight parameters
        SetupParabolicPath();

        isInFlight = true;
    }

    private void SetupParabolicPath()
    {
        // Calculate distance and flight duration
        float distance = Vector2.Distance(firePosition, targetPosition);
        flightDuration = distance / speed;

        // Calculate fire-to-target vector and angle
        Vector2 fireToTarget = (targetPosition - firePosition);

        // Calculate scaling factor based on cosine of angle
        // When target is directly left/right: scale = 1.0 (full parabola)
        // When target is directly up/down: scale = 0.0 (straight line)
        float angleFromHorizontal = Mathf.Abs(Vector2.Angle(fireToTarget, Vector2.right));
        if (angleFromHorizontal > 90f)
            angleFromHorizontal = 180f - angleFromHorizontal;
        float heightScale = Mathf.Cos(angleFromHorizontal * Mathf.Deg2Rad);

        // Calculate maximum parabola height
        float baseHeight = distance * Mathf.Tan(fireAngle * Mathf.Deg2Rad) * 0.5f;
        maxParabolaHeight = baseHeight * heightScale;

        // Calculate perpendicular direction (always pointing downward/upward)
        // Rotate the fire-to-target vector by 90 degrees to get perpendicular
        Vector2 normalizedFireToTarget = fireToTarget.normalized;
        perpendicularDirection = new Vector2(-normalizedFireToTarget.y, normalizedFireToTarget.x);

        // Ensure parabola faces downward (positive y direction preference for downward arc)
        if (perpendicularDirection.y < 0)
        {
            perpendicularDirection = -perpendicularDirection;
        }

        // Reset elapsed time
        elapsedTime = 0f;
    }

    private void Update()
    {
        if (!isInFlight)
            return;

        // Update flight progress
        elapsedTime += Time.deltaTime;
        float t = elapsedTime / flightDuration;

        // Check if flight is complete
        if (t >= 1.0f)
        {
            isInFlight = false;

            // Move to exact target position
            transform.position = targetPosition;

            // Handle impact
            HandleImpact();

            // Destroy projectile after effect
            StartCoroutine(DestroyAfterEffect());
            return;
        }

        // Calculate current position along parabolic path
        Vector2 currentPosition = CalculateParabolicPosition(t);
        transform.position = currentPosition;

        // Rotate sprite to face movement direction
        Vector2 velocity = CalculateVelocityDirection(t);
        if (velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private IEnumerator DestroyAfterEffect()
    {
        if (effect != null)
        {
            var spriteRenderer =
                GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
            effect.Play();
            yield return new WaitForSeconds(effectDuration);
        }

        Destroy(gameObject);
    }

    private Vector2 CalculateParabolicPosition(float t)
    {
        // Linear interpolation between fire and target positions
        Vector2 basePosition = Vector2.Lerp(firePosition, targetPosition, t);

        // Calculate parabolic height offset using sine wave (0 at start/end, max at middle)
        float heightOffset = Mathf.Sin(t * Mathf.PI) * maxParabolaHeight;

        // Apply height offset in perpendicular direction
        return basePosition + (perpendicularDirection * heightOffset);
    }

    private Vector2 CalculateVelocityDirection(float t)
    {
        // Calculate velocity by taking derivative of position function
        // This gives us the direction the projectile is moving for proper rotation

        float dt = 0.01f; // Small time step for numerical derivative
        Vector2 currentPos = CalculateParabolicPosition(t);
        Vector2 nextPos = CalculateParabolicPosition(t + dt);

        return (nextPos - currentPos).normalized;
    }

    public static Vector2? CalculateInterceptPoint(
        Vector2 firingPosition,
        Vector2 targetPosition,
        Vector2 targetVelocity,
        float projectileSpeed
    )
    {
        Vector2 toTarget = targetPosition - firingPosition;

        // Quadratic equation coefficients for solving intercept time
        // We're solving: |targetPos + targetVel * t - firingPos| = projectileSpeed * t
        float a = Vector2.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
        float b = 2 * Vector2.Dot(toTarget, targetVelocity);
        float c = Vector2.Dot(toTarget, toTarget);

        float discriminant = b * b - 4 * a * c;

        // No real solution exists
        if (discriminant < 0)
            return null;

        // Calculate both possible times
        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float t1 = (-b + sqrtDiscriminant) / (2 * a);
        float t2 = (-b - sqrtDiscriminant) / (2 * a);

        // Choose the smallest positive time
        float interceptTime;
        if (t1 > 0 && t2 > 0)
            interceptTime = Mathf.Min(t1, t2);
        else if (t1 > 0)
            interceptTime = t1;
        else if (t2 > 0)
            interceptTime = t2;
        else
            return null; // No positive solution

        // Calculate the intercept point
        Vector2 interceptPoint = targetPosition + targetVelocity * interceptTime;
        return interceptPoint;
    }

    private void HandleImpact()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, hitRadius);

        List<Unit> allUnits = new();
        Unit closestUnit = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (Collider2D collider in colliders)
        {
            Unit unit = collider.GetComponent<Unit>();
            Health unitHealth = unit?.Health();
            if (
                unitHealth == null
                || unitHealth.IsDead()
                || !UnitUtils.IsValidTarget(type, unitHealth.Type())
            )
                continue;

            allUnits.Add(unit);

            float dSqrToUnit = (transform.position - unit.transform.position).sqrMagnitude;
            if (dSqrToUnit < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToUnit;
                closestUnit = unit;
            }
        }

        if (isAOE)
        {
            foreach (Unit unit in allUnits)
            {
                DamageAndApplyForce(unit.Health());
            }
        }
        else if (closestUnit != null)
        {
            DamageAndApplyForce(closestUnit.Health());
        }
    }

    private void DamageAndApplyForce(Health health)
    {
        // Apply push force
        if (pushForce != 0f)
        {
            Rigidbody2D targetRigidbody = health.GetComponent<Rigidbody2D>();
            if (targetRigidbody != null)
            {
                // make sure we are not exactly on top of target
                Vector3 randomOffsetPosition =
                    transform.position
                    + new Vector3(
                        Random.Range(-0.0001f, 0.0001f),
                        Random.Range(-0.0001f, 0.0001f),
                        0f
                    );

                Vector2 pushDirection = (
                    health.transform.position - randomOffsetPosition
                ).normalized;

                targetRigidbody.AddForce(pushDirection * pushForce, ForceMode2D.Impulse);
            }
        }

        health.Damage(damage);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
