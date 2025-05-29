using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaAttack : AttackBehaviour
{
    [SerializeField]
    private Collider2D downAttackCollider;

    [SerializeField]
    private Collider2D sideAttackCollider;

    [SerializeField]
    private Collider2D upAttackCollider;

    [SerializeField]
    private ParticleSystem effect;

    protected override void PerformAttack(Health target)
    {
        Collider2D areaCollider = GetAttackCollider(target);
        if (areaCollider == null)
            return;

        Vector2 attackPosition = (Vector2)transform.position + areaCollider.offset;

        // Get all colliders overlapping with our area collider
        ContactFilter2D contactFilter = new();
        contactFilter.useTriggers = false; // We want to hit actual unit colliders, not other triggers

        List<Collider2D> overlappingColliders = new();
        areaCollider.OverlapCollider(contactFilter, overlappingColliders);

        HashSet<Health> targetsHit = new(); // Prevent hitting the same unit multiple times

        foreach (Collider2D collider in overlappingColliders)
        {
            Health targetHealth = collider.GetComponent<Health>();

            if (
                targetHealth != null
                && !targetsHit.Contains(targetHealth)
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
                            (Vector2)targetHealth.transform.position - attackPosition
                        ).normalized;
                        targetRigidbody.AddForce(pushDirection * pushForce, ForceMode2D.Impulse);
                    }
                }

                targetHealth.Damage(AttackDamage());
                targetsHit.Add(targetHealth);
            }
        }

        if (effect != null)
        {
            effect.transform.localPosition = new Vector3(
                areaCollider.offset.x,
                areaCollider.offset.y,
                0f
            );
            effect.Play();
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
