using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VacuumAttack : AttackBehaviour
{
    [SerializeField]
    private int minNumberOfTarget = 3;

    [SerializeField]
    private float pullForce = 50f;

    [SerializeField]
    private float rotationSpeed = 90f; // degrees per second

    [SerializeField]
    private ParticleSystem effect;

    public override Health GetTarget(Unit targetUnit)
    {
        if (targetUnit == null)
            return null;

        var nearbyUnits = UnitManager.Instance.GetNearbyUnits(
            health,
            attackRange + health.HitBoxRadius(),
            new List<UnitType> { health.OppositeType() }
        );

        return nearbyUnits.Count >= minNumberOfTarget ? health : null;
    }

    protected override void PerformAttack(Health _)
    {
        StartCoroutine(PerformVacuum());
    }

    private IEnumerator PerformVacuum()
    {
        effect.transform.localScale = Vector2.one * attackRange / 6f;
        effect.Play();

        float elapsedTime = 0f;
        while (elapsedTime < attackDuration - 0.5f && !health.IsDead())
        {
            List<Unit> allTargets = UnitManager.Instance.GetNearbyUnits(
                health,
                attackRange,
                new List<UnitType> { UnitType.Friend, UnitType.Neutral, UnitType.Enemy }
            );

            foreach (Unit target in allTargets)
            {
                Rigidbody2D rb = target.GetComponent<Rigidbody2D>();

                if (rb == null)
                    continue;

                rb.AddForce(
                    (transform.position - target.transform.position).normalized * pullForce
                );
            }

            // Rotate effect at constant speed
            effect.transform.Rotate(0, 0, rotationSpeed * Time.fixedDeltaTime);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        effect.Stop();

        // HACK: keep the thing spinning while the particles are stopping
        elapsedTime = 0f;
        while (elapsedTime < 0.5f)
        {
            // Rotate effect at constant speed
            effect.transform.Rotate(0, 0, rotationSpeed * Time.fixedDeltaTime);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}
