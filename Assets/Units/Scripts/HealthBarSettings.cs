using UnityEngine;

[CreateAssetMenu(fileName = "HealthBarSettings", menuName = "Game/Health Bar Settings")]
public class HealthBarSettings : ScriptableObject
{
    [Header("Health Bar Sprites")]
    [SerializeField]
    private Sprite frameSprite; // The 1-pixel border frame

    [SerializeField]
    private Sprite filledBarSprite; // The solid color bar with shadow

    [SerializeField]
    private Sprite unfilledBarSprite; // The gray unfilled bar

    [Header("Team Indicator Dot Sprites")]
    [SerializeField]
    private Sprite dotFrameSprite; // The dot frame for team indicator

    [SerializeField]
    private Sprite dotFillSprite; // The dot fill for team indicator

    // Public getters
    public Sprite FrameSprite => frameSprite;
    public Sprite FilledBarSprite => filledBarSprite;
    public Sprite UnfilledBarSprite => unfilledBarSprite;
    public Sprite DotFrameSprite => dotFrameSprite;
    public Sprite DotFillSprite => dotFillSprite;

    // Validation
    public bool IsValid()
    {
        return frameSprite != null
            && filledBarSprite != null
            && unfilledBarSprite != null
            && dotFrameSprite != null
            && dotFillSprite != null;
    }
}
