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
    private Bounds playableBounds;

    private int selectedCardIndex = -1;

    private Camera playerCamera;

    // Preview unit system
    private List<GameObject> previewUnits = new List<GameObject>();
    private Material previewMaterial;

    private void Start()
    {
        playerCamera = Camera.main;

        CardsUI.Instance.onCardButtonClicked.AddListener(OnCardButtonClicked);

        // Create preview material
        CreatePreviewMaterial();
    }

    private void CreatePreviewMaterial()
    {
        // Create a material for preview units (white and transparent)
        previewMaterial = new Material(Shader.Find("Sprites/Default"));
        previewMaterial.color = new Color(1f, 1f, 1f, 0.5f); // White and semi-transparent
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
            UpdatePreviewUnits();
        }
        else
        {
            ClearPreviewUnits();
        }
    }

    private void UpdatePreviewUnits()
    {
        Card selectedCard = DeckManager.Instance.GetHand()[selectedCardIndex];

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
            bool isValidPosition = IsValidSpawnPosition();

            SpriteRenderer[] spriteRenderers = previewUnits[i]
                .GetComponentsInChildren<SpriteRenderer>();
            Color previewColor =
                (hasEnoughMana && isValidPosition)
                    ? new Color(1f, 1f, 1f, 0.5f)
                    : // White and semi-transparent when valid
                    new Color(1f, 0.5f, 0.5f, 0.5f); // Reddish when invalid

            foreach (SpriteRenderer sr in spriteRenderers)
            {
                sr.color = previewColor;
            }
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
                ClearPreviewUnits(); // Clear preview units when card is deselected
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
            ClearPreviewUnits(); // Clear preview units when card is deselected
        }
    }

    private void OnCardButtonClicked(int index)
    {
        Debug.Log("Card button clicked: " + index);

        // Clear previous preview units if switching cards
        if (selectedCardIndex != index)
        {
            ClearPreviewUnits();
        }

        selectedCardIndex = index;
        CardsUI.Instance.SetActiveCardIndex(index);
    }

    private bool IsValidSpawnPosition()
    {
        Vector2 spawnPosition = GetMouseWorldPosition();
        if (!playableBounds.Contains(spawnPosition))
            return false;

        // Check if spawn position is inside a wall
        Collider2D wallCollider = Physics2D.OverlapPoint(spawnPosition);
        if (wallCollider != null && wallCollider.CompareTag("Wall"))
            return false;

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
            Debug.Log("Trid to play card at invalid index");
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

        if (!IsValidSpawnPosition())
        {
            Debug.Log("Invalid spawn position");
            return;
        }

        // Clear preview units before spawning real units
        ClearPreviewUnits();

        ManaManager.Instance.SpendMana(cardToPlay.manaCost);
        DeckManager.Instance.CycleCard(cardIndex);
        UnitManager.Instance.SpawnUnits(
            GetSpawnPositions(cardToPlay),
            cardToPlay.unitPrefab,
            UnitType.Friend
        );
    }

    private List<Vector2> GetSpawnPositions(Card card)
    {
        float unitRadius = card.unitPrefab.GetComponent<CircleCollider2D>().radius;
        return UnitUtils.GetSpreadPositions(GetMouseWorldPosition(), card.unitCount, unitRadius);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(playableBounds.center, playableBounds.size);
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
    }
}
