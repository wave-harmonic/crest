// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest.Spline
{
    [ExecuteAlways]
    public partial class Spline : MonoBehaviour
    {
        public bool _closed = false;

        public SplinePoint[] SplinePoints => GetComponentsInChildren<SplinePoint>();

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.black * 0.5f;
            var points = SplinePoints;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Gizmos.DrawLine(points[i].transform.position, points[i + 1].transform.position);
            }

            {
                var splinePoints = SplinePoints;
                if (splinePoints.Length < 2) return;

                var splinePointCount = splinePoints.Length;
                if (_closed && splinePointCount > 2)
                {
                    splinePointCount++;
                }

                var hullPoints = new Vector3[(splinePointCount - 1) * 3 + 1];

                if (!SplineInterpolation.GenerateCubicSplineHull(splinePoints, hullPoints, _closed))
                {
                    return;
                }

                foreach(var pt in hullPoints)
                {
                    Debug.DrawLine(pt - 10f * Vector3.up, pt + 10f * Vector3.up);
                }

            }

            Gizmos.color = Color.white;
        }
    }

#if UNITY_EDITOR
    public partial class Spline : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            var points = SplinePoints;

            if (points.Length < 2)
            {
                showMessage
                (
                    "Spline must have at least 2 spline points. Click the <i>Add point</i> button in the Inspector, or add a child GameObject and attach <i>SplinePoint</i> component to it.",
                    ValidatedHelper.MessageType.Error, this
                );

                isValid = false;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<SplinePoint>() == null)
                {
                    showMessage
                    (
                        $"All child GameObjects under <i>Spline</i> must have <i>SplinePoint</i> component added. Object <i>{transform.GetChild(i).gameObject.name}</i> does not and should have one added, or be moved out of the hierarchy.",
                        ValidatedHelper.MessageType.Error, this
                    );

                }
            }

            return isValid;
        }
    }

    [CustomEditor(typeof(Spline))]
    public class SplineEditor : ValidatedEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var targetSpline = target as Spline;

            if (GUILayout.Button("Add point (extend)"))
            {
                SplinePointEditor.AddSplinePointAfter(targetSpline.transform);
            }

            GUILayout.BeginHorizontal();
            var pointCount = targetSpline.transform.childCount;
            GUI.enabled = pointCount > 0;
            if (GUILayout.Button("Select first point"))
            {
                Selection.activeGameObject = targetSpline.transform.GetChild(0).gameObject;
            }
            if (GUILayout.Button("Select last point"))
            {
                Selection.activeGameObject = targetSpline.transform.GetChild(pointCount - 1).gameObject;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
    }
#endif
}
