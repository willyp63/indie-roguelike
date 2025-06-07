using UnityEngine;
using UnityEngine.Rendering;

public class DynamicSortingOrder : MonoBehaviour
{
    [SerializeField]
    private bool isStatic = false;

    private SpriteRenderer spriteRenderer;
    private SortingGroup sortingGroup;

    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        sortingGroup = GetComponent<SortingGroup>();

        UpdateSoritingOrder();
    }

    void Update()
    {
        if (!isStatic)
            UpdateSoritingOrder();
    }

    void UpdateSoritingOrder()
    {
        // Update the sorting order based on the Y position
        int sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = sortingOrder;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }
}
