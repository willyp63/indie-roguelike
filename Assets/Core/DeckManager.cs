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
        for (int i = currentDeck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            Card temp = currentDeck[i];
            currentDeck[i] = currentDeck[randomIndex];
            currentDeck[randomIndex] = temp;
        }

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
