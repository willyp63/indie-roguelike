using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardInputHandler : Singleton<CardInputHandler>
{
    [Header("Keybinds")]
    [SerializeField]
    private KeyCode[] cardKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };

    [SerializeField]
    private Bounds playableUnitBounds;

    [SerializeField]
    private Bounds playableSpellBounds;

    private int selectedCardIndex = -1;

    private Camera playerCamera;

    private static readonly Color INVALID_COLOR = new Color(1f, 0.5f, 0.5f, 0.5f);
    private static readonly Color VALID_COLOR = new Color(1f, 1f, 1f, 0.5f);

    // Preview unit system
    private List<GameObject> previewUnits = new List<GameObject>();
    private Material previewMaterial;

    // Spell preview system
    private GameObject spellPreviewObject;
    private SpriteRenderer spellPreviewSprite;
    private Sprite spellPreviewCircle1x;
    private Sprite spellPreviewCircle2x;
    private Sprite spellPreviewCircle3x;
    private Texture2D circleTexture1x;
    private Texture2D circleTexture2x;
    private Texture2D circleTexture3x;

    private const float PIXELS_PER_UNIT = 32f;

    private void Start()
    {
        playerCamera = Camera.main;

        CardsUI.Instance.onCardButtonClicked.AddListener(OnCardButtonClicked);

        // Create preview material and spell preview circle
        CreatePreviewMaterial();
        CreateSpellPreviewCircle();
    }

    private void CreatePreviewMaterial()
    {
        // Create a material for preview units (white and transparent)
        previewMaterial = new Material(Shader.Find("Sprites/Default"));
        previewMaterial.color = new Color(1f, 1f, 1f, 0.5f); // White and semi-transparent
    }

    private void CreateSpellPreviewCircle()
    {
        // Create the circle textures
        circleTexture1x = CreateCircleTexture(256, 12);
        circleTexture2x = CreateCircleTexture(256, 8);
        circleTexture3x = CreateCircleTexture(256, 4);

        // Create sprites from textures
        spellPreviewCircle1x = CreateCircleSprite(circleTexture1x);
        spellPreviewCircle2x = CreateCircleSprite(circleTexture2x);
        spellPreviewCircle3x = CreateCircleSprite(circleTexture3x);

        // Create GameObject for spell preview
        spellPreviewObject = new GameObject("SpellPreview");
        spellPreviewSprite = spellPreviewObject.AddComponent<SpriteRenderer>();
        spellPreviewSprite.sortingLayerName = "Background";
        spellPreviewSprite.sortingOrder = 999;
        UpdateSpellPreviewCircle(0);

        // Start with preview disabled
        spellPreviewObject.SetActive(false);
    }

    private void UpdateSpellPreviewCircle(float spellRadius)
    {
        if (spellRadius < 1f)
        {
            spellPreviewSprite.sprite = spellPreviewCircle1x;
        }
        else if (spellRadius < 2f)
        {
            spellPreviewSprite.sprite = spellPreviewCircle2x;
        }
        else
        {
            spellPreviewSprite.sprite = spellPreviewCircle3x;
        }
    }

    private Sprite CreateCircleSprite(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            Vector2.one * 0.5f,
            PIXELS_PER_UNIT
        );
    }

    private Texture2D CreateCircleTexture(int size, int borderThickness)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerRadius = size * 0.5f - 1f;
        float innerRadius = outerRadius - borderThickness;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                if (distance <= outerRadius)
                {
                    float alpha = 0f;

                    if (distance >= innerRadius)
                    {
                        // Border area - solid with anti-aliasing
                        alpha = 1f;
                        if (distance > outerRadius - 1)
                        {
                            alpha = outerRadius - distance;
                        }
                        else if (distance < innerRadius + 1)
                        {
                            alpha = distance - innerRadius;
                        }
                    }
                    else
                    {
                        // Inner gradient area
                        float gradientFactor = distance / innerRadius;
                        // Start at 0.3 alpha at inner radius, fade to 0 at center
                        alpha = 0.3f * gradientFactor;
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleMouseInput();
        UpdateTargetIndicator();
    }

    private void UpdateTargetIndicator()
    {
        if (selectedCardIndex >= 0)
        {
            Card selectedCard = DeckManager.Instance.GetHand()[selectedCardIndex];

            if (selectedCard.IsUnitCard())
            {
                UpdatePreviewUnits();
                HideSpellPreview();
            }
            else if (selectedCard.IsSpellCard())
            {
                UpdateSpellPreview();
                ClearPreviewUnits();
            }
        }
        else
        {
            ClearPreviewUnits();
            HideSpellPreview();
        }
    }

    private void UpdatePreviewUnits()
    {
        Card selectedCard = DeckManager.Instance.GetHand()[selectedCardIndex];

        // Skip if this is not a unit card
        if (!selectedCard.IsUnitCard())
            return;

        // Get the positions where units would be spawned
        List<Vector2> spawnPositions = GetSpawnPositions(selectedCard);

        // Clear existing preview units if count changed
        if (previewUnits.Count != selectedCard.unitCount)
        {
            ClearPreviewUnits();
        }

        // Create or update preview units
        for (int i = 0; i < selectedCard.unitCount; i++)
        {
            if (i >= previewUnits.Count)
            {
                // Create new preview unit
                GameObject previewUnit = CreatePreviewUnit(selectedCard.unitPrefab);
                previewUnits.Add(previewUnit);
            }

            // Update position
            previewUnits[i].transform.position = spawnPositions[i];

            // Update color based on validity and mana
            bool hasEnoughMana = ManaManager.Instance.HasEnoughMana(selectedCard.manaCost);
            bool isValidPosition = IsValidTargetPosition(selectedCard);

            SpriteRenderer[] spriteRenderers = previewUnits[i]
                .GetComponentsInChildren<SpriteRenderer>();
            Color previewColor = (hasEnoughMana && isValidPosition) ? VALID_COLOR : INVALID_COLOR;

            foreach (SpriteRenderer sr in spriteRenderers)
            {
                sr.color = previewColor;
            }
        }
    }

    private void UpdateSpellPreview()
    {
        Card selectedCard = DeckManager.Instance.GetHand()[selectedCardIndex];

        // Skip if this is not a spell card
        if (!selectedCard.IsSpellCard())
            return;

        Vector3 mousePosition = GetMouseWorldPosition();

        // Show spell preview object
        spellPreviewObject.SetActive(true);
        spellPreviewObject.transform.position = mousePosition;

        // Scale the circle to match the spell's effect radius
        // The circle texture is created with a radius of 1 world unit at scale 1
        float spellRadius = selectedCard.spellPrefab.EffectRadius();
        spellPreviewObject.transform.localScale = Vector3.one * spellRadius * 0.25f;
        UpdateSpellPreviewCircle(spellRadius);

        // Update color based on validity and mana
        bool hasEnoughMana = ManaManager.Instance.HasEnoughMana(selectedCard.manaCost);
        bool isValidPosition = IsValidTargetPosition(selectedCard);

        Color previewColor = (hasEnoughMana && isValidPosition) ? VALID_COLOR : INVALID_COLOR;

        spellPreviewSprite.color = previewColor;
    }

    private void HideSpellPreview()
    {
        if (spellPreviewObject != null)
        {
            spellPreviewObject.SetActive(false);
        }
    }

    private GameObject CreatePreviewUnit(Unit unitPrefab)
    {
        // Instantiate the unit prefab
        GameObject previewUnit = Instantiate(unitPrefab.gameObject);

        // Make it non-interactive by removing physics and gameplay components
        DisableGameplayComponents(previewUnit);

        // Set up visual appearance
        SetupPreviewVisuals(previewUnit);

        return previewUnit;
    }

    private void DisableGameplayComponents(GameObject previewUnit)
    {
        // Remove or disable physics components
        Rigidbody2D rb = previewUnit.GetComponent<Rigidbody2D>();
        if (rb != null)
            Destroy(rb);

        // Remove colliders to prevent interaction
        Collider2D[] colliders = previewUnit.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            Destroy(col);
        }

        // Disable gameplay scripts
        Unit unitScript = previewUnit.GetComponent<Unit>();
        if (unitScript != null)
            unitScript.enabled = false;

        Health healthScript = previewUnit.GetComponent<Health>();
        if (healthScript != null)
            healthScript.enabled = false;

        // Disable animator to prevent animations
        Animator animator = previewUnit.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        // Disable particle systems
        ParticleSystem[] particleSystems = previewUnit.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.gameObject.SetActive(false);
        }
    }

    private void SetupPreviewVisuals(GameObject previewUnit)
    {
        // Get all sprite renderers and apply preview material/color
        SpriteRenderer[] spriteRenderers = previewUnit.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            // Apply preview material if it exists, otherwise just change color
            if (previewMaterial != null)
            {
                sr.material = previewMaterial;
            }
            sr.color = new Color(1f, 1f, 1f, 0.5f); // White and semi-transparent
        }
    }

    private void ClearPreviewUnits()
    {
        foreach (GameObject previewUnit in previewUnits)
        {
            if (previewUnit != null)
            {
                Destroy(previewUnit);
            }
        }
        previewUnits.Clear();
    }

    private void HandleKeyboardInput()
    {
        for (int i = 0; i < cardKeys.Length && i < DeckManager.Instance.GetHand().Count; i++)
        {
            if (Input.GetKeyDown(cardKeys[i]))
            {
                selectedCardIndex = i;
                CardsUI.Instance.SetActiveCardIndex(i);
            }
            else if (Input.GetKeyUp(cardKeys[i]) && selectedCardIndex >= 0)
            {
                if (selectedCardIndex >= 0)
                    TryPlayCardAtMousePosition(selectedCardIndex);

                selectedCardIndex = -1;
                CardsUI.Instance.SetActiveCardIndex(-1);
                ClearPreviewUnits();
                HideSpellPreview();
            }
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonUp(0) && selectedCardIndex >= 0)
        {
            if (selectedCardIndex >= 0)
                TryPlayCardAtMousePosition(selectedCardIndex);

            selectedCardIndex = -1;
            CardsUI.Instance.SetActiveCardIndex(-1);
            ClearPreviewUnits();
            HideSpellPreview();
        }
    }

    private void OnCardButtonClicked(int index)
    {
        Debug.Log("Card button clicked: " + index);

        // Clear previous previews if switching cards
        if (selectedCardIndex != index)
        {
            ClearPreviewUnits();
            HideSpellPreview();
        }

        selectedCardIndex = index;
        CardsUI.Instance.SetActiveCardIndex(index);
    }

    private bool IsValidTargetPosition(Card card)
    {
        Vector2 targetPosition = GetMouseWorldPosition();

        if (card.IsUnitCard())
        {
            if (!playableUnitBounds.Contains(targetPosition))
                return false;

            // For unit cards, check if spawn position is not inside a wall
            Collider2D wallCollider = Physics2D.OverlapPoint(targetPosition);
            if (wallCollider != null && wallCollider.CompareTag("Wall"))
                return false;
        }
        else if (card.IsSpellCard())
        {
            if (!playableSpellBounds.Contains(targetPosition))
                return false;
        }

        return true;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePosition);
    }

    private void TryPlayCardAtMousePosition(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= DeckManager.Instance.GetHand().Count)
        {
            Debug.Log("Tried to play card at invalid index");
            return;
        }

        Card cardToPlay = DeckManager.Instance.GetHand()[cardIndex];

        if (!ManaManager.Instance.HasEnoughMana(cardToPlay.manaCost))
        {
            Debug.Log(
                $"Not enough mana to play {cardToPlay.cardName}. Need {cardToPlay.manaCost}, have {ManaManager.Instance.CurrentMana}"
            );
            return;
        }

        if (!IsValidTargetPosition(cardToPlay))
        {
            Debug.Log("Invalid target position");
            return;
        }

        // Clear previews before playing card
        ClearPreviewUnits();
        HideSpellPreview();

        // Spend mana and cycle card
        ManaManager.Instance.SpendMana(cardToPlay.manaCost);
        DeckManager.Instance.CycleCard(cardIndex);

        // Play the card based on its type
        if (cardToPlay.IsUnitCard())
        {
            PlayUnitCard(cardToPlay);
        }
        else if (cardToPlay.IsSpellCard())
        {
            PlaySpellCard(cardToPlay);
        }
    }

    private void PlayUnitCard(Card card)
    {
        UnitManager.Instance.SpawnUnits(GetSpawnPositions(card), card.unitPrefab, UnitType.Friend);

        Debug.Log($"Played unit card: {card.cardName}");
    }

    private void PlaySpellCard(Card card)
    {
        Vector2 targetPosition = GetMouseWorldPosition();

        // Instantiate the spell and cast it
        GameObject spellObject = Instantiate(
            card.spellPrefab.gameObject,
            targetPosition,
            Quaternion.identity
        );
        Spell spell = spellObject.GetComponent<Spell>();

        if (spell != null)
        {
            spell.Cast(targetPosition, UnitType.Friend);
            Debug.Log($"Cast spell: {card.cardName} at {targetPosition}");
        }
        else
        {
            Debug.LogError($"Spell card {card.cardName} does not have a valid Spell component!");
            Destroy(spellObject);
        }
    }

    private List<Vector2> GetSpawnPositions(Card card)
    {
        if (!card.IsUnitCard())
        {
            Debug.LogError("GetSpawnPositions called on non-unit card!");
            return new List<Vector2>();
        }

        float unitRadius = card.unitPrefab.GetComponent<CircleCollider2D>().radius;
        return UnitUtils.GetSpreadPositions(GetMouseWorldPosition(), card.unitCount, unitRadius);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(playableUnitBounds.center, playableUnitBounds.size);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(playableSpellBounds.center, playableSpellBounds.size);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Clean up preview units when the object is destroyed
        ClearPreviewUnits();

        // Clean up the preview material
        if (previewMaterial != null)
        {
            Destroy(previewMaterial);
        }

        // Clean up the circle texture
        Destroy(circleTexture1x);
        Destroy(circleTexture2x);
        Destroy(circleTexture3x);

        // Clean up spell preview object
        if (spellPreviewObject != null)
        {
            Destroy(spellPreviewObject);
        }
    }
}
