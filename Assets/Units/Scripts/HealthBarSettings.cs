using UnityEngine;

[CreateAssetMenu(fileName = "HealthBarSettings", menuName = "Game/Health Bar Settings")]
public class HealthBarSettings : ScriptableObject
{
    [Header("Health Bar Prefab")]
    [SerializeField]
    private HealthBarUI healthBarPrefab;

    public HealthBarUI HealthBarPrefab => healthBarPrefab;

    // Validation
    public bool IsValid()
    {
        return healthBarPrefab != null;
    }
}
