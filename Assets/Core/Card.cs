using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Unit,
    Spell,
}

[CreateAssetMenu(fileName = "New Card", menuName = "Card System/Card")]
public class Card : ScriptableObject
{
    [Header("Card Information")]
    public string cardName;

    [TextArea(3, 5)]
    public string description;

    public Sprite cardArt;

    public int manaCost;

    [Header("Unit Card")]
    public int unitCount;
    public Unit unitPrefab;

    [Header("Spell Card")]
    public Spell spellPrefab;

    public CardType GetCardType()
    {
        if (unitPrefab != null && spellPrefab != null)
        {
            Debug.LogError(
                $"Card '{cardName}' has both unit and spell prefabs assigned! Cards should only have one or the other."
            );
        }

        if (spellPrefab != null)
            return CardType.Spell;

        return CardType.Unit;
    }

    public bool IsSpellCard()
    {
        return GetCardType() == CardType.Spell;
    }

    public bool IsUnitCard()
    {
        return GetCardType() == CardType.Unit;
    }
}
