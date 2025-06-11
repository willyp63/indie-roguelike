using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalTower : MonoBehaviour
{
    [SerializeField]
    private GameObject breakEffectPrefab;

    [SerializeField]
    private ParticleSystem damageBreakEffect;

    [SerializeField]
    private Transform crystalTransform;

    [SerializeField]
    private float delay = 0.25f;

    [SerializeField]
    private float travelTime = 0.75f; // Time in seconds to reach the player

    [SerializeField]
    private float breakTime = 1f; // Amount of time the crystal is immune after breaking

    [SerializeField]
    private float accelerationCurve = 2f; // Higher values = more acceleration towards the end (1 = linear, 2 = quadratic, etc.)

    [SerializeField]
    private int breakDamage = 1;

    [SerializeField]
    private float breakDamageRadius = 2f;

    [SerializeField]
    private float breakDamageForce = 50f;

    private Health healthComponent;
    private HealthBar healthBar;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        healthComponent = GetComponent<Health>();
        animator = GetComponent<Animator>();
        healthBar = GetComponent<HealthBar>();

        if (healthComponent == null)
        {
            Debug.LogError("CrystalTower requires a Health component!");
            return;
        }

        // Subscribe to the damage taken event
        healthComponent.onDamageTaken.AddListener(OnDamageTaken);
    }

    private void OnDamageTaken(int damage)
    {
        // Check if this damage would be lethal
        if (healthComponent.CurrentHealth() <= 0 && !healthComponent.IsBrokenCrystal())
        {
            // Heal back to full health, set immune, and trigger break animation
            healthComponent.SetIsBrokenCrystal(true);
            animator.SetTrigger("Break");
            healthBar.IsVisible = false;
            StartCoroutine(EnableHealthAfterBreak());

            DamageNearbyUnits();
            if (damageBreakEffect != null)
                damageBreakEffect.Play();

            // Create and play particle effect
            if (breakEffectPrefab != null)
            {
                StartCoroutine(PlayAbsorptionEffect());
            }

            ShardManager.Instance.AddShards(1);
            Debug.Log("Crystal Tower regenerated! TODO: Give player crystal resource");
        }
    }

    private void DamageNearbyUnits()
    {
        List<Unit> nearbyUnits = UnitManager.Instance.GetNearbyUnits(
            transform.position,
            breakDamageRadius,
            new List<UnitType> { UnitType.Enemy, UnitType.Neutral, UnitType.Friend }
        );

        foreach (Unit unit in nearbyUnits)
        {
            if (unit.gameObject == gameObject)
                continue;

            unit.Health().Damage(breakDamage);

            Rigidbody2D rigidbody = unit.GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.AddForce(
                    (unit.transform.position - transform.position).normalized * breakDamageForce,
                    ForceMode2D.Impulse
                );
            }
        }
    }

    private IEnumerator EnableHealthAfterBreak()
    {
        yield return new WaitForSeconds(breakTime);

        healthComponent.SetIsBrokenCrystal(false);
        healthComponent.Heal(healthComponent.MaxHealth());
        healthBar.IsVisible = true;
    }

    private IEnumerator PlayAbsorptionEffect()
    {
        // Instantiate the particle effect at the crystal's position
        GameObject effectInstance = Instantiate(
            breakEffectPrefab,
            crystalTransform.position,
            Quaternion.identity
        );
        ParticleSystem particleSystem = effectInstance.GetComponent<ParticleSystem>();

        if (particleSystem == null)
        {
            Debug.LogError("Break effect prefab must have a ParticleSystem component!");
            Destroy(effectInstance);
            yield break;
        }

        // Play the particle effect
        particleSystem.Play();

        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("No player found with tag 'Player'");
            // Still wait for the particle system to finish and then destroy
            yield return new WaitUntil(() => !particleSystem.IsAlive());
            Destroy(effectInstance);
            yield break;
        }

        yield return new WaitForSeconds(delay);

        // Move the effect towards the player over time
        Vector3 startPosition = effectInstance.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < travelTime && effectInstance != null)
        {
            elapsedTime += Time.deltaTime;

            // Calculate normalized time (0 to 1)
            float normalizedTime = Mathf.Clamp01(elapsedTime / travelTime);

            // Apply acceleration curve (easing function)
            float easedTime = Mathf.Pow(normalizedTime, accelerationCurve);

            // Get current player position (in case player is moving)
            Vector3 currentPlayerPosition = player.transform.position;

            // Interpolate position with acceleration curve
            effectInstance.transform.position = Vector3.Lerp(
                startPosition,
                currentPlayerPosition,
                easedTime
            );

            yield return null;
        }

        // Effect has reached the player or close enough, destroy it
        if (effectInstance != null)
        {
            Destroy(effectInstance);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (healthComponent != null)
        {
            healthComponent.onDamageTaken.RemoveListener(OnDamageTaken);
        }
    }
}
