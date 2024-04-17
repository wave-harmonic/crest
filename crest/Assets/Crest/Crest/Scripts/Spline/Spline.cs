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
    /// Simple spline object. Spline points are child GameObjects.
    /// </summary>
    [ExecuteDuringEditMode]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SPLINE + "Spline")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "water-inputs.html" + Internal.Constants.HELP_URL_RP + "#spline-mode")]
    public partial class Spline : CustomMonoBehaviour, ISplinePointCustomDataSetup
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 1;
#pragma warning restore 414

        public enum Offset
        {
            Left,
            Center,
            Right
        }
        [Tooltip("Where generated ribbon should lie relative to spline. If set to Center, ribbon is centered around spline.")]
        [OnChange(nameof(UpdateSpline)), DecoratedField]
        public Offset _offset = Offset.Center;

        [Tooltip("Connect start and end point to close spline into a loop. Requires at least 3 spline points.")]
        [OnChange(nameof(UpdateSpline)), DecoratedField]
        public bool _closed = false;

        [SerializeField, DecoratedField, OnChange(nameof(UpdateSpline))]
        float _radius = 20f;

        [SerializeField, Delayed, OnChange(nameof(UpdateSpline))]
        int _subdivisions = 1;

        public float Radius { get => _radius; set => _radius = value; }
        public int Subdivisions { get => _subdivisions; set => _subdivisions = value; }

        public void UpdateSpline()
        {
            foreach (var receiver in transform.GetComponents<IReceiveSplineChangeMessages>())
            {
                receiver.OnSplineChange();
            }
        }

        public bool AttachDataToSplinePoint(GameObject splinePoint)
        {
            if (splinePoint.TryGetComponent<SplinePointData>(out _))
            {
                // Already added, nothing to do
                return false;
            }

            splinePoint.AddComponent<SplinePointData>();
            return true;
        }

#if CREST_UNITY_SPLINES
        void OnEnable()
        {
            UnityEngine.Splines.Spline.Changed -= OnSplineChanged;
            UnityEngine.Splines.Spline.Changed += OnSplineChanged;
        }

        void OnDisable()
        {
            UnityEngine.Splines.Spline.Changed -= OnSplineChanged;
        }

        void OnSplineChanged(UnityEngine.Splines.Spline spline, int index, UnityEngine.Splines.SplineModification modification)
        {
            if (TryGetComponent<UnityEngine.Splines.SplineContainer>(out var container) && container.Spline == spline)
            {
                UpdateSpline();
            }
        }
#endif
    }

    // Version handling - perform data migration after data loaded.
    public partial class Spline : ISerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
            // Intentionally left empty.
        }

        public void OnAfterDeserialize()
        {
            // Version 1 (2021.10.02)
            // - Alignment added with default value different than old behaviour
            if (_version == 0)
            {
                // Set alignment to Right to maintain old behaviour
                _offset = Offset.Right;

                _version = 1;
            }
        }
    }

