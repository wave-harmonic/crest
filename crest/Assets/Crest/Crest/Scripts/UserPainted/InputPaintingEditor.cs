// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
#endif

namespace Crest
{
#if UNITY_EDITOR
    public class PaintableEditorBase : ValidatedEditor
    {
        public static float s_paintRadius = 5f;
        public static float s_paintStrength = 1f;
    }

    public class PaintableEditor : PaintableEditorBase
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target is IPaintable)
            {
                OnInspectorGUIPainting(target as IPaintable);
            }
        }

        void OnInspectorGUIPainting(IPaintable target)
        {
            EditorGUILayout.Space();

            if (InputPaintingEditorTool.CurrentlyPainting)
            {
                if (GUILayout.Button("Stop Painting"))
                {
                    ToolManager.RestorePreviousPersistentTool();

                    if (_dirtyFlag)
                    {
                        // This causes a big hitch it seems, so only do it when stop painting. However do we also need to detect selection changes? And other events like quitting?
                        UnityEngine.Profiling.Profiler.BeginSample("Crest:PaintedInputEditor.OnInspectorGUI.SetDirty");
                        var obj = target as UnityEngine.Object;
                        if (obj)
                        {
                            EditorUtility.SetDirty(obj);
                        }
                        UnityEngine.Profiling.Profiler.EndSample();

                        _dirtyFlag = false;
                    }
                }

                s_paintRadius = EditorGUILayout.Slider("Brush Radius", s_paintRadius, 0f, 100f);
                s_paintStrength = EditorGUILayout.Slider("Brush Strength", s_paintStrength, 0f, 3f);
            }
            else
            {
                if (GUILayout.Button("Start Painting"))
                {
                    ToolManager.SetActiveTool<InputPaintingEditorTool>();
                }
            }

            if (GUILayout.Button("Clear"))
            {
                target.ClearData();
            }
        }

        Transform _cursor;

        bool _dirtyFlag = false;

        protected virtual void OnEnable()
        {
            _cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            _cursor.gameObject.hideFlags = HideFlags.HideAndDontSave;
            _cursor.GetComponent<Renderer>().material = new Material(Shader.Find("Crest/PaintCursor"));
        }

        protected virtual void OnDestroy()
        {
            UnityEngine.Profiling.Profiler.BeginSample("Crest:PaintedInputEditor.OnDestroy");

            if (_dirtyFlag)
            {
                EditorUtility.SetDirty(target);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        protected virtual void OnDisable()
        {
            DestroyImmediate(_cursor.gameObject);
        }

        protected virtual void OnSceneGUI()
        {
            if (ToolManager.activeToolType != typeof(InputPaintingEditorTool))
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

            var target = this.target as IPaintable;
            if (target == null) return;

            if (!WorldPosFromMouse(Event.current.mousePosition, out Vector3 pt))
            {
                return;
            }

            _cursor.position = pt;
            _cursor.localScale = new Vector3(2f, 0.25f, 2f) * s_paintRadius;

            if (dragging && WorldPosFromMouse(Event.current.mousePosition - Event.current.delta, out Vector3 ptLast))
            {
                Vector2 dir;
                dir.x = pt.x - ptLast.x;
                dir.y = pt.z - ptLast.z;
                dir.Normalize();

                if (target.Paint(pt, dir, weightMultiplier, Event.current.shift))
                {
                    _dirtyFlag = true;
                }
            }
        }
    }

    [EditorTool("Crest Input Painting", typeof(IPaintable))]
    public class InputPaintingEditorTool : EditorTool
    {
        public override GUIContent toolbarIcon => _toolbarIcon ??
            (_toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PaintedWaves.png"), "Crest Input Painting"));

        GUIContent _toolbarIcon;

        public static bool CurrentlyPainting => ToolManager.activeToolType == typeof(InputPaintingEditorTool);
    }
#endif // UNITY_EDITOR
}