using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassHeal : Spell
{
    [SerializeField]
    private int healing = 50;

    [SerializeField]
    private float healingDelay = 0.5f;

    [SerializeField]
    private ParticleSystem healingEffect;

    [SerializeField]
    private float effectDuration = 2f;

    public override void Cast(Vector2 targetPosition, UnitType unitType)
    {
        transform.position = targetPosition;

        if (healingEffect != null)
            healingEffect.Play();

        StartCoroutine(HealAfterDelay(targetPosition, unitType));
        StartCoroutine(DestroyAfterEffect());
    }

    private IEnumerator HealAfterDelay(Vector2 targetPosition, UnitType unitType)
    {
        yield return new WaitForSeconds(healingDelay);

        List<Unit> targetUnits = UnitManager.Instance.GetNearbyUnits(
            targetPosition,
            EffectRadius(),
            new List<UnitType> { unitType }
        );

        foreach (Unit unit in targetUnits)
        {
            unit.Health()?.Heal(healing);
        }
    }

    private IEnumerator DestroyAfterEffect()
    {
        yield return new WaitForSeconds(effectDuration);

        Destroy(gameObject);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, EffectRadius());
    }
}
