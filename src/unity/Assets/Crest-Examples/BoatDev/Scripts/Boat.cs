// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

public interface IBoat
{
    Vector3 DisplacementToBoat { get; }
    float BoatWidth { get; }
    bool InWater { get; }
    Rigidbody RB { get; }
}
