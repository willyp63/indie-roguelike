using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TreeData
{
    public GameObject prefab;

    public float weight;
}

public class TreePlacer : MonoBehaviour
{
    [SerializeField]
    private TreeData[] treeData; // Array of tree data with prefabs and weights

    [SerializeField]
    private float spacing = 1f; // Base spacing between trees

    [SerializeField]
    private float randomOffset = 0.5f; // How much random offset to add to the grid positions

    [SerializeField]
    private float density = 0.7f; // Chance of spawning a tree at each position

    [SerializeField]
    private Vector2 areaSize = new Vector2(10f, 10f);

    [SerializeField]
    private LayerMask openAreaLayer;

    [SerializeField]
    private Color colorA = Color.green; // First color for random range

    [SerializeField]
    private Color colorB = Color.yellow; // Second color for random range

    private void Start()
    {
        PlaceTrees();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Destroy all existing trees
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            PlaceTrees();
        }
    }

    private TreeData SelectWeightedRandomTree()
    {
        // Calculate total weight
        float totalWeight = 0f;
        foreach (TreeData data in treeData)
        {
            totalWeight += data.weight;
        }

        // Generate random value between 0 and total weight
        float randomValue = Random.Range(0f, totalWeight);

        // Find the tree that corresponds to this random value
        float currentWeight = 0f;
        foreach (TreeData data in treeData)
        {
            currentWeight += data.weight;
            if (randomValue <= currentWeight)
            {
                return data;
            }
        }

        // Fallback (should never reach here)
        return treeData[0];
    }

    private void PlaceTrees()
    {
        // Calculate the bounds of the spawn area
        Vector2 startPos = (Vector2)transform.position - (areaSize / 2f);
        Vector2 endPos = (Vector2)transform.position + (areaSize / 2f);

        // Loop through the grid
        for (float x = startPos.x; x <= endPos.x; x += spacing)
        {
            for (float y = startPos.y; y <= endPos.y; y += spacing)
            {
                // Random chance to skip this position based on density
                if (Random.value > density)
                    continue;

                // Add random offset to position
                Vector2 randomizedOffset = new Vector2(
                    Random.Range(-randomOffset, randomOffset),
                    Random.Range(-randomOffset, randomOffset)
                );

                Vector2 spawnPosition = new Vector2(x, y) + randomizedOffset;

                // Skip if position is outside the area bounds
                if (
                    spawnPosition.x < startPos.x
                    || spawnPosition.x > endPos.x
                    || spawnPosition.y < startPos.y
                    || spawnPosition.y > endPos.y
                )
                {
                    continue;
                }

                // Check if position overlaps with an "Open Area"
                Collider2D[] colliders = Physics2D.OverlapCircleAll(spawnPosition, 0.1f);
                bool canSpawn = true;

                foreach (Collider2D collider in colliders)
                {
                    if (collider.CompareTag("Open Area"))
                    {
                        canSpawn = false;
                        break;
                    }
                }

                // Spawn tree if position is valid
                if (canSpawn)
                {
                    // Check if we have any tree data
                    if (treeData.Length == 0)
                    {
                        Debug.LogWarning("No tree data assigned to TreePlacer!");
                        continue;
                    }

                    // Select a weighted random tree
                    TreeData selectedData = SelectWeightedRandomTree();
                    GameObject tree = Instantiate(
                        selectedData.prefab,
                        spawnPosition,
                        Quaternion.identity,
                        transform
                    );

                    tree.transform.localScale = new Vector3(Random.value < 0.5f ? -1f : 1f, 1f, 1f);

                    // Apply random color between colorA and colorB
                    Renderer treeRenderer = tree.GetComponent<Renderer>();
                    if (treeRenderer != null)
                    {
                        Color randomColor = Color.Lerp(colorA, colorB, Random.value);
                        treeRenderer.material.color = randomColor;
                    }
                }
            }
        }
    }

    // Optional: Visualize the spawn area in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}
