using UnityEngine;
using Crest;

public sealed class BoatControlPlayer : BoatControl
{
    [Tooltip("The input axis name for steering."), SerializeField]
    string _steerInputAxisName = "Horizontal";

    [Tooltip("The input axis name for throttle."), SerializeField]
    string _throttleInputAxisName = "Vertical";

    void Update()
    {
        // The boat will read this input the next frame in FixedUpdate (one frame latency).
        var throttle = UnityEngine.Input.GetAxis(_throttleInputAxisName);
        var steer = UnityEngine.Input.GetAxis(_steerInputAxisName);
        if (throttle < 0f) steer *= -1f;
        Input = new Vector3(steer, 0, throttle);
    }
}
