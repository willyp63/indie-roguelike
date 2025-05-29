using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelectionUI : MonoBehaviour
{
    [System.Serializable]
    public class UnitTypeData
    {
        public string unitName;
        public GameObject unitPrefab;
        public Sprite unitIcon;
    }

    [Header("UI References")]
    public Transform buttonContainer;
    public GameObject unitButtonPrefab;

    [Header("Unit Types")]
    public UnitTypeData[] unitTypes;

    [Header("Styling")]
    public Color selectedButtonColor = Color.green;
    public Color normalButtonColor = Color.white;

    private UnitTypeData currentSelectedUnit;
    private List<Button> unitButtons = new List<Button>();
    private UnitSpawner unitSpawner;

    void Start()
    {
        unitSpawner = FindObjectOfType<UnitSpawner>();
        if (unitSpawner == null)
        {
            Debug.LogError("UnitSpawner not found! Make sure there's a UnitSpawner in the scene.");
        }

        CreateUnitButtons();
    }

    void CreateUnitButtons()
    {
        // Clear existing buttons
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
        unitButtons.Clear();

        // Create buttons for each unit type
        for (int i = 0; i < unitTypes.Length; i++)
        {
            int index = i; // Capture for closure
            UnitTypeData unitData = unitTypes[i];

            GameObject buttonObj = Instantiate(unitButtonPrefab, buttonContainer);
            Button button = buttonObj.GetComponent<Button>();

            // Set up button visuals
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = normalButtonColor;
            }

            // Set up icon if available
            Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && unitData.unitIcon != null)
            {
                iconImage.sprite = unitData.unitIcon;
            }

            // Set up text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = unitData.unitName;
            }

            // Add click listener
            button.onClick.AddListener(() => SelectUnit(index));

            unitButtons.Add(button);
        }
    }

    public void SelectUnit(int unitIndex)
    {
        if (unitIndex < 0 || unitIndex >= unitTypes.Length)
        {
            Debug.LogError($"Invalid unit index: {unitIndex}");
            return;
        }

        currentSelectedUnit = unitTypes[unitIndex];

        UpdateButtonVisuals();

        Debug.Log($"Selected unit: {currentSelectedUnit.unitName}");
    }

    void UpdateButtonVisuals()
    {
        for (int i = 0; i < unitButtons.Count; i++)
        {
            Image buttonImage = unitButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                bool isSelected =
                    currentSelectedUnit != null && unitTypes[i] == currentSelectedUnit;
                buttonImage.color = isSelected ? selectedButtonColor : normalButtonColor;
            }
        }
    }

    // Public method to get currently selected unit
    public UnitTypeData GetSelectedUnit()
    {
        return currentSelectedUnit;
    }
}
