// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest.Spline
{
    /// <summary>
    /// Spline point, intended to be child of Spline object
    /// </summary>
    [ExecuteAlways]
    public class SplinePoint : MonoBehaviour
    {
#if UNITY_EDITOR
        void Awake()
        {
            EditorHelpers.EditorHelpers.SetGizmoIconEnabled(typeof(SplinePoint), false);
        }

        void OnDrawGizmos()
        {
            // We could not get gizmos or handles to work well when 3D Icons is enabled. problems included
            // them being almost invisible when occluded, or hard to select. DrawIcon is almost perfect,
            // but is very faint when occluded, but drawing it 8 times makes it easier to see.. sigh..
            var iconName = "d_Animation.Record@2x";
            Gizmos.DrawIcon(transform.position, iconName, true);
            Gizmos.DrawIcon(transform.position, iconName, true);
            Gizmos.DrawIcon(transform.position, iconName, true);
            Gizmos.DrawIcon(transform.position, iconName, true);
            Gizmos.DrawIcon(transform.position, iconName, true);
            Gizmos.DrawIcon(transform.position, iconName, true);
            Gizmos.DrawIcon(transform.position, iconName, true);
            Gizmos.DrawIcon(transform.position, iconName, true);
        }

        void OnDrawGizmosSelected()
        {
            if (transform.parent.TryGetComponent<IReceiveSplinePointOnDrawGizmosSelectedMessages>(out var receiver))
            {
                receiver.OnSplinePointDrawGizmosSelected(this);
            }
        }
#endif
    }

#if UNITY_EDITOR
    public interface IReceiveSplinePointOnDrawGizmosSelectedMessages
    {
        void OnSplinePointDrawGizmosSelected(SplinePoint point);
    }

    [CustomEditor(typeof(SplinePoint))]
    public class SplinePointEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var thisSP = target as SplinePoint;
            var thisIdx = thisSP.transform.GetSiblingIndex();

            var parent = thisSP.transform.parent;
            if (parent == null || parent.GetComponent<Spline>() == null)
            {
                EditorGUILayout.HelpBox("Spline component must be present on parent of this GameObject.", MessageType.Error);
                return;
            }

            GUILayout.Label("Selection", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (thisIdx == 0) GUI.enabled = false;
            if (GUILayout.Button("Select previous"))
            {
                Selection.activeObject = parent.GetChild(thisIdx - 1).gameObject;
            }
            GUI.enabled = true;
            if (thisIdx == parent.childCount - 1) GUI.enabled = false;
            if (GUILayout.Button("Select next"))
            {
                Selection.activeObject = parent.GetChild(thisIdx + 1).gameObject;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Select spline"))
            {
                Selection.activeObject = parent.gameObject;
            }

            GUILayout.Label("Spline actions", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            string label;

            label = thisIdx == 0 ? "Add before (extend)" : "Add before";
            if (GUILayout.Button(label))
            {
                var newPoint = AddSplinePointBefore(parent, thisIdx);

                Undo.RegisterCreatedObjectUndo(newPoint, "Add Crest Spline Point");

                Selection.activeObject = newPoint;
            }

            label = (thisIdx == parent.childCount - 1 || parent.childCount == 0) ? "Add after (extend)" : "Add after";
            if (GUILayout.Button(label))
            {
                var newPoint = AddSplinePointAfter(parent, thisIdx);

                Undo.RegisterCreatedObjectUndo(newPoint, "Add Crest Spline Point");

                Selection.activeObject = newPoint;
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Delete"))
            {
                if (thisIdx > 0)
                {
                    Selection.activeObject = parent.GetChild(thisIdx - 1);
                }
                else
                {
                    // If there is more than one child, select the first
                    if (parent.childCount > 1)
                    {
                        Selection.activeObject = parent.GetChild(1);
                    }
                    else
                    {
                        // No children - select the parent
                        Selection.activeObject = parent;
                    }
                }
                DestroyImmediate(thisSP.gameObject);
            }
        }

        static GameObject CreateNewSP()
        {
            var newPoint = new GameObject();
            newPoint.name = "SplinePoint";
            newPoint.AddComponent<SplinePoint>();
            return newPoint;
        }

        public static GameObject AddSplinePointBefore(Transform parent, int beforeIdx = 0)
        {
            var newPoint = CreateNewSP();
            newPoint.transform.parent = parent;

            // Put in front of child at beforeIdx
            newPoint.transform.SetSiblingIndex(beforeIdx);
            // Inserting has moved the before point forwards, update its index to simplify the below
            beforeIdx++;

            if (parent.childCount == 1)
            {
                // New point is sole point, place at center
                newPoint.transform.localPosition = Vector3.zero;
            }
            else if (parent.childCount == 2)
            {
                // New point has one sibling, place nearby it
                newPoint.transform.position = parent.GetChild(beforeIdx).position - 10f * Vector3.forward;
            }
            else if (beforeIdx > 1)
            {
                // New point being inserted between two existing points, bisect them
                var beforeNewPoint = parent.GetChild(beforeIdx);
                var afterNewPoint = parent.GetChild(beforeIdx - 2);
                newPoint.transform.position = Vector3.Lerp(beforeNewPoint.position, afterNewPoint.position, 0.5f);
            }
            else
            {
                // New point being inserted before first point, and spline has multiple points, extrapolate backwards
                var newPos = 2f * parent.GetChild(1).position - parent.GetChild(2).position;
                Debug.Log(newPos);
                newPoint.transform.position = newPos;
            }

            return newPoint;
        }

        public static GameObject AddSplinePointAfter(Transform parent, int afterIdx = -1)
        {
            // If no index specified, assume adding after last point
            if (afterIdx == -1) afterIdx = parent.childCount - 1;

            var newPoint = CreateNewSP();
            newPoint.transform.parent = parent;

            var newIdx = afterIdx + 1;
            newPoint.transform.SetSiblingIndex(newIdx);

            if (parent.childCount == 1)
            {
                // New point is sole point, place at center
                newPoint.transform.localPosition = Vector3.zero;
            }
            else if (parent.childCount == 2)
            {
                // New point has one sibling, place nearby it
                newPoint.transform.position = parent.GetChild(afterIdx).position + 10f * Vector3.forward;
            }
            else if (newIdx < parent.childCount - 1)
            {
                // New point being inserted between two existing points, bisect them
                var beforeNewPoint = parent.GetChild(afterIdx);
                var afterNewPoint = parent.GetChild(afterIdx + 2);
                newPoint.transform.position = Vector3.Lerp(beforeNewPoint.position, afterNewPoint.position, 0.5f);
            }
            else
            {
                // New point being added after last point, and spline has multiple points, extrapolate forwards
                newPoint.transform.position = 2f * parent.GetChild(newIdx - 1).position - parent.GetChild(newIdx - 2).position;
            }

            return newPoint;
        }
    }
#endif
}
