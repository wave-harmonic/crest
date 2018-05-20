// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

/// <summary>
/// Attach this script to any GameObject and it will create three collision probes in front of the camera
/// </summary>
public class OceanCollisionDemo : MonoBehaviour
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

        var rect = new Rect(query.x - 0.5f, query.z - 0.5f, 1f, 1f);
        int lod = OceanRenderer.SuggestCollisionLOD(rect);

        if (lod > -1)
        {
            var disp = Vector3.zero;
            if( OceanRenderer.Instance.Builder._shapeWDCs[lod].SampleDisplacement(ref query, ref disp))
            {
                Debug.DrawLine(query, query + disp);
                marker.transform.position = query + disp;
            }
        }
        else
        {
            marker.transform.position = query;
        }
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
