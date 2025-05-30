using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CardUIButton : MonoBehaviour, IPointerDownHandler
{
    [Header("UI References")]
    [SerializeField]
    private UnityEngine.UI.Image cardImage;

    [SerializeField]
    private UnityEngine.UI.Image glowImage;

    [SerializeField]
    private TMPro.TextMeshProUGUI manaCostText;

    private Card associatedCard;
    private int handIndex;

    [NonSerialized]
    public UnityEvent<int> onMouseDown = new();

    public void SetupCard(Card card, int index)
    {
        associatedCard = card;
        handIndex = index;

        if (cardImage != null)
            cardImage.sprite = card.cardArt;
        if (manaCostText != null)
            manaCostText.text = card.manaCost.ToString();

        SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        onMouseDown?.Invoke(handIndex);
    }

    public void SetActive(bool active)
    {
        // Raise card position when active
        Vector3 position = transform.localPosition;
        position.y = active ? 20f : 0f;
        transform.localPosition = position;

        // Add glow effect when active
        if (glowImage != null)
        {
            Color glowColor = glowImage.color;
            glowColor.a = active ? 0.5f : 0f;
            glowImage.color = glowColor;
        }
    }
}
