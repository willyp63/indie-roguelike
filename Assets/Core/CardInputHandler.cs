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
    private SpriteRenderer targetIndicator;

    [SerializeField]
    private Bounds playableBounds;

    private int selectedCardIndex = -1;

    private Camera playerCamera;

    private void Start()
    {
        playerCamera = Camera.main;

        CardsUI.Instance.onCardButtonClicked.AddListener(OnCardButtonClicked);

        // Hide the target indicator initially
        if (targetIndicator != null)
            targetIndicator.gameObject.SetActive(false);
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleMouseInput();
        UpdateTargetIndicator();
    }

    private void UpdateTargetIndicator()
    {
        if (targetIndicator == null)
            return;

        if (selectedCardIndex >= 0)
        {
            // Show indicator and position it at mouse
            targetIndicator.gameObject.SetActive(true);
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            targetIndicator.transform.position = mouseWorldPos;

            // Set color based on validity and mana
            Card selectedCard = DeckManager.Instance.GetHand()[selectedCardIndex];
            bool hasEnoughMana = ManaManager.Instance.HasEnoughMana(selectedCard.manaCost);
            bool isValidPosition = IsValidSpawnPosition();

            if (hasEnoughMana && isValidPosition)
            {
                targetIndicator.color = Color.green;
            }
            else
            {
                targetIndicator.color = Color.red;
            }
        }
        else
        {
            // Hide indicator when no card is selected
            targetIndicator.gameObject.SetActive(false);
        }
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
        }
    }

    private void OnCardButtonClicked(int index)
    {
        Debug.Log("Card button clicked: " + index);

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

        ManaManager.Instance.SpendMana(cardToPlay.manaCost);
        DeckManager.Instance.CycleCard(cardIndex);
        UnitManager.Instance.SpawnUnits(
            GetMouseWorldPosition(),
            cardToPlay.unitPrefab,
            UnitType.Friend,
            cardToPlay.unitCount
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(playableBounds.center, playableBounds.size);
    }
}
