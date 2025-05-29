using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [SerializeField]
    private float scale = 1f;

    [SerializeField]
    private int offsetY = 24;

    private readonly Color ENEMY_BACKGROUND_COLOR = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private readonly Color ENEMY_FOREGROUND_COLOR = new Color(0.8f, 0.2f, 0.2f, 0.9f);
    private readonly Color FRIEND_BACKGROUND_COLOR = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private readonly Color FRIEND_FOREGROUND_COLOR = new Color(0.2f, 0.8f, 0.2f, 0.9f);
    private readonly Color NEUTRAL_BACKGROUND_COLOR = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    private readonly Color NEUTRAL_FOREGROUND_COLOR = new Color(0.8f, 0.8f, 0.2f, 0.9f);

    private Health health;
    private Transform healthBarTransform;
    private SpriteRenderer backgroundRenderer;
    private SpriteRenderer foregroundRenderer;
    private Vector3 healthBarOffset;

    // Health bar dimensions in pixels
    private static readonly int HEALTH_BAR_WIDTH_PIXELS = 18;
    private static readonly int HEALTH_BAR_HEIGHT_PIXELS = 2;
    private static readonly int PIXELS_PER_UNIT = 32;

    // Calculated world dimensions
    private static readonly float HEALTH_BAR_WIDTH_WORLD =
        (float)HEALTH_BAR_WIDTH_PIXELS / PIXELS_PER_UNIT;

    // Static sprites shared across all health bars for performance
    private static Sprite backgroundSprite;
    private static Sprite[] foregroundSprites = new Sprite[HEALTH_BAR_WIDTH_PIXELS + 1];
    private static bool spritesCreated = false;

    void Start()
    {
        health = GetComponent<Health>();
        if (health == null)
        {
            Debug.LogError("HealthBar requires a Health component on the same GameObject.");
            return;
        }

        CreateHealthBar();

        // Subscribe to health events
        health.onDamageTaken.AddListener(OnHealthChanged);
        health.onHealed.AddListener(OnHealthChanged);
    }

    static void CreateSharedSprites()
    {
        if (spritesCreated)
            return; // Already created

        // Create background sprite (full width)
        backgroundSprite = CreatePixelPerfectSprite(
            HEALTH_BAR_WIDTH_PIXELS,
            HEALTH_BAR_HEIGHT_PIXELS
        );

        // Create foreground sprites for each possible width (0-16 pixels)
        for (int width = 0; width <= HEALTH_BAR_WIDTH_PIXELS; width++)
        {
            foregroundSprites[width] =
                width > 0 ? CreatePixelPerfectSprite(width, HEALTH_BAR_HEIGHT_PIXELS) : null;
        }

        spritesCreated = true;
    }

    static Sprite CreatePixelPerfectSprite(int widthPixels, int heightPixels)
    {
        Texture2D texture = new Texture2D(widthPixels, heightPixels, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point; // Critical for pixel perfect
        texture.name = $"HealthBar_{widthPixels}x{heightPixels}";

        // Fill with white pixels
        Color[] pixels = new Color[widthPixels * heightPixels];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // Create sprite with exact pixel dimensions
        return Sprite.Create(
            texture,
            new Rect(0, 0, widthPixels, heightPixels),
            new Vector2(0.5f, 0.5f), // Pivot at center
            PIXELS_PER_UNIT, // This matches your art's PPU
            0,
            SpriteMeshType.FullRect
        );
    }

    void CreateHealthBar()
    {
        // Ensure shared sprites are created (only happens once across all instances)
        CreateSharedSprites();

        healthBarOffset = new Vector3(
            0f,
            offsetY * health.ScaleFactor() / (float)PIXELS_PER_UNIT,
            0f
        );

        // Create health bar container
        GameObject healthBarObj = new GameObject("HealthBar");
        healthBarObj.transform.SetParent(transform);
        healthBarTransform = healthBarObj.transform;

        // Create background sprite
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(healthBarTransform);
        backgroundRenderer = backgroundObj.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = backgroundSprite;
        backgroundRenderer.sortingLayerName = "Units UI";
        backgroundRenderer.sortingOrder = 1;

        // Create foreground sprite
        GameObject foregroundObj = new GameObject("Foreground");
        foregroundObj.transform.SetParent(healthBarTransform);
        foregroundRenderer = foregroundObj.AddComponent<SpriteRenderer>();
        foregroundRenderer.sprite = foregroundSprites[HEALTH_BAR_WIDTH_PIXELS]; // Start with full health
        foregroundRenderer.sortingLayerName = "Units UI";
        foregroundRenderer.sortingOrder = 2;

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
        if (health.Type() == UnitType.Enemy)
        {
            backgroundRenderer.color = ENEMY_BACKGROUND_COLOR;
            foregroundRenderer.color = ENEMY_FOREGROUND_COLOR;
        }
        else if (health.Type() == UnitType.Friend)
        {
            backgroundRenderer.color = FRIEND_BACKGROUND_COLOR;
            foregroundRenderer.color = FRIEND_FOREGROUND_COLOR;
        }
        else // Neutral
        {
            backgroundRenderer.color = NEUTRAL_BACKGROUND_COLOR;
            foregroundRenderer.color = NEUTRAL_FOREGROUND_COLOR;
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
        if (health == null || foregroundRenderer == null)
            return;

        float healthPercentage = (float)health.CurrentHealth() / health.MaxHealth();
        healthPercentage = Mathf.Clamp01(healthPercentage);

        // Calculate the width in pixels for the foreground bar
        int foregroundWidthPixels = Mathf.RoundToInt(HEALTH_BAR_WIDTH_PIXELS * healthPercentage);

        // Ensure we have at least 1 pixel width if there's any health
        if (healthPercentage > 0 && foregroundWidthPixels == 0)
            foregroundWidthPixels = 1;

        // Use pre-created sprite
        foregroundRenderer.sprite = foregroundSprites[foregroundWidthPixels];

        // Position the foreground bar to be left-aligned with the background
        if (foregroundWidthPixels > 0)
        {
            float offsetX =
                -(HEALTH_BAR_WIDTH_WORLD - (float)foregroundWidthPixels / PIXELS_PER_UNIT) * 0.5f;
            foregroundRenderer.transform.localPosition = new Vector3(offsetX, 0, 0);
        }

        // Hide health bar if dead
        healthBarTransform.gameObject.SetActive(!health.IsDead());
    }

    void OnDestroy()
    {
        if (healthBarTransform != null)
            Destroy(healthBarTransform.gameObject);
    }
}