#if UNITY_EDITOR
    public partial class Spline : IValidated
    {
        public void OnDrawGizmos()
        {
            var points = GetComponentsInChildren<SplinePoint>(includeInactive: false);
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

#if CREST_UNITY_SPLINES
            // Skip validation as there may be temporary hidden objects in the hierarchy.
            if (TryGetComponent<UnityEngine.Splines.SplineInstantiate>(out var _))
            {
                return isValid;
            }
#endif

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
    public class SplineEditor : CustomBaseEditor
    {
#if CREST_UNITY_SPLINES
        int _instantiatedSplinePointCount;
#endif

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var targetSpline = target as Spline;

#if CREST_UNITY_SPLINES
            var points = targetSpline.GetComponentsInChildren<SplinePoint>(includeInactive: false);
            var hasInstantiate = targetSpline.TryGetComponent<UnityEngine.Splines.SplineInstantiate>(out var instantiate);
            var hasContainer = targetSpline.TryGetComponent<UnityEngine.Splines.SplineContainer>(out var container);

            if (!hasInstantiate)
#endif
            {
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

#if CREST_UNITY_SPLINES
            if (hasInstantiate)
            {
                if (_instantiatedSplinePointCount != points.Length)
                {
                    targetSpline.UpdateSpline();
                    _instantiatedSplinePointCount = points.Length;
                }

                // Enable rich text in help boxes. Store original so we can revert since this might be a "hack".
                var styleRichText = GUI.skin.GetStyle("HelpBox").richText;
                GUI.skin.GetStyle("HelpBox").richText = true;

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Spline is being controlled by <i>Spline Instantiate</i>. If you want to change data on the points, click <i>Bake Instances</i>.", MessageType.Info);

                // Revert skin since it persists.
                GUI.skin.GetStyle("HelpBox").richText = styleRichText;
            }
            else
            {
                EditorGUILayout.Space();

                // Destroy instantiate temporary.
                {
                    var temporary = targetSpline.transform.Find("Crest Temporary");
                    if (temporary != null) Helpers.Destroy(temporary.gameObject);
                }

                // Generation buttons.
                if (points.Length == 0)
                {
                    if (GUILayout.Button("Generate From Unity Spline"))
                    {
                        var temporary = new GameObject
                        {
                            name = "Crest Temporary",
                            hideFlags = HideFlags.HideInHierarchy,
                        };

                        temporary.SetActive(false);
                        temporary.transform.SetParent(targetSpline.transform, worldPositionStays: true);

                        var go = new GameObject
                        {
                            name = "Spline Point",
                            hideFlags = HideFlags.HideInHierarchy,
                        };

                        go.transform.parent = targetSpline.transform;
                        go.transform.SetParent(temporary.transform, worldPositionStays: true);

                        go.AddComponent<SplinePoint>();
                        go.AddComponent<SplinePointData>();

                        instantiate = Undo.AddComponent<UnityEngine.Splines.SplineInstantiate>(targetSpline.gameObject);
                        instantiate.itemsToInstantiate = new UnityEngine.Splines.SplineInstantiate.InstantiableItem[]
                        {
                            new() { Prefab = go },
                        };

                        instantiate.InstantiateMethod = UnityEngine.Splines.SplineInstantiate.Method.SpacingDistance;
                        instantiate.MinSpacing = 5;
                        instantiate.MaxSpacing = 5;

                        Undo.RegisterCreatedObjectUndo(temporary, "Generate Spline");
                        Undo.RegisterCreatedObjectUndo(go, "Generate Spline");
                    }

                    if (hasContainer && GUILayout.Button("Generate From Unity Spline Knots"))
                    {
                        foreach (var point in container.Spline)
                        {
                            var go = new GameObject();
                            go.name = "Spline Point";
                            go.transform.parent = targetSpline.transform;
                            go.transform.position = container.transform.TransformPoint(point.Position);
                            go.AddComponent<SplinePoint>();
                            go.AddComponent<SplinePointData>();
                            Undo.RegisterCreatedObjectUndo(go, "Generate Spline From Knots");
                        }
                    }
                }

            }
#endif // CREST_UNITY_SPLINES

            // Helpers to quickly attach ocean inputs
            EditorGUILayout.Space();
            GUILayout.Label("Add Feature", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            FeatureButton<RegisterHeightInput>("Set Height", targetSpline.gameObject);
            FeatureButton<RegisterFlowInput>("Add Flow", targetSpline.gameObject);
            FeatureButton<ShapeFFT>("Add Waves", targetSpline.gameObject);
            GUILayout.EndHorizontal();
        }

        public static void ExtendSpline(Spline spline)
        {
            var newPoint = SplinePointEditor.AddSplinePointAfter(spline.transform);

            Undo.RegisterCreatedObjectUndo(newPoint, "Add Crest Spline Point");
        }

        static void FeatureButton<ComponentType>(string label, GameObject go) where ComponentType : Component
        {
            if (!go.TryGetComponent<ComponentType>(out _))
            {
                if (GUILayout.Button(label))
                {
                    Undo.AddComponent<ComponentType>(go);
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button(label);
                GUI.enabled = true;
            }
        }
    }
#endif
}
