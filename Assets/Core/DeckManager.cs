using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DeckManager : Singleton<DeckManager>
{
    [SerializeField]
    private int handSize = 4;

    [SerializeField]
    private List<Card> initialDeck = new();

    private List<Card> currentDeck = new();

    [NonSerialized]
    public UnityEvent onHandChange = new();

    protected override void Awake()
    {
        base.Awake();

        InitializeDeck();
    }

    public void InitializeDeck()
    {
        if (initialDeck.Count < handSize + 1)
        {
            Debug.LogError("Deck is too small!!");
        }

        currentDeck.Clear();
        currentDeck.AddRange(initialDeck);
        Shuffle();
    }

    public void Shuffle()
    {
        int targetUniqueCount = handSize + 1; // 5 cards that should be unique if possible

        // Get unique cards and group duplicates
        Dictionary<string, List<int>> cardGroups = new Dictionary<string, List<int>>();
        for (int i = 0; i < currentDeck.Count; i++)
        {
            string cardName = currentDeck[i].cardName;
            if (!cardGroups.ContainsKey(cardName))
            {
                cardGroups[cardName] = new List<int>();
            }
            cardGroups[cardName].Add(i);
        }

        // Create a new deck arrangement
        List<Card> newDeck = new List<Card>(currentDeck.Count);
        List<bool> used = new List<bool>(new bool[currentDeck.Count]);

        // First, place unique cards in the first positions
        List<string> uniqueCardNames = new List<string>(cardGroups.Keys);
        int uniqueCardsPlaced = 0;

        // Shuffle the unique card names to randomize which unique cards come first
        for (int i = uniqueCardNames.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            string temp = uniqueCardNames[i];
            uniqueCardNames[i] = uniqueCardNames[randomIndex];
            uniqueCardNames[randomIndex] = temp;
        }

        // Place one card of each unique type in the first positions
        foreach (string cardName in uniqueCardNames)
        {
            if (uniqueCardsPlaced >= targetUniqueCount)
                break;

            List<int> indices = cardGroups[cardName];
            int randomIndexFromGroup = UnityEngine.Random.Range(0, indices.Count);
            int chosenIndex = indices[randomIndexFromGroup];

            newDeck.Add(currentDeck[chosenIndex]);
            used[chosenIndex] = true;
            uniqueCardsPlaced++;
        }

        // Collect remaining cards
        List<Card> remainingCards = new List<Card>();
        for (int i = 0; i < currentDeck.Count; i++)
        {
            if (!used[i])
            {
                remainingCards.Add(currentDeck[i]);
            }
        }

        // Shuffle remaining cards using Fisher-Yates
        for (int i = remainingCards.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            Card temp = remainingCards[i];
            remainingCards[i] = remainingCards[randomIndex];
            remainingCards[randomIndex] = temp;
        }

        // Add remaining cards to the deck
        newDeck.AddRange(remainingCards);

        // Replace current deck with new arrangement
        currentDeck = newDeck;

        onHandChange?.Invoke();
    }

    public void CycleCard(int handIndex)
    {
        if (handIndex >= handSize)
        {
            Debug.LogError("Invalid hand index!!");
            return;
        }

        // remove card and then add it back to the bottom
        Card card = currentDeck[handIndex];
        currentDeck.RemoveAt(handIndex);
        currentDeck.Add(card);

        onHandChange?.Invoke();
    }

    public List<Card> GetHand()
    {
        return currentDeck.GetRange(0, handSize);
    }

    public Card GetNextCard()
    {
        return currentDeck[handSize];
    }
}
