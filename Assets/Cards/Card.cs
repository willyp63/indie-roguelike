using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card System/Card")]
public class Card : ScriptableObject
{
    [Header("Card Information")]
    public string cardName;

    [TextArea(3, 5)]
    public string description;

    public Sprite cardArt;

    public int manaCost;

    public int unitCount;

    public Unit unitPrefab;
}
