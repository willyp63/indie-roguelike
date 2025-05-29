using System.Collections;
using UnityEngine;

public class BasicAttack : AttackBehaviour
{
    protected override void PerformAttack(Health target)
    {
        // make sure target is still in range (target was choosen before attack delay)
        if (!UnitUtils.IsWithinRange(health, target, AttackRange()))
            return;

        // Apply push force
        if (pushForce != 0f)
        {
            Rigidbody2D targetRigidbody = target.GetComponent<Rigidbody2D>();
            if (targetRigidbody != null)
            {
                Vector2 pushDirection = (target.transform.position - transform.position).normalized;
                targetRigidbody.AddForce(pushDirection * pushForce, ForceMode2D.Impulse);
            }
        }

        target.Damage(AttackDamage());
    }
}
