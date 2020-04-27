using UnityEngine;
using Crest;

public class BoatControlBiased : BoatControl
{
    [Tooltip("Used to automatically add throttle input"), SerializeField]
    float _throttle = 0;

    [Tooltip("Used to automatically add turning input"), SerializeField]
    float _steer = 0;

    void Update() => Input = new Vector3(_steer, 0, _throttle);
}
