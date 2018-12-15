using UnityEngine;

public interface IBoat
{
    Vector3 DisplacementToBoat { get; }
    float BoatWidth { get; }
    bool InWater { get; }
    Rigidbody RB { get; }
}
