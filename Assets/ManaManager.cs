using System;
using UnityEngine;

public class ManaManager : Singleton<ManaManager>
{
    [Header("Mana Settings")]
    [SerializeField]
    private float maxMana = 10f;

    [SerializeField]
    private float manaRegenRate = 1f; // Mana per second

    private float currentMana = 0f;

    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;

    public bool HasEnoughMana(int cost) => currentMana >= cost;

    private void Start()
    {
        currentMana = 0f;
    }

    private void Update()
    {
        if (currentMana < maxMana)
            AddMana(manaRegenRate * Time.deltaTime);
    }

    public void SpendMana(int cost)
    {
        if (!HasEnoughMana(cost))
            Debug.LogError("You spent mana that you don't have!!");

        currentMana = Mathf.Max(currentMana - cost, 0);
    }

    public void AddMana(float amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
    }
}
