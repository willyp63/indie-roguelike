using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritWell : MonoBehaviour
{
    [SerializeField]
    private bool isEnemy = false;

    public bool IsEnemy => isEnemy;

    [SerializeField]
    private float prioritizeWellDistance = 0f;
    public float PrioritizeWellDistance => prioritizeWellDistance;

    [SerializeField]
    private float teleportDistance = 0f;
    public float TeleportDistance => teleportDistance;

    [Header("Portal Settings")]
    [SerializeField]
    private Transform protalTransform;

    [SerializeField]
    private Vector3 rotationSpeed = new Vector3(0, 90, 0); // Degrees per second

    void Update()
    {
        // Rotate the object by rotationSpeed degrees per second
        protalTransform.Rotate(rotationSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, PrioritizeWellDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, TeleportDistance);
    }
}
