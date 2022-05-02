// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    // Interface that this component uses to find clients and determine their data requirements
    public interface IPaintedDataClient
    {
        GraphicsFormat GraphicsFormat { get; }

        void ClearData();

        bool Paint(Vector3 paintPosition3, Vector2 paintDir, float paintWeight, bool remove);

        CPUTexture2DBase Texture { get; }

        Vector2 WorldSize { get; }
        float PaintRadius { get; }

        Transform Transform { get; }
    }

    // TODO this is now merely just a paint support component. Perhaps it shoudl be added automatically. Maybe it shoudl not show in inspector.

    // TODO - maybe rename? UserDataPainted and UserDataSpline would have been a systematic naming. However not sure
    // if this is user friendly, and not sure if it makes sense if we dont rename the Spline.
    // TODO - this component has no Enabled checkbox because enabling/disabling would need handling in terms of updating the
    // material keywords, and I'm unsure how best for this communication to happen
    // Made separate component as it matches the spline input (somewhat) and also it gives all the editor functionality below which may be painful
    // to apply to all our component types? Assuming it stays this way, then we need a way for it to be query the required data type and any
    // behaviour modifications.
    [ExecuteAlways]
    public class UserDataPainted : MonoBehaviour
    {
        [Header("Paint Settings")]
        [Range(0f, 1f)]
        public float _brushStrength = 0.75f;

        [Range(0.25f, 100f, 5f)]
        public float _brushRadius = 5f;

        [Range(1f, 100f, 5f)]
        public float _brushHardness = 1f;

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            var client = GetComponent<IPaintedDataClient>();
            if (client == null) return;

            Gizmos.matrix = Matrix4x4.Translate(transform.position) * Matrix4x4.Scale(new Vector3(client.WorldSize.x, 1f, client.WorldSize.y));
            Gizmos.color = WavePaintingEditorTool.CurrentlyPainting ? new Color(1f, 0f, 0f, 0.5f) : new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0f, 1f));
        }
#endif
    }

#if UNITY_EDITOR
    // This typeof means it gets activated for the above component
    [EditorTool("Crest Wave Painting", typeof(UserDataPainted))]
    class WavePaintingEditorTool : EditorTool
    {
        UserDataPainted _waves;

        public override GUIContent toolbarIcon => _toolbarIcon ??
            (_toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PaintedWaves.png"), "Crest Wave Painting"));

        public static bool CurrentlyPainting => ToolManager.activeToolType == typeof(WavePaintingEditorTool);

        GUIContent _toolbarIcon;

        private void OnEnable()
        {
            ToolManager.activeToolChanged += ActiveToolDidChange;
        }
        private void OnDisable()
        {
            ToolManager.activeToolChanged -= ActiveToolDidChange;
        }

        void ActiveToolDidChange()
        {
            if (!ToolManager.IsActiveTool(this))
                return;

            _waves = target as UserDataPainted;
        }
    }

    // Additively blend mouse motion vector onto RG16F. Vector size < 1 used as wave weight.
    // Weight could also ramp up when motion vector confidence is low. Motion vector could lerp towards
    // current delta each frame.
    [CustomEditor(typeof(UserDataPainted))]
    class PaintedInputEditor : Editor
    {
        Transform _cursor;

        bool _dirtyFlag = false;

        private void OnEnable()
        {
            _cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            _cursor.gameObject.hideFlags = HideFlags.HideAndDontSave;
            _cursor.GetComponent<Renderer>().material = new Material(Shader.Find("Crest/PaintCursor"));
        }

        private void OnDestroy()
        {
            UnityEngine.Profiling.Profiler.BeginSample("Crest:PaintedInputEditor.OnDestroy");

            if (_dirtyFlag)
            {
                EditorUtility.SetDirty(target);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void ClearData()
        {
            (target as UserDataPainted)?.GetComponent<IPaintedDataClient>()?.ClearData();
        }

        private void OnDisable()
        {
            DestroyImmediate(_cursor.gameObject);
        }

        private void OnSceneGUI()
        {
            if (ToolManager.activeToolType != typeof(WavePaintingEditorTool))
            {
                return;
            }

            switch (Event.current.type)
            {
                case EventType.MouseMove:
                    OnMouseMove(false);
                    break;
                case EventType.MouseDown:
                    // Boost strength of mouse down, feels much better when clicking
                    OnMouseMove(Event.current.button == 0, 3f);
                    break;
                case EventType.MouseDrag:
                    OnMouseMove(Event.current.button == 0);
                    break;
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                    break;
            }
        }

        bool WorldPosFromMouse(Vector2 mousePos, out Vector3 pos)
        {
            var r = HandleUtility.GUIPointToWorldRay(mousePos);

            var heightOffset = r.origin.y - OceanRenderer.Instance.transform.position.y;
            var diry = r.direction.y;
            if (heightOffset * diry >= 0f)
            {
                // Ray going away from ocean plane
                pos = Vector3.zero;
                return false;
            }

            var dist = -heightOffset / diry;
            pos = r.GetPoint(dist);
            return true;
        }

        void OnMouseMove(bool dragging, float weightMultiplier = 1f)
        {
            if (!OceanRenderer.Instance) return;

            var waves = target as UserDataPainted;

            if (!WorldPosFromMouse(Event.current.mousePosition, out Vector3 pt))
            {
                return;
            }

            var client = waves.GetComponent<IPaintedDataClient>();
            if (client == null)
            {
                return;
            }

            _cursor.position = pt;
            _cursor.localScale = new Vector3(2f, 0.25f, 2f) * client.PaintRadius;

            if (dragging && WorldPosFromMouse(Event.current.mousePosition - Event.current.delta, out Vector3 ptLast))
            {
                Vector2 dir;
                dir.x = pt.x - ptLast.x;
                dir.y = pt.z - ptLast.z;
                dir.Normalize();

                if (client.Paint(pt, dir, weightMultiplier, Event.current.shift))
                {
                    _dirtyFlag = true;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (WavePaintingEditorTool.CurrentlyPainting)
            {
                if (GUILayout.Button("Stop Painting"))
                {
                    ToolManager.RestorePreviousPersistentTool();

                    if (_dirtyFlag)
                    {
                        var waves = target as UserDataPainted;
                        var client = waves?.GetComponent<IPaintedDataClient>();
                        if (client == null)
                        {
                            return;
                        }

                        // This causes a big hitch it seems, so only do it when stop painting. However do we also need to detect selection changes? And other events like quitting?
                        UnityEngine.Profiling.Profiler.BeginSample("Crest:PaintedInputEditor.OnInspectorGUI.SetDirty");
                        EditorUtility.SetDirty(client as Component);
                        UnityEngine.Profiling.Profiler.EndSample();

                        _dirtyFlag = false;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Start Painting"))
                {
                    ToolManager.SetActiveTool<WavePaintingEditorTool>();
                }
            }

            if (GUILayout.Button("Clear"))
            {
                ClearData();
            }
        }
    }
#endif
}
