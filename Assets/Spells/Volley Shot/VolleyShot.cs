using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolleyShot : Spell
{
    [SerializeField]
    private int damage = 50;

    [SerializeField]
    private float damageDelay = 0.5f;

    [SerializeField]
    private ParticleSystem arrowEffect;

    [SerializeField]
    private float effectDuration = 2f;

    public override void Cast(Vector2 targetPosition, UnitType unitType)
    {
        transform.position = targetPosition;

        if (arrowEffect != null)
            arrowEffect.Play();

        StartCoroutine(DamageAfterDelay(targetPosition, unitType));
        StartCoroutine(DestroyAfterEffect());
    }

    private IEnumerator DamageAfterDelay(Vector2 targetPosition, UnitType unitType)
    {
        yield return new WaitForSeconds(damageDelay);

        List<Unit> hitUnits = UnitManager.Instance.GetNearbyUnits(
            targetPosition,
            EffectRadius(),
            new List<UnitType> { UnitUtils.GetOppositeUnitType(unitType) }
        );

        foreach (Unit unit in hitUnits)
        {
            unit.Health()?.Damage(damage);
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
