using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField]
    private List<Card> playerDeckCards = new List<Card>();

    [Header("References")]
    [SerializeField]
    private DeckManager handManager;

    [SerializeField]
    private ManaManager manaSystem;

    [SerializeField]
    private CardInputHandler inputHandler;

    public void RestartGame()
    {
        // Reset mana
        ManaManager.Instance.AddMana(-manaSystem.CurrentMana);

        // Reshuffle deck and reinitialize hand
        DeckManager.Instance.Shuffle();
    }
}
