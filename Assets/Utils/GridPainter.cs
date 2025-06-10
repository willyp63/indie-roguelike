using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPainter : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector3 centerPosition = Vector3.zero;
    public float cellSize = 1f;
    public int gridWidth = 8;
    public int gridHeight = 8;

    [Header("Colors")]
    public Color color1 = Color.white;
    public Color color2 = Color.black;

    [Header("Rendering Settings")]
    public int sortingOrder = 0;
    public string sortingLayerName = "Default";
    public bool generateOnStart = true;

    private List<GameObject> gridCells = new List<GameObject>();
    private Texture2D whiteTexture;

    // Start is called before the first frame update
    void Start()
    {
        CreateWhiteTexture();
        if (generateOnStart)
        {
            GenerateGrid();
        }
    }

    // Update is called once per frame
    void Update() { }

    void CreateWhiteTexture()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    public void GenerateGrid()
    {
        ClearGrid();

        // Calculate the starting position (bottom-left corner of the grid)
        Vector3 startPos =
            centerPosition
            - new Vector3(
                (gridWidth * cellSize) / 2f - cellSize / 2f,
                (gridHeight * cellSize) / 2f - cellSize / 2f,
                0f
            );

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CreateGridCell(x, y, startPos);
            }
        }
    }

    void CreateGridCell(int x, int y, Vector3 startPos)
    {
        // Create a new GameObject for this cell
        GameObject cell = new GameObject($"GridCell_{x}_{y}");
        cell.transform.parent = this.transform;

        // Calculate the position of this cell
        Vector3 cellPosition = startPos + new Vector3(x * cellSize, y * cellSize, 0f);
        cell.transform.position = cellPosition;

        // Add SpriteRenderer component
        SpriteRenderer spriteRenderer = cell.AddComponent<SpriteRenderer>();

        // Create sprite from the white texture
        Sprite cellSprite = Sprite.Create(
            whiteTexture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        spriteRenderer.sprite = cellSprite;

        // Determine which color to use based on checkerboard pattern
        bool isEvenCell = (x + y) % 2 == 0;
        Color baseColor = isEvenCell ? color1 : color2;

        float randomOpacityDelta = Random.Range(-0.033f, 0.033f);
        baseColor.a += randomOpacityDelta;

        spriteRenderer.color = baseColor;

        // Set sorting properties
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.sortingLayerName = sortingLayerName;

        // Scale the sprite to match cell size
        cell.transform.localScale = new Vector3(cellSize, cellSize, 1f);

        // Add to our list for management
        gridCells.Add(cell);
    }

    public void ClearGrid()
    {
        foreach (GameObject cell in gridCells)
        {
            if (cell != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(cell);
                }
                else
                {
                    DestroyImmediate(cell);
                }
            }
        }
        gridCells.Clear();
    }

    public void UpdateGrid()
    {
        GenerateGrid();
    }

    void OnDestroy()
    {
        ClearGrid();
        if (whiteTexture != null)
        {
            if (Application.isPlaying)
            {
                Destroy(whiteTexture);
            }
            else
            {
                DestroyImmediate(whiteTexture);
            }
        }
    }

    void OnValidate()
    {
        // Regenerate grid when values change in editor
        if (Application.isPlaying && gridCells.Count > 0)
        {
            UpdateGrid();
        }
    }
}
