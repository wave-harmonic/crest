// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

/// <summary>
/// Attach this script to any GameObject and it will create three collision probes in front of the camera
/// </summary>
public class OceanSampleDisplacementDemo : MonoBehaviour
{
    public bool _trackCamera = true;

    GameObject _marker, _markerX, _markerZ;
    Vector3 _markerPos, _markerPosX, _markerPosZ;

    void PlaceMarkerCube(ref GameObject marker, Vector3 query)
    {
        if (marker == null)
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(marker.GetComponent<Collider>());
        }

        query.y = 0f;

        // If you are taking multiple samples over an area, setup up the collision sampling state first by calling
        // PrewarmForSamplingArea().

        // Assume a primitive like a sphere or box, providing this side length means high frequency waves
        // much shorter than the object will be ignored.
        float shapeLength = 2f * transform.lossyScale.magnitude;

        Vector3 disp;
        if (OceanRenderer.Instance.CollisionProvider.SampleDisplacement(ref query, out disp, shapeLength))
        {
            Debug.DrawLine(query, query + disp);
            marker.transform.position = query + disp;
        }
        else
        {
            marker.transform.position = query;
        }

        // if you are taking multiple samples over an area, setup up the collision sampling state first by calling
        // PrewarmForSamplingArea()
    }

    void Update()
    {
        if (_trackCamera) _markerPos = Camera.main.transform.position + Camera.main.transform.forward * 10f;
        if (_trackCamera) _markerPosX = Camera.main.transform.position + Camera.main.transform.forward * 10f + 5f * Vector3.right;
        if (_trackCamera) _markerPosZ = Camera.main.transform.position + Camera.main.transform.forward * 10f + 5f * Vector3.forward;

        PlaceMarkerCube(ref _marker, _markerPos);
        PlaceMarkerCube(ref _markerX, _markerPosX);
        PlaceMarkerCube(ref _markerZ, _markerPosZ);
    }
}
