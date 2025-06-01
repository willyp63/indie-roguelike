using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class CardClickEvent : UnityEvent<Card, int> { }

public class CardsUI : Singleton<CardsUI>
{
    [Header("Card UI References")]
    [SerializeField]
    private List<CardUIButton> handCardUIButtons = new();

    [SerializeField]
    private CardUIButton nextCardUIButton;

    [NonSerialized]
    public UnityEvent<int> onCardButtonClicked = new();

    private void Start()
    {
        DeckManager.Instance.onHandChange.AddListener(UpdateCardUI);

        // Update UI initially
        UpdateCardUI();
    }

    private void Update()
    {
        UpdateCardCooldowns();
    }

    public void SetActiveCardIndex(int index)
    {
        for (int i = 0; i < handCardUIButtons.Count; i++)
        {
            handCardUIButtons[i].SetActive(i == index);
        }
    }

    private void UpdateCardCooldowns()
    {
        List<Card> hand = DeckManager.Instance.GetHand();

        for (int i = 0; i < hand.Count; i++)
        {
            int cardCost = hand[i].manaCost;
            float maxCooldown = cardCost / ManaManager.Instance.ManaRegenRate;
            float currentCooldown = Mathf.Max(
                0,
                (cardCost - ManaManager.Instance.CurrentMana) / ManaManager.Instance.ManaRegenRate
            );
            handCardUIButtons[i].SetCooldownPercent(currentCooldown / maxCooldown);
        }
    }

    private void UpdateCardUI()
    {
        // Get current hand and next card from DeckManager
        List<Card> currentHand = DeckManager.Instance.GetHand();
        Card nextCard = DeckManager.Instance.GetNextCard();

        // Update hand card UIs
        for (int i = 0; i < handCardUIButtons.Count; i++)
        {
            if (handCardUIButtons[i] != null)
            {
                handCardUIButtons[i].SetupCard(currentHand[i], i);

                // Subscribe to card button click events
                handCardUIButtons[i].onMouseDown.RemoveAllListeners();
                handCardUIButtons[i].onMouseDown.AddListener(OnCardButtonClicked);
            }
        }

        // Update next card UI
        if (nextCardUIButton != null)
        {
            nextCardUIButton.SetupCard(nextCard, -1);
        }
    }

    private void OnCardButtonClicked(int index)
    {
        onCardButtonClicked?.Invoke(index);
    }
}
