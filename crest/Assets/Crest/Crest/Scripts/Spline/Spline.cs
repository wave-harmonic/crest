// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest.Spline
{
    public interface ISplinePointCustomDataSetup
    {
        bool AttachDataToSplinePoint(GameObject splinePoint);
    }

    /// <summary>
    /// Simple spline object. Spline points are child gameobjects.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SPLINE + "Spline")]
    [HelpURL("https://crest.readthedocs.io/en/latest/user/wave-conditions.html#wave-splines-preview")]
    public partial class Spline : MonoBehaviour
    {
        [Tooltip("Connect start and end point to close spline into a loop. Requires at least 3 spline points.")]
        public bool _closed = false;

        [SerializeField]
        float _radius = 20f;
        [SerializeField, Delayed]
        int _subdivisions = 1;
        [SerializeField, Delayed]
        int _smoothingIterations = 0;

        public float Radius => _radius;
        public int Subdivisions => _subdivisions;
        public int SmoothingIterations => _smoothingIterations;
    }

#if UNITY_EDITOR
    public partial class Spline : IValidated
    {
        public void OnDrawGizmos()
        {
            var points = GetComponentsInChildren<SplinePoint>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                SetLineColor(points[i], points[i + 1], false);
                Gizmos.DrawLine(points[i].transform.position, points[i + 1].transform.position);
            }

            if (_closed && points.Length > 2)
            {
                SetLineColor(points[points.Length - 1], points[0], true);
                Gizmos.DrawLine(points[points.Length - 1].transform.position, points[0].transform.position);
            }

            Gizmos.color = Color.white;
        }

        void SetLineColor(SplinePoint from, SplinePoint to, bool isClosing)
        {
            Gizmos.color = isClosing ? Color.white : Color.black * 0.5f;

            if (Selection.activeObject == from.gameObject || Selection.activeObject == to.gameObject)
            {
                Gizmos.color = Color.yellow;
            }
        }

        void FixAddSplinePoints(SerializedObject splineComponent)
        {
            var spline = splineComponent.targetObject as Spline;
            var requiredPoints = spline._closed ? 3 : 2;
            var needToAdd = requiredPoints - spline.GetComponentsInChildren<SplinePoint>().Length;

            for (var i = 0; i < needToAdd; i++)
            {
                SplineEditor.ExtendSpline(spline);
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
                        $"All child GameObjects under <i>Spline</i> must have <i>SplinePoint</i> component added. Object <i>{transform.GetChild(i).gameObject.name}</i> does not have one.",
                        $"Add a <i>SplinePoint</i> component to object {transform.GetChild(i).gameObject.name}, or move this object out in the hierarchy.",
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
                    "Spline must have at least 2 spline points.",
                    "Click the <i>Add Point</i> button in the Inspector, or add a child GameObject and attach <i>SplinePoint</i> component to it.",
                    ValidatedHelper.MessageType.Error, this,
                    FixAddSplinePoints
                );

                isValid = false;
            }
            else if (_closed && points.Length < 3)
            {
                showMessage
                (
                    "Closed splines must have at least 3 spline points. See the <i>Closed</i> parameter and tooltip.",
                    "Add a point by clicking the <i>Add Point</i> button in the Inspector.",
                    ValidatedHelper.MessageType.Error, this,
                    FixAddSplinePoints
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
                ExtendSpline(targetSpline);
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

        public static void ExtendSpline(Spline spline)
        {
            var newPoint = SplinePointEditor.AddSplinePointAfter(spline.transform);

            Undo.RegisterCreatedObjectUndo(newPoint, "Add Crest Spline Point");
        }
    }
#endif
}
