using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [SerializeField]
    private float scale = 1f;

    [SerializeField]
    private int offsetY = 24;

    private HealthBarSettings sharedSettings;

    private readonly Color ENEMY_HEALTH_COLOR = new Color(1f, 0.5f, 0f, 0.85f);
    private readonly Color FRIEND_HEALTH_COLOR = new Color(0f, 1f, 0.5f, 0.85f);
    private readonly Color FRIEND_KARMA_COLOR = new Color(0f, 0.7f, 0.7f, 0.85f);

    private Health health;
    private HealthBarUI healthBarUI;
    private Vector3 healthBarOffset;
    private static readonly int PIXELS_PER_UNIT = 32;

    void Start()
    {
        health = GetComponent<Health>();
        if (health == null)
        {
            Debug.LogError("HealthBar requires a Health component on the same GameObject.");
            return;
        }

        // Load shared settings if not already loaded
        if (sharedSettings == null)
        {
            sharedSettings = Resources.Load<HealthBarSettings>("HealthBarSettings");
        }

        // Validate settings
        if (sharedSettings == null || !sharedSettings.IsValid())
        {
            Debug.LogError(
                "HealthBar requires a valid HealthBarSettings asset in Resources folder. Create one using 'Assets > Create > Game > Health Bar Settings'"
            );
            return;
        }

        CreateHealthBar();

        // Subscribe to health events
        health.onDamageTaken.AddListener(OnHealthChanged);
        health.onHealed.AddListener(OnHealthChanged);
    }

    void Update()
    {
        UpdateHealthBarPosition();
    }

    void CreateHealthBar()
    {
        healthBarOffset = new Vector3(
            0f,
            offsetY * health.ScaleFactor() / (float)PIXELS_PER_UNIT,
            0f
        );

        // Find the health bar canvas
        // TODO: Make this a singleton
        Canvas healthBarCanvas = GameObject.Find("HealthBarCanvas")?.GetComponent<Canvas>();
        if (healthBarCanvas == null)
        {
            Debug.LogError(
                "HealthBarCanvas not found in scene. Please create a canvas named 'HealthBarCanvas' for health bars."
            );
            return;
        }

        // Instantiate the health bar UI
        healthBarUI = Instantiate(sharedSettings.HealthBarPrefab, healthBarCanvas.transform);
        healthBarUI.transform.localScale = new Vector3(scale, scale, 1f);

        int karmaValue = health.GetComponent<Unit>()?.KarmaValue() ?? 0;

        // Set the color of the health bar
        healthBarUI.SetKarmaBackgroundColor(
            health.IsEnemy() ? ENEMY_HEALTH_COLOR
            : karmaValue > 0 ? FRIEND_KARMA_COLOR
            : FRIEND_HEALTH_COLOR
        );
        healthBarUI.SetHealthBarFillColor(
            health.IsEnemy() ? ENEMY_HEALTH_COLOR : FRIEND_HEALTH_COLOR
        );

        // Update initial display
        UpdateKarma(karmaValue);
        UpdateHealthBar();
        UpdateHealthBarPosition();
    }

    void UpdateKarma(int karmaValue)
    {
        if (healthBarUI == null)
            return;

        healthBarUI.SetKarma(karmaValue);
    }

    void UpdateHealthBar()
    {
        if (health == null || healthBarUI == null)
            return;

        healthBarUI.SetHealthPercent((float)health.CurrentHealth() / health.MaxHealth());
    }

    void UpdateHealthBarPosition()
    {
        if (healthBarUI == null)
            return;

        // Calculate desired world position
        Vector3 desiredPosition = transform.position + healthBarOffset;

        // Convert world position to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(desiredPosition);

        // Set the position in screen space
        healthBarUI.transform.position = screenPos;
    }

    void OnHealthChanged(int amount)
    {
        UpdateHealthBar();
    }

    void OnDestroy()
    {
        if (healthBarUI != null)
            Destroy(healthBarUI.gameObject);
    }
}
