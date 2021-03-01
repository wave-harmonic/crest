// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest.Spline
{
    /// <summary>
    /// Simple spline object. Spline points are child gameobjects.
    /// </summary>
    [ExecuteAlways]
    public partial class Spline : MonoBehaviour
    {
        [Tooltip("Connect start and end point to close spline into a loop. Requires at least 3 spline points.")]
        public bool _closed = false;
    }

#if UNITY_EDITOR
    public partial class Spline : IValidated
    {
        void Awake()
        {
            EditorHelpers.EditorHelpers.SetGizmoIconEnabled(typeof(Spline), false);
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.black * 0.5f;
            var points = GetComponentsInChildren<SplinePoint>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                Gizmos.DrawLine(points[i].transform.position, points[i + 1].transform.position);
            }

            Gizmos.color = Color.white;

            if (_closed && points.Length > 2)
            {
                Gizmos.DrawLine(points[points.Length - 1].transform.position, points[0].transform.position);
            }
        }

        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            for (int i = 0; i < transform.childCount; i++)
            {
                if (!transform.GetChild(i).TryGetComponent<SplinePoint>(out _))
                {
                    showMessage
                    (
                        $"All child GameObjects under <i>Spline</i> must have <i>SplinePoint</i> component added. Object <i>{transform.GetChild(i).gameObject.name}</i> does not and should have one added, or be moved out of the hierarchy.",
                        ValidatedHelper.MessageType.Error, this
                    );

                    isValid = false;

                    // One error is enough probably - don't fill the Inspector with tons of errors
                    break;
                }
            }

            var points = GetComponentsInChildren<SplinePoint>();

            if (points.Length < 2)
            {
                showMessage
                (
                    "Spline must have at least 2 spline points. Click the <i>Add point</i> button in the Inspector, or add a child GameObject and attach <i>SplinePoint</i> component to it.",
                    ValidatedHelper.MessageType.Error, this
                );

                isValid = false;
            }
            else if (_closed && points.Length < 3)
            {
                showMessage
                (
                    "Closed splines must have at least 3 spline points. See the <i>Closed</i> parameter and tooltip. To add a point click the <i>Add point</i> button in the Inspector, or add a child GameObject and attach <i>SplinePoint</i> component to it.",
                    ValidatedHelper.MessageType.Error, this
                );

                isValid = false;
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
                var newPoint = SplinePointEditor.AddSplinePointAfter(targetSpline.transform);

                Undo.RegisterCreatedObjectUndo(newPoint, "Add Crest Spline Point");
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

            if (GUILayout.Button("Reverse"))
            {
                for (int i = 1; i < targetSpline.transform.childCount; i++)
                {
                    targetSpline.transform.GetChild(i).SetSiblingIndex(0);
                }
            }
        }
    }
#endif
}
