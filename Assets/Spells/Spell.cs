using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Spell : MonoBehaviour
{
    [SerializeField]
    private float effectRadius = 1.0f;

    public float EffectRadius()
    {
        return effectRadius;
    }

    public abstract void Cast(Vector2 targetPosition, UnitType unitType);
}
