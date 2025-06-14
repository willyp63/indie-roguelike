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

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!isEnemy)
            return;

        // Check if the colliding object is a friendly unit
        Health health = other.collider.GetComponent<Health>();
        Unit unit = other.collider.GetComponent<Unit>();
        if (unit != null && health != null && health.IsFriend())
        {
            unit.StartTeleporting(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, PrioritizeWellDistance);
    }
}
