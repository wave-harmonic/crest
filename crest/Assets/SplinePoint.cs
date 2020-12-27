using UnityEditor;
using UnityEngine;

namespace Crest.Spline
{
    [ExecuteAlways]
    public class SplinePoint : MonoBehaviour
    {
        Spline _spline;

        void Awake()
        {
            _spline = transform.parent != null ? transform.parent.GetComponent<Spline>() : null;
        }

        void OnDrawGizmosSelected()
        {
            if (_spline != null)
            {
                _spline.OnDrawGizmosSelected();
            }
        }
    }

#if UNITY_EDITOR
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
            if (GUILayout.Button("Add before"))
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
            if (GUILayout.Button("Add after"))
            {
                var newPoint = CreateNewSP();
                newPoint.transform.parent = parent;
                var newIdx = thisIdx + 1;
                newPoint.transform.SetSiblingIndex(newIdx);

                if (newIdx < parent.childCount - 1)
                {
                    var afterNewPoint = parent.GetChild(thisIdx + 2);
                    newPoint.transform.position = Vector3.Lerp(afterNewPoint.position, thisSP.transform.position, 0.5f);
                }
                else if (parent.childCount > 2)
                {
                    newPoint.transform.position = 2f * parent.GetChild(newIdx - 1).position - parent.GetChild(newIdx - 2).position;
                }

                Selection.activeObject = newPoint;
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

        GameObject CreateNewSP()
        {
            var newPoint = new GameObject();
            newPoint.name = "SplinePoint";
            newPoint.AddComponent<SplinePoint>();
            return newPoint;
        }
    }
#endif
}
