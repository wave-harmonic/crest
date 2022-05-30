// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// General helpers for interactive painting system.

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    /// <summary>
    /// Interface implemented by data used to expose some high level properties
    /// </summary>
    public interface IPaintedData
    {
        Texture2D Texture { get; }
        Vector2 WorldSize { get; }
        float BrushRadius { get; }
    }

    /// <summary>
    /// Interface implemented by 'clients' / receivers of painted data.
    /// </summary>
    public interface IPaintable
    {
        IPaintedData PaintedData { get; }
        Shader PaintedInputShader { get; }

        void ClearData();

        bool Paint(Vector3 paintPosition3, Vector2 paintDir, float paintWeight, bool remove);

        void MakeDirty();
    }

    /// <summary>
    /// Helper functions for various operations on data types.
    /// </summary>
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
                var len2 = result.sqrMagnitude;
                if (len2 > 1f)
                {
                    result /= Mathf.Sqrt(len2);
                }

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

    /// <summary>
    /// Base class for a texture that is paintable by the user.
    /// </summary>
    [Serializable]
    public abstract class CPUTexture2DPaintable<DataType> : CPUTexture2D<DataType>, IPaintedData
    {
        // These two params have been around the houses. It doesn't really make sense to put them here, but it was awful having them shared across all
        // input types, and having them as statics so they'd get lost after code changes. Perhaps they belong in a dictionary based on data type,
        // with recovery after recompiles.
        [Range(0f, 50f)]
        public float _brushRadius = 2f;
        public float BrushRadius => _brushRadius;

        [Range(0f, 5f)]
        public float _brushStrength = 1f;

        static int sp_ParamPaintedDataSize = Shader.PropertyToID("_PaintedDataSize");
        static int sp_ParamPaintedDataPosition = Shader.PropertyToID("_PaintedDataPosition");
        static int sp_ParamPaintedData = Shader.PropertyToID("_PaintedData");

        public void PrepareMaterial(Material mat, Func<DataType, Color> colorConstructFn)
        {
            mat.EnableKeyword("_PAINTED_ON");

            mat.SetVector(sp_ParamPaintedDataSize, WorldSize);
            mat.SetVector(sp_ParamPaintedDataPosition, CenterPosition);
            mat.SetTexture(sp_ParamPaintedData, GetGPUTexture(colorConstructFn));
        }

        public void UpdateMaterial(Material mat, Func<DataType, Color> colorConstructFn)
        {
#if UNITY_EDITOR
            // Any per-frame update. In editor keep it all fresh.
            mat.SetVector(sp_ParamPaintedDataSize, WorldSize);
            mat.SetVector(sp_ParamPaintedDataPosition, CenterPosition);
            mat.SetTexture(sp_ParamPaintedData, GetGPUTexture(colorConstructFn));
#endif
        }

        public bool PaintSmoothstep(Component owner, Vector3 paintPosition3, float paintWeight, DataType paintValue, float brushRadius, float brushStrength, Func<DataType, DataType, float, bool, DataType> paintFn, bool remove)
        {
            return PaintSmoothstep(owner, paintPosition3, brushRadius, paintWeight * brushStrength, paintValue, paintFn, remove);
        }
    }
}
