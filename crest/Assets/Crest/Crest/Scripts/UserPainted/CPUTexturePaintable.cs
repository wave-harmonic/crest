// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    public interface IPaintedData
    {
        Texture2D Texture { get; }
        Vector2 WorldSize { get; }
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

            // Has to be done outside - this contains non-generic knowledge. TODO review how this is done.
            //mat.SetTexture("_PaintedWavesData", GPUTexture(GraphicsFormat.R16_SFloat, CPUTexture2DHelpers.ColorConstructFnOneChannel));
            mat.SetVector("_PaintedWavesSize", WorldSize);
            mat.SetVector("_PaintedWavesPosition", CenterPosition);
            mat.SetTexture("_PaintedWavesData", GetGPUTexture(colorConstructFn));
        }

        public void UpdateMaterial(Material mat, Func<T, Color> colorConstructFn)
        {
#if UNITY_EDITOR
            // Any per-frame update. In editor keep it all fresh.
            // Has to be done outside - this contains non-generic knowledge. TODO review how this is done.
            //mat.SetTexture("_PaintedWavesData", GPUTexture(GraphicsFormat.R16_SFloat, CPUTexture2DHelpers.ColorConstructFnOneChannel));
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
}
