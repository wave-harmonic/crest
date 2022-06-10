// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public static class CPUTexture2DHelpers
    {
        public static float BilinearInterpolateFloat(float[] data, int dataResolutionX, Vector2Int coordBottomLeft, Vector2 fractional)
        {
#if UNITY_EDITOR
            if ((coordBottomLeft.y + 1) * dataResolutionX + coordBottomLeft.x + 1 >= data.Length)
            {
                Debug.Assert(false, $"Out of bounds array access coords ({coordBottomLeft.x + 1}, {coordBottomLeft.y + 1}), resolution x = {dataResolutionX}.");
            }
#endif

            // Assumes data is bigger than 1x1, and assume coordBottomLeft is in valid range!
            var v00 = data[coordBottomLeft.y * dataResolutionX + coordBottomLeft.x];
            var v01 = data[coordBottomLeft.y * dataResolutionX + coordBottomLeft.x + 1];
            var v10 = data[(coordBottomLeft.y + 1) * dataResolutionX + coordBottomLeft.x];
            var v11 = data[(coordBottomLeft.y + 1) * dataResolutionX + coordBottomLeft.x + 1];

            var v0 = Mathf.Lerp(v00, v01, fractional.x);
            var v1 = Mathf.Lerp(v10, v11, fractional.x);
            var v = Mathf.Lerp(v0, v1, fractional.y);
            return v;
        }

        public static Vector2 BilinearInterpolateVector2(Vector2[] data, int dataResolutionX, Vector2Int coordBottomLeft, Vector2 fractional)
        {
            // Assumes data is bigger than 1x1, and assume coordBottomLeft is in valid range!
            var v00 = data[coordBottomLeft.y * dataResolutionX + coordBottomLeft.x];
            var v01 = data[coordBottomLeft.y * dataResolutionX + coordBottomLeft.x + 1];
            var v10 = data[(coordBottomLeft.y + 1) * dataResolutionX + coordBottomLeft.x];
            var v11 = data[(coordBottomLeft.y + 1) * dataResolutionX + coordBottomLeft.x + 1];

            var v0 = Vector2.Lerp(v00, v01, fractional.x);
            var v1 = Vector2.Lerp(v10, v11, fractional.x);
            var v = Vector2.Lerp(v0, v1, fractional.y);
            return v;
        }

        public static Color ColorConstructFnOneChannel(float value) => new Color(value, 0f, 0f);
        public static Color ColorConstructFnTwoChannel(Vector2 value) => new Color(value.x, value.y, 0f);
    }
}
