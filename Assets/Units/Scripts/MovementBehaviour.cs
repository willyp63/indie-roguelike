using UnityEngine;

public abstract class MovementBehaviour : MonoBehaviour
{
    [SerializeField]
    private float speed = 2.0f;

    private float speedFactor = 1f;

    public void ModifySpeed(float factor)
    {
        speedFactor *= (1f + factor);
    }

    public void ResetSpeed()
    {
        speedFactor = 1.0f;
    }

    public float GetSpeed()
    {
        return speed * speedFactor;
    }

    public abstract void Move(Vector2 moveDirection);
}
