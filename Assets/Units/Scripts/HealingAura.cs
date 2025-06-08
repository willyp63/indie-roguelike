using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingAura : MonoBehaviour
{
    [SerializeField]
    private float healRadius = 2f;

    [SerializeField]
    private int healAmount = 4;

    [SerializeField]
    private float healInterval = 1f;

    [SerializeField]
    private float healDelay = 0.25f;

    [SerializeField]
    private ParticleSystem healEffect;

    private Health health;
    private Coroutine healingCoroutine;

    void Start()
    {
        health = GetComponent<Health>();
        if (health == null)
        {
            Debug.LogError($"HealingAura on {gameObject.name} requires a Health component!");
            return;
        }

        // Start the healing coroutine
        healingCoroutine = StartCoroutine(HealingLoop());
    }

    void OnDestroy()
    {
        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine);
        }
    }

    private IEnumerator HealingLoop()
    {
        while (true)
        {
            healEffect.Play();

            yield return new WaitForSeconds(healDelay);

            HealNearbyAllies();

            yield return new WaitForSeconds(healInterval - healDelay);

            // Don't heal if this unit is dead
            if (health.IsDead())
                continue;
        }
    }

    private void HealNearbyAllies()
    {
        // Get nearby allied units (same type as this unit)
        List<Unit> nearbyAllies = UnitManager.Instance.GetNearbyUnits(
            transform.position,
            healRadius,
            new List<UnitType> { health.Type() }
        );

        foreach (Unit ally in nearbyAllies)
        {
            // Don't heal self and don't heal units at full health
            if (ally.gameObject == gameObject || ally.Health().IsFullHealth())
                continue;

            // Heal the ally
            ally.Health().Heal(healAmount);
        }
    }

    // Optional: Draw the heal radius in the scene view for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}
