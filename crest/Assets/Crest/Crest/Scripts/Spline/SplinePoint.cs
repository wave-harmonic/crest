// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest.Spline
{
    [ExecuteAlways]
    public class SplinePoint : MonoBehaviour
    {
#if UNITY_EDITOR
        Spline _spline;

        void Awake()
        {
            _spline = transform.parent != null ? transform.parent.GetComponent<Spline>() : null;
        }

        void OnDrawGizmos()
        {
            if (Selection.activeGameObject == gameObject)
            {
                var messageReceivers = GetComponentsInParent<IReceiveSplinePointOnDrawGizmosSelectedMessages>();
                foreach (var rec in messageReceivers)
                {
                    rec.OnSplinePointDrawGizmosSelected(this);
                }

                if (_spline == null) _spline = transform.parent.GetComponent<Spline>();
                if (_spline != null)
                {
                    _spline.OnDrawGizmosSelected();
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.position, 1f);
            }
            else
            {
                Gizmos.color = Color.black * 0.5f;
                Gizmos.DrawSphere(transform.position, 1f);
            }
            Gizmos.color = Color.white;
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
                var newPoint = CreateNewSP();
                newPoint.transform.parent = parent;
                var newIdx = thisIdx;
                newPoint.transform.SetSiblingIndex(newIdx);

                if (thisIdx > 0)
                {
                    var beforeNewPoint = parent.GetChild(thisIdx - 1);
                    newPoint.transform.position = Vector3.Lerp(beforeNewPoint.position, thisSP.transform.position, 0.5f);
                }
                else if (parent.childCount > 2)
                {
                    newPoint.transform.position = 2f * parent.GetChild(newIdx + 1).position - parent.GetChild(newIdx + 2).position;
                }

                Selection.activeObject = newPoint;
            }

            label = (thisIdx == parent.childCount - 1 || parent.childCount == 0) ? "Add after (extend)" : "Add after";
            if (GUILayout.Button(label))
            {
                Selection.activeObject = AddSplinePointAfter(parent, thisIdx);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Delete"))
            {
                if (thisIdx > 0)
                {
                    Selection.activeObject = parent.GetChild(thisIdx - 1);
                }
                else if (parent.childCount > 1)
                {
                    Selection.activeObject = parent.GetChild(1);
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

        public static GameObject AddSplinePointAfter(Transform parent, int afterIdx = -1)
        {
            if (afterIdx == -1) afterIdx = parent.childCount - 1;

            var newPoint = CreateNewSP();
            newPoint.transform.parent = parent;

            var newIdx = afterIdx + 1;
            newPoint.transform.SetSiblingIndex(newIdx);

            if (parent.childCount == 1)
            {
                newPoint.transform.localPosition = Vector3.zero;
            }
            else if (parent.childCount == 2)
            {
                newPoint.transform.position = parent.GetChild(afterIdx).position + 10f * Vector3.forward;
            }
            else if (newIdx < parent.childCount - 1)
            {
                var beforeNewPoint = parent.GetChild(afterIdx);
                var afterNewPoint = parent.GetChild(afterIdx + 2);
                newPoint.transform.position = Vector3.Lerp(beforeNewPoint.position, afterNewPoint.position, 0.5f);
            }
            else if (parent.childCount > 2)
            {
                newPoint.transform.position = 2f * parent.GetChild(newIdx - 1).position - parent.GetChild(newIdx - 2).position;
            }

            return newPoint;
        }
    }
#endif
}
