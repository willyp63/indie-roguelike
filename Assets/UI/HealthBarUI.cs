using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField]
    private Transform healthBar;

    [SerializeField]
    private Image healthBarFill;

    [Header("Karma Display")]
    [SerializeField]
    private Transform karmaDisplay;

    [SerializeField]
    private Image karmaFrame;

    [SerializeField]
    private Image karmaBackground;

    [SerializeField]
    private TMPro.TextMeshProUGUI[] karmaTexts;

    [Header("Karma Mini Sprites")]
    [SerializeField]
    private Sprite karmaBackgroundMini;

    [SerializeField]
    private Sprite karmaFrameMini;

    [SerializeField]
    private Sprite karmaBackgroundNormal;

    [SerializeField]
    private Sprite karmaFrameNormal;

    [Header("Karma Sprite Sizes")]
    [SerializeField]
    private Vector2 normalSpriteSize = new Vector2(17, 17);

    [SerializeField]
    private Vector2 miniSpriteSize = new Vector2(9, 9);

    private Color karmaColor;

    private bool alwaysShowHealthBar = false;

    private Vector3 originalKarmaPosition;
    private Vector3 originalHealthBarPosition;

    private int karmaValue = -1;
    private float healthPercent = 1f;

    private void Awake()
    {
        karmaColor = karmaBackground.color;
        originalKarmaPosition = karmaDisplay.transform.localPosition;
        originalHealthBarPosition = healthBar.transform.localPosition;
    }

    public void SetKarma(int newKarmaValue)
    {
        // Clamp karma value between 0 and 9
        karmaValue = Mathf.Clamp(newKarmaValue, 0, 9);
        bool hasKarma = karmaValue > 0;

        // Update all karma text displays
        if (karmaTexts != null)
        {
            foreach (var text in karmaTexts)
            {
                if (text != null)
                {
                    text.text = karmaValue.ToString();
                    text.gameObject.SetActive(hasKarma);
                }
            }
        }

        // Swap sprites and resize based on karma value
        Vector2 targetSize = hasKarma ? normalSpriteSize : miniSpriteSize;

        karmaBackground.sprite = hasKarma ? karmaBackgroundNormal : karmaBackgroundMini;
        karmaBackground.rectTransform.sizeDelta = targetSize;

        karmaFrame.sprite = hasKarma ? karmaFrameNormal : karmaFrameMini;
        karmaFrame.rectTransform.sizeDelta = targetSize;

        if (karmaBackground != null)
        {
            float darknessFactor = karmaValue == 0 ? 1f : 1f - ((9f - karmaValue) / 9f) * 0.4f;
            karmaBackground.color = new Color(
                karmaColor.r * darknessFactor,
                karmaColor.g * darknessFactor,
                karmaColor.b * darknessFactor,
                karmaColor.a
            );
        }

        UpdateVisibility();
    }

    public void SetHealthPercent(float newHealthPercent)
    {
        // Clamp percent between 0 and 1
        healthPercent = Mathf.Clamp01(newHealthPercent);

        // Scale the health bar fill horizontally based on the percentage
        Vector3 scale = healthBarFill.transform.localScale;
        scale.x = healthPercent;
        healthBarFill.transform.localScale = scale;

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool isDead = healthPercent <= 0f;
        bool isHealthBarVisible = !isDead && (healthPercent < 1f || alwaysShowHealthBar);
        bool isKarmaVisible = !isDead && (karmaValue > 0 || !isHealthBarVisible);

        healthBar.gameObject.SetActive(isHealthBarVisible);
        karmaDisplay.gameObject.SetActive(isKarmaVisible);

        if (isKarmaVisible && isHealthBarVisible)
        {
            // keep both in original positions
            healthBar.transform.localPosition = originalHealthBarPosition;
            karmaDisplay.transform.localPosition = originalKarmaPosition;
        }
        else
        {
            // center both (only 1 is visible)
            karmaDisplay.transform.localPosition = new Vector3(0f, 0f, 0f);
            healthBar.transform.localPosition = new Vector3(0f, 0f, 0f);
        }
    }

    public void SetKarmaBackgroundColor(Color color)
    {
        if (karmaBackground != null)
        {
            karmaColor = color;
            karmaBackground.color = color;
        }
    }

    public void SetHealthBarFillColor(Color color)
    {
        if (healthBarFill != null)
        {
            healthBarFill.color = color;
        }
    }

    public void SetAlwaysShowHealthBar(bool value)
    {
        alwaysShowHealthBar = value;
        UpdateVisibility();
    }
}
