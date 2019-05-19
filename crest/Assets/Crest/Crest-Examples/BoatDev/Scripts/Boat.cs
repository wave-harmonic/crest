// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

public abstract class BoatBase : MonoBehaviour
{
    public abstract Vector3 DisplacementToBoat { get; set; }
    public abstract float BoatWidth { get; }
    public abstract bool InWater { get; }
    public abstract Rigidbody RB { get; set; }
}
