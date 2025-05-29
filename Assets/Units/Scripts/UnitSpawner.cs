using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [SerializeField]
    private Bounds spawnArea;

    private int scaleFactor = 1;

    private Camera playerCamera;
    private UnitSelectionUI unitSelectionUI;

    private static readonly int SPAWN_COUNT = 10;

    void Start()
    {
        // Get the main camera if not assigned
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }

        // Find the unit selection UI
        unitSelectionUI = FindObjectOfType<UnitSelectionUI>();
    }

    void Update()
    {
        HandleMouseInput();
        HandleKeyInput();
    }

    void HandleKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            scaleFactor += 1;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            scaleFactor -= 1;

        scaleFactor = Mathf.Clamp(scaleFactor, 1, 3);
    }

    void HandleMouseInput()
    {
        // Left mouse button - spawn friendly unit
        if (Input.GetMouseButtonDown(0))
        {
            SpawnUnit(UnitType.Friend);
        }

        // Right mouse button - spawn enemy unit
        if (Input.GetMouseButtonDown(1))
        {
            SpawnUnit(UnitType.Enemy);
        }
    }

    void SpawnUnit(UnitType unitType)
    {
        GameObject prefabToSpawn = unitSelectionUI?.GetSelectedUnit()?.unitPrefab;
        if (prefabToSpawn == null)
            return;

        Vector3 spawnPosition = GetSpawnPosition();
        if (!spawnArea.Contains(spawnPosition))
        {
            Debug.Log("Invalid spawn position - not on spawnable ground");
            return;
        }

        // Check if spawn position is inside a wall
        Collider2D wallCollider = Physics2D.OverlapPoint(spawnPosition);
        if (wallCollider != null && wallCollider.CompareTag("Wall"))
        {
            Debug.Log("Invalid spawn position - inside a wall");
            return;
        }

        List<Vector2> spawnPositions = UnitUtils.GetSpreadPositions(spawnPosition, SPAWN_COUNT);
        string unitTypeName = unitType == UnitType.Friend ? "Friendly" : "Enemy";

        foreach (Vector2 pos in spawnPositions)
        {
            // Instantiate the unit at each position
            GameObject spawnedUnit = Instantiate(prefabToSpawn, pos, Quaternion.identity);

            // Set the unit type on the Health component
            Health healthComponent = spawnedUnit.GetComponent<Health>();
            healthComponent?.SetUnitType(unitType);
            healthComponent?.SetScaleFactor(scaleFactor);

            Debug.Log($"Spawned {unitTypeName} {prefabToSpawn.name} at {pos}");
        }
    }

    Vector3 GetSpawnPosition()
    {
        // Convert mouse position to world position
        Vector3 mousePosition = Input.mousePosition;
        Vector3 worldPosition = playerCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f; // Keep Z at 0 for 2D

        return worldPosition;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnArea.center, spawnArea.size);
    }
}
