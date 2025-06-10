using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalTower : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem breakEffect;

    private Health healthComponent;

    // Start is called before the first frame update
    void Start()
    {
        // Get the Health component on this GameObject
        healthComponent = GetComponent<Health>();

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
        if (healthComponent.CurrentHealth() <= 0)
        {
            // Heal back to full health
            healthComponent.Heal(healthComponent.MaxHealth());

            // Play particle effect
            if (breakEffect != null)
            {
                breakEffect.Play();
            }

            ShardManager.Instance.AddShards(1);
            Debug.Log("Crystal Tower regenerated! TODO: Give player crystal resource");
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
