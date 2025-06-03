using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ManaBarUI : Singleton<ManaBarUI>
{
    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI manaText;

    [SerializeField]
    private Transform manaBarFill;

    // Start is called before the first frame update
    void Start()
    {
        UpdateManaUI();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateManaUI();
    }

    private void UpdateManaUI()
    {
        if (ManaManager.Instance == null)
            return;

        float currentMana = ManaManager.Instance.CurrentMana;
        float maxMana = ManaManager.Instance.MaxMana;

        // Update text display
        if (manaText != null)
        {
            manaText.text = $"{Mathf.FloorToInt(currentMana)}";
        }

        // Update mana bar fill by scaling
        if (manaBarFill != null)
        {
            float manaPercentage = maxMana > 0 ? currentMana / maxMana : 0f;
            manaBarFill.localScale = new Vector3(manaPercentage, 1f, 1f);
        }
    }
}
