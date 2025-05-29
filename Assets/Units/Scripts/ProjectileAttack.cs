using System.Collections;
using UnityEngine;

public class ProjectileAttack : AttackBehaviour
{
    [SerializeField]
    private UnitProjectile projectilePrefab;

    [SerializeField]
    private Transform downFireLocation;

    [SerializeField]
    private Transform sideFireLocation;

    [SerializeField]
    private Transform upFireLocation;

    [SerializeField]
    private float projectileFireAngle = 30f; // 0-90 degrees, controls parabola height

    [SerializeField]
    private float projectileSpeed = 5f;

    protected override void PerformAttack(Health target)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("ProjectilePrefab is not assigned!");
            return;
        }

        // Spawn projectile at attacker's position
        GameObject projectile = Instantiate(
            projectilePrefab.gameObject,
            GetFireLocation(target).position,
            Quaternion.identity
        );

        // Get or add ProjectileBehaviour component
        UnitProjectile projectileBehaviour = projectile.GetComponent<UnitProjectile>();
        if (projectileBehaviour == null)
        {
            projectileBehaviour = projectile.AddComponent<UnitProjectile>();
        }

        // Initialize the projectile
        projectile.transform.localScale = new Vector3(
            health.ScaleFactor(),
            health.ScaleFactor(),
            1f
        );
        projectileBehaviour.Initialize(
            health.Type(),
            target,
            AttackDamage(),
            pushForce,
            projectileSpeed,
            projectileFireAngle
        );
    }

    private Transform GetFireLocation(Health target)
    {
        switch (UnitUtils.GetDirection(transform.position, target.transform.position))
        {
            case Direction.Down:
                return downFireLocation;
            case Direction.Up:
                return upFireLocation;
            default:
                return sideFireLocation;
        }
    }
}
