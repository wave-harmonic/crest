// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

/// <summary>
/// Set this transform to ocean height
/// </summary>
public class OceanHeightDemo : MonoBehaviour
{
    public bool _extrapolateForwards = true;

    float _lastTime = -1f;
    float _lastHeight;

	void Update ()
    {
        float scale = transform.lossyScale.x;

        var rect = new Rect(transform.position.x - scale / 2f, transform.position.z - scale / 2f, scale, scale);
        int lod = OceanRenderer.SuggestCollisionLOD(rect);

        if (lod > -1)
        {
            var wdc = OceanRenderer.Instance.Builder._shapeWDCs[lod];

            var pos = transform.position;
            var height = wdc.GetHeightExpensive(ref pos);
            var time = wdc.GetCollisionTime();

            var targetHeight = height;

            if (_extrapolateForwards && _lastTime != -1f && (time - _lastTime) > Mathf.Epsilon)
            {
                float vel = (height - _lastHeight) / (time - _lastTime);
                targetHeight += (OceanRenderer.Instance.ElapsedTime - time) * vel;
            }

            transform.position += Vector3.up * (targetHeight - pos.y);

            _lastHeight = height;
            _lastTime = time;
        }
    }
}
