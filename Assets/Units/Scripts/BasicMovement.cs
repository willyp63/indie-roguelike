using System;
using UnityEngine;

public class BasicMovement : MovementBehaviour
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void Move(Vector2 moveDirection)
    {
        if (rb == null)
            return;

        // Apply the force
        float adjustedSpeed = GetSpeed() * 0.066f;
        Vector2 force = moveDirection * adjustedSpeed * rb.mass / Time.fixedDeltaTime;
        rb.AddForce(force);
    }
}
