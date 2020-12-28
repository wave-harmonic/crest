using UnityEditor;
using UnityEngine;

namespace Crest.Spline
{
    [ExecuteAlways]
    public partial class Spline : MonoBehaviour
    {
        public SplinePoint[] SplinePoints => GetComponentsInChildren<SplinePoint>();

#if UNITY_EDITOR
        void Update()
        {
            // Would be ideal to hash and only generate on change
            //Generate();
        }
#endif

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white * 0.65f;
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Gizmos.DrawWireMesh(mf.sharedMesh, 0, transform.position, transform.rotation, transform.lossyScale);
            }

            Gizmos.color = Color.black * 0.5f;
            var points = SplinePoints;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Gizmos.DrawLine(points[i].transform.position, points[i + 1].transform.position);
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

            if (GUILayout.Button("Add point (extend)"))
            {
                SplinePointEditor.AddSplinePointAfter((target as Spline).transform);
            }
        }
    }
#endif
}
