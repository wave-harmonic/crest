using System;
using UnityEngine;

namespace Crest
{
    public static class CPUTexture2DHelpers
    {
        public static float BilinearInterpolateFloat(float[] data, int dataResolutionX, Vector2Int coordBottomLeft, Vector2 fractional)
        {
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

        public static float PaintFnAlphaBlendFloat(float existingValue, float paintValue, float weight)
        {
            return Mathf.Lerp(existingValue, paintValue, weight);
        }

        public static float PaintFnAdditiveBlendFloat(float existingValue, float paintValue, float weight)
        {
            return existingValue + paintValue * weight;
        }
    }

    [System.Serializable]
    public class CPUTexture2D<T>
    {
        [SerializeField, HideInInspector]
        T[] _data;

        [SerializeField, HideInInspector]
        Vector2 _worldSize = Vector2.one;
        public Vector2 WorldSize
        {
            get => _worldSize;
            set => SetWorldSize(value);
        }

        [SerializeField, HideInInspector]
        Vector2 _centerPosition = Vector2.zero;
        public Vector2 CenterPosition
        {
            get => _centerPosition;
            set => SetCenterPosition(value);
        }

        // 2x2 minimum instead of 1x1 as latter would require painful special casing in sample function
        Vector2Int _resolution = Vector2Int.one * 2;
        public Vector2Int Resolution
        {
            get => _resolution;
            set => SetResolution(value);
        }

        // Interpolation func(data[], dataResolutionX, bottomLeftCoord, fractional) return interpolated value
        public T Sample(Vector3 position3, Func<T[], int, Vector2Int, Vector2, T> interpolationFn)
        {
            var position = new Vector2(position3.x, position3.z);
            var uv = (position - _centerPosition) / _worldSize + 0.5f * Vector2.one;
            var texel = uv * _resolution - 0.5f * Vector2.one;
            var texelBottomLeft = new Vector2(Mathf.Floor(texel.x), Mathf.Floor(texel.y));
            var coordBottomLeft = new Vector2Int
            {
                x = Mathf.FloorToInt(texelBottomLeft.x),
                y = Mathf.FloorToInt(texelBottomLeft.y)
            };

            var fractional = texel - texelBottomLeft;

            // Clamp
            if (coordBottomLeft.x < 0)
            {
                coordBottomLeft.x = 0;
                fractional.x = 0f;
            }
            if (coordBottomLeft.y < 0)
            {
                coordBottomLeft.y = 0;
                fractional.y = 0f;
            }
            if (coordBottomLeft.x >= _resolution.x)
            {
                coordBottomLeft.x = _resolution.x - 2;
                fractional.x = 1f;
            }
            if (coordBottomLeft.y >= _resolution.y)
            {
                coordBottomLeft.y = _resolution.y - 2;
                fractional.y = 1f;
            }

            return interpolationFn(_data, _resolution.x, coordBottomLeft, fractional);
        }

        // Paint func(Existing value, Paint value, Value weight) returns new value
        public void PaintSmoothstep(Vector3 paintPosition3, float paintRadius, float paintWeight, T paintValue, Func<T, T, float, T> paintFn)
        {
            // TODO - remove this later
            paintWeight = Mathf.Clamp01(paintWeight);

            InitialiseDataIfNeeded();

            var paintPosition = new Vector2(paintPosition3.x, paintPosition3.z);
            var paintUv = (paintPosition - _centerPosition) / _worldSize + 0.5f * Vector2.one;
            var paintTexel = paintUv * _resolution - 0.5f * Vector2.one;
            var paintCoord = new Vector2Int
            {
                x = Mathf.RoundToInt(paintTexel.x),
                y = Mathf.RoundToInt(paintTexel.y)
            };

            var radiusUV = paintRadius * Vector2.one / _worldSize;
            var radiusTexel = new Vector2Int
            {
                x = Mathf.CeilToInt(radiusUV.x * _resolution.x),
                y = Mathf.CeilToInt(radiusUV.y * _resolution.y)
            };

            for (int yy = -radiusTexel.y; yy <= radiusTexel.y; yy++)
            {
                int y = paintCoord.y + yy;
                if (y < 0) continue;
                if (y >= _resolution.y) break;

                for (int xx = -radiusTexel.x; xx <= radiusTexel.x; xx++)
                {
                    int x = paintCoord.x + xx;
                    if (x < 0) continue;
                    if (x >= _resolution.x) break;

                    float xn = xx / radiusTexel.x;
                    float yn = yy / radiusTexel.y;
                    var alpha = Mathf.Sqrt(xn * xn + yn * yn);
                    var wt = Mathf.SmoothStep(1f, 0f, alpha);

                    var idx = y * _resolution.x + x;
                    _data[idx] = paintFn(_data[idx], paintValue, paintWeight * wt);
                }
            }
        }

        public void InitialiseDataIfNeeded()
        {
            // 2x2 minimum instead of 1x1 as latter would require painful special casing in sample function
            Debug.Assert(_resolution.x > 1 && _resolution.y > 1);

            if (_data == null || _data.Length != _resolution.x * _resolution.y)
            {
                // Could copy data to be more graceful
                _data = new T[_resolution.x * _resolution.y];
            }
        }

        void SetWorldSize(Vector2 newWorldSize)
        {
            // Could copy data to be more graceful
            _worldSize = newWorldSize;
        }

        void SetCenterPosition(Vector2 newCenterPosition)
        {
            // Could copy data to be more graceful..
            _centerPosition = newCenterPosition;
        }

        private void SetResolution(Vector2Int newResolution)
        {
            // Could copy data to be more graceful..
            _resolution = newResolution;
        }
    }
}
