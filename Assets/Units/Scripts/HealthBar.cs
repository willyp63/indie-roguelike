using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [SerializeField]
    private float scale = 1f;

    [SerializeField]
    private int offsetY = 24;

    [SerializeField]
    private bool alwaysShow = false;

    // Static reference to shared settings - loaded once and shared across all instances
    private static HealthBarSettings sharedSettings;

    private readonly Color ENEMY_BACKGROUND_COLOR = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private readonly Color ENEMY_FOREGROUND_COLOR = new Color(0.8f, 0.2f, 0.2f, 0.9f);
    private readonly Color FRIEND_BACKGROUND_COLOR = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private readonly Color FRIEND_FOREGROUND_COLOR = new Color(0.2f, 0.8f, 0.2f, 0.9f);
    private readonly Color NEUTRAL_BACKGROUND_COLOR = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    private readonly Color NEUTRAL_FOREGROUND_COLOR = new Color(0.8f, 0.8f, 0.2f, 0.9f);

    private Health health;
    private Transform healthBarTransform;
    private SpriteRenderer frameRenderer;
    private SpriteRenderer unfilledRenderer;
    private SpriteRenderer filledRenderer;
    private SpriteRenderer dotFrameRenderer;
    private SpriteRenderer dotFillRenderer;
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

    void CreateHealthBar()
    {
        healthBarOffset = new Vector3(
            0f,
            offsetY * health.ScaleFactor() / (float)PIXELS_PER_UNIT,
            0f
        );

        // Create health bar container
        GameObject healthBarObj = new GameObject("HealthBar");
        healthBarObj.transform.SetParent(transform);
        healthBarTransform = healthBarObj.transform;

        // Create frame sprite (outermost layer)
        GameObject frameObj = new GameObject("Frame");
        frameObj.transform.SetParent(healthBarTransform);
        frameRenderer = frameObj.AddComponent<SpriteRenderer>();
        frameRenderer.sprite = sharedSettings.FrameSprite;
        frameRenderer.sortingLayerName = "Units UI";
        frameRenderer.sortingOrder = 3; // Highest layer

        // Create unfilled background sprite
        GameObject unfilledObj = new GameObject("Unfilled");
        unfilledObj.transform.SetParent(healthBarTransform);
        unfilledRenderer = unfilledObj.AddComponent<SpriteRenderer>();
        unfilledRenderer.sprite = sharedSettings.UnfilledBarSprite;
        unfilledRenderer.sortingLayerName = "Units UI";
        unfilledRenderer.sortingOrder = 1; // Bottom layer

        // Create filled foreground sprite
        GameObject filledObj = new GameObject("Filled");
        filledObj.transform.SetParent(healthBarTransform);
        filledRenderer = filledObj.AddComponent<SpriteRenderer>();
        filledRenderer.sprite = sharedSettings.FilledBarSprite;
        filledRenderer.sortingLayerName = "Units UI";
        filledRenderer.sortingOrder = 2; // Middle layer

        // Create dot frame sprite for team indicator
        GameObject dotFrameObj = new GameObject("DotFrame");
        dotFrameObj.transform.SetParent(healthBarTransform);
        dotFrameRenderer = dotFrameObj.AddComponent<SpriteRenderer>();
        dotFrameRenderer.sprite = sharedSettings.DotFrameSprite;
        dotFrameRenderer.sortingLayerName = "Units UI";
        dotFrameRenderer.sortingOrder = 5; // Higher than health bar

        // Create dot fill sprite for team indicator
        GameObject dotFillObj = new GameObject("DotFill");
        dotFillObj.transform.SetParent(healthBarTransform);
        dotFillRenderer = dotFillObj.AddComponent<SpriteRenderer>();
        dotFillRenderer.sprite = sharedSettings.DotFillSprite;
        dotFillRenderer.sortingLayerName = "Units UI";
        dotFillRenderer.sortingOrder = 4; // Between dot frame and health bar

        // Set colors based on unit type
        SetHealthBarColors();

        // Position the health bar with pixel perfect alignment
        UpdatePixelPerfectPosition();

        // Update initial health display
        UpdateHealthBar();

        healthBarObj.transform.localScale = new Vector3(scale, scale, 1f);
    }

    void SetHealthBarColors()
    {
        // Frame stays neutral/white to preserve the pixel art
        frameRenderer.color = Color.white;
        dotFrameRenderer.color = Color.white;

        if (health.Type() == UnitType.Enemy)
        {
            unfilledRenderer.color = ENEMY_BACKGROUND_COLOR;
            filledRenderer.color = ENEMY_FOREGROUND_COLOR;
            dotFillRenderer.color = ENEMY_FOREGROUND_COLOR;
        }
        else if (health.Type() == UnitType.Friend)
        {
            unfilledRenderer.color = FRIEND_BACKGROUND_COLOR;
            filledRenderer.color = FRIEND_FOREGROUND_COLOR;
            dotFillRenderer.color = FRIEND_FOREGROUND_COLOR;
        }
        else // Neutral
        {
            unfilledRenderer.color = NEUTRAL_BACKGROUND_COLOR;
            filledRenderer.color = NEUTRAL_FOREGROUND_COLOR;
            dotFillRenderer.color = NEUTRAL_FOREGROUND_COLOR;
        }
    }

    void UpdatePixelPerfectPosition()
    {
        if (healthBarTransform == null)
            return;

        // Calculate desired world position
        Vector3 desiredPosition = transform.position + healthBarOffset;

        // Snap to pixel grid
        Vector3 snappedPosition = SnapToPixelGrid(desiredPosition);
        healthBarTransform.position = snappedPosition;
    }

    Vector3 SnapToPixelGrid(Vector3 worldPosition)
    {
        // Convert world position to pixel coordinates
        float pixelX = worldPosition.x * PIXELS_PER_UNIT;
        float pixelY = worldPosition.y * PIXELS_PER_UNIT;

        // Round to nearest pixel
        pixelX = Mathf.Round(pixelX);
        pixelY = Mathf.Round(pixelY);

        // Convert back to world coordinates
        return new Vector3(pixelX / PIXELS_PER_UNIT, pixelY / PIXELS_PER_UNIT, worldPosition.z);
    }

    void OnHealthChanged(int amount)
    {
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (health == null || filledRenderer == null)
            return;

        float healthPercentage = (float)health.CurrentHealth() / health.MaxHealth();
        healthPercentage = Mathf.Clamp01(healthPercentage);

        bool isAtFullHealth = health.CurrentHealth() >= health.MaxHealth();
        bool isAlive = !health.IsDead();

        if (!alwaysShow && isAtFullHealth && isAlive)
        {
            // Show team indicator dot, hide health bar components
            frameRenderer.gameObject.SetActive(false);
            unfilledRenderer.gameObject.SetActive(false);
            filledRenderer.gameObject.SetActive(false);
            dotFrameRenderer.gameObject.SetActive(true);
            dotFillRenderer.gameObject.SetActive(true);

            // Show the health bar container
            healthBarTransform.gameObject.SetActive(true);
        }
        else if (isAlive)
        {
            // Show health bar, hide team indicator dot
            frameRenderer.gameObject.SetActive(true);
            unfilledRenderer.gameObject.SetActive(true);
            filledRenderer.gameObject.SetActive(true);
            dotFrameRenderer.gameObject.SetActive(false);
            dotFillRenderer.gameObject.SetActive(false);

            // Scale the filled bar horizontally based on health percentage
            // This assumes your sprite is designed to be scaled
            filledRenderer.transform.localScale = new Vector3(healthPercentage, 1f, 1f);

            // Position the filled bar to align with the left side of the frame
            // You may need to adjust this offset based on your sprite's pivot and dimensions
            float offsetX = -(1f - healthPercentage) * filledRenderer.sprite.bounds.size.x * 0.5f;
            filledRenderer.transform.localPosition = new Vector3(offsetX, 0, 0);

            // Show the health bar container
            healthBarTransform.gameObject.SetActive(true);
        }
        else
        {
            // Unit is dead, hide everything
            healthBarTransform.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (healthBarTransform != null)
            Destroy(healthBarTransform.gameObject);
    }
}
