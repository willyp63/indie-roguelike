using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [SerializeField]
    private int priority;

    public int Priority()
    {
        return priority;
    }
}
