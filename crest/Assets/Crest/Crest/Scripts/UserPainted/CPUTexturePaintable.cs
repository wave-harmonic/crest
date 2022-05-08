// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
#endif

namespace Crest
{
    public interface IPaintedData
    {
        Texture2D Texture { get; }
        Vector2 WorldSize { get; }
    }

    public interface IPaintable
    {
        IPaintedData PaintedData { get; }
        Shader PaintedInputShader { get; }

        void ClearData();

        bool Paint(Vector3 paintPosition3, Vector2 paintDir, float paintWeight, bool remove);
    }

    public static class CPUTexturePaintHelpers
    {
        public static float PaintFnAlphaBlendFloat(float existingValue, float paintValue, float weight, bool remove)
        {
            return Mathf.Lerp(existingValue, paintValue, weight);
        }

        public static float PaintFnAdditiveBlendFloat(float existingValue, float paintValue, float weight, bool remove)
        {
            return existingValue + (remove ? -1f : 1f) * paintValue * weight;
        }

        public static float PaintFnAdditiveBlendSaturateFloat(float existingValue, float paintValue, float weight, bool remove)
        {
            return Mathf.Clamp01(existingValue + (remove ? -1f : 1f) * paintValue * weight);
        }

        public static Vector2 PaintFnAdditivePlusRemoveBlendVector2(Vector2 existingValue, Vector2 paintValue, float weight, bool remove)
        {
            if (remove)
            {
                return Vector2.MoveTowards(existingValue, Vector2.zero, weight);
            }
            else
            {
                return existingValue + paintValue * weight;
            }
        }

        public static Vector2 PaintFnAdditivePlusRemoveBlendSaturateVector2(Vector2 existingValue, Vector2 paintValue, float weight, bool remove)
        {
            if (remove)
            {
                return Vector2.MoveTowards(existingValue, Vector2.zero, weight);
            }
            else
            {
                var result = existingValue + paintValue * weight;

                // 'Saturate' - clamp length to 1. Oddly, this seems less predictable/more prone to warping, and perhaps more difficult to manage.
                // It may be wise to apply some kind of downward pressure to size though.
                //var len2 = result.sqrMagnitude;
                //if (len2 > 1f)
                //{
                //    result /= Mathf.Sqrt(len2);
                //}
                return result;
            }
        }
    }

    [Serializable]
    public class CPUTexture2DPaintable_R16_AddBlend : CPUTexture2DPaintable<float>
    {
        public override GraphicsFormat GraphicsFormat => GraphicsFormat.R16_SFloat;

        public bool Sample(Vector3 position3, ref float result)
        {
            return Sample(position3, CPUTexture2DHelpers.BilinearInterpolateFloat, ref result);
        }

        public bool PaintSmoothstep(Component owner, Vector3 paintPosition3, float paintRadius, float paintWeight, float paintValue, bool remove)
        {
            return PaintSmoothstep(owner, paintPosition3, paintRadius, paintWeight, paintValue, CPUTexturePaintHelpers.PaintFnAdditiveBlendFloat, remove);
        }
    }

    [Serializable]
    public class CPUTexture2DPaintable_RG16_AddBlend : CPUTexture2DPaintable<Vector2>
    {
        public override GraphicsFormat GraphicsFormat => GraphicsFormat.R16G16_SFloat;

        public bool Sample(Vector3 position3, ref Vector2 result)
        {
            return Sample(position3, CPUTexture2DHelpers.BilinearInterpolateVector2, ref result);
        }

        public bool PaintSmoothstep(Component owner, Vector3 paintPosition3, float paintRadius, float paintWeight, Vector2 paintValue, bool remove)
        {
            return PaintSmoothstep(owner, paintPosition3, paintRadius, paintWeight, paintValue, CPUTexturePaintHelpers.PaintFnAdditivePlusRemoveBlendVector2, remove);
        }
    }

    [Serializable]
    public abstract class CPUTexture2DPaintable<T> : CPUTexture2D<T>, IPaintedData
    {
        public void PrepareMaterial(Material mat, Func<T, Color> colorConstructFn)
        {
            mat.EnableKeyword("_PAINTED_ON");

            mat.SetVector("_PaintedWavesSize", WorldSize);
            mat.SetVector("_PaintedWavesPosition", CenterPosition);
            mat.SetTexture("_PaintedWavesData", GetGPUTexture(colorConstructFn));
        }

        public void UpdateMaterial(Material mat, Func<T, Color> colorConstructFn)
        {
#if UNITY_EDITOR
            // Any per-frame update. In editor keep it all fresh.
            mat.SetVector("_PaintedWavesSize", WorldSize);
            mat.SetVector("_PaintedWavesPosition", CenterPosition);
            mat.SetTexture("_PaintedWavesData", GetGPUTexture(colorConstructFn));
#endif
        }

        public bool PaintSmoothstep(Component owner, Vector3 paintPosition3, float paintWeight, T paintValue, float brushRadius, float brushStrength, Func<T, T, float, bool, T> paintFn, bool remove)
        {
            return PaintSmoothstep(owner, paintPosition3, brushRadius, paintWeight * brushStrength, paintValue, paintFn, remove);
        }
    }

#if UNITY_EDITOR
    public class PaintableEditor : ValidatedEditor
    {
        public static float s_paintRadius = 5f;
        public static float s_paintStrength = 1f;

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
            if (WavePaintingEditorTool.CurrentlyPainting)
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
                    ToolManager.SetActiveTool<WavePaintingEditorTool>();
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
#endif // UNITY_EDITOR
}
