// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

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

    [Serializable]
    public abstract class CPUTexture2D<T> : CPUTexture2DBase
    {
        [SerializeField, HideInInspector]
        T[] _data;

        [SerializeField, HideInInspector]
        bool _dataChangeFlag = false;

        Texture2D _textureGPU;

        // Interpolation func(data[], dataResolutionX, bottomLeftCoord, fractional) return interpolated value
        public bool Sample(Vector3 position3, Func<T[], int, Vector2Int, Vector2, T> interpolationFn, ref T result)
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
            var clamped = false;
            if (coordBottomLeft.x < 0)
            {
                coordBottomLeft.x = 0;
                fractional.x = 0f;
                clamped = true;
            }
            else if (coordBottomLeft.x >= _resolution.x - 1)
            {
                coordBottomLeft.x = _resolution.x - 2;
                fractional.x = 1f;
                clamped = true;
            }
            if (coordBottomLeft.y < 0)
            {
                coordBottomLeft.y = 0;
                fractional.y = 0f;
                clamped = true;
            }
            else if (coordBottomLeft.y >= _resolution.y - 1)
            {
                coordBottomLeft.y = _resolution.y - 2;
                fractional.y = 1f;
                clamped = true;
            }

            result = interpolationFn(_data, _resolution.x, coordBottomLeft, fractional);
            return !clamped;
        }

        // Paint func(Existing value, Paint value, Value weight) returns new value
        protected bool PaintSmoothstep(Component owner, Vector3 paintPosition3, float paintRadius, float paintWeight, T paintValue, Func<T, T, float, bool, T> paintFn, bool remove)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Crest:CPUTexture2D.PaintSmoothstep");

            InitialiseDataIfNeeded(owner);

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

            var valuesWritten = false;

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

                    float xn = (x - paintTexel.x) / radiusTexel.x;
                    float yn = (y - paintTexel.y) / radiusTexel.y;
                    var alpha = Mathf.Sqrt(xn * xn + yn * yn);
                    var wt = Mathf.SmoothStep(1f, 0f, alpha);

                    var idx = y * _resolution.x + x;
                    _data[idx] = paintFn(_data[idx], paintValue, paintWeight * wt, remove);

                    valuesWritten = true;
                }
            }

            if (valuesWritten)
            {
                _dataChangeFlag = true;
            }

            UnityEngine.Profiling.Profiler.EndSample();

            return valuesWritten;
        }

        public void InitialiseDataIfNeeded(Component owner)
        {
            // 2x2 minimum instead of 1x1 as latter would require painful special casing in sample function
            Debug.Assert(_resolution.x > 1 && _resolution.y > 1);

            if (_data == null || _data.Length != _resolution.x * _resolution.y)
            {
                // Could copy data to be more graceful
                _data = new T[_resolution.x * _resolution.y];

#if UNITY_EDITOR
                if (owner != null)
                {
                    EditorUtility.SetDirty(owner);
                }
#endif
            }
        }

        public override Texture2D Texture => _textureGPU;

        // This may allocate the texture and update it with data if needed.
        public Texture2D GetGPUTexture(Func<T, Color> colorConstructFn)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Crest:CPUTexture2D.GetGPUTexture");

            InitialiseDataIfNeeded(null);

            if (_textureGPU == null || _textureGPU.width != _resolution.x || _textureGPU.height != _resolution.y || _textureGPU.graphicsFormat != GraphicsFormat)
            {
                Debug.Assert(GraphicsFormat != GraphicsFormat.None);

                _textureGPU = new Texture2D(_resolution.x, _resolution.y, GraphicsFormat, 0, TextureCreationFlags.None);

                _dataChangeFlag = true;
            }

            if (_dataChangeFlag)
            {
                var colors = new Color[_data.Length];
                for (int i = 0; i < _data.Length; i++)
                {
                    colors[i] = colorConstructFn(_data[i]);
                }
                _textureGPU.SetPixels(colors);

                _textureGPU.Apply();
            }

            _dataChangeFlag = false;

            UnityEngine.Profiling.Profiler.EndSample();

            return _textureGPU;
        }

        public void Clear(Component owner, T value)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Crest:CPUTexture2D.Clear");

            InitialiseDataIfNeeded(owner);

            for (int i = 0; i < _data.Length; i++)
            {
                _data[i] = value;
            }

            _dataChangeFlag = true;

            UnityEngine.Profiling.Profiler.EndSample();
        }

        protected override void SetWorldSize(Vector2 newWorldSize)
        {
            // Could copy data to be more graceful
            _worldSize = newWorldSize;
        }

        protected override void SetCenterPosition(Vector2 newCenterPosition)
        {
            // Could copy data to be more graceful..
            _centerPosition = newCenterPosition;
        }

        protected override void SetResolution(Vector2Int newResolution)
        {
            // Could copy data to be more graceful..
            _resolution = newResolution;
        }
    }

    [Serializable]
    public abstract class CPUTexture2DBase
    {
        [SerializeField]
        protected Vector2 _worldSize = Vector2.one * 128f;
        public Vector2 WorldSize
        {
            get => _worldSize;
            set => SetWorldSize(value);
        }

        [SerializeField, HideInInspector]
        protected Vector2 _centerPosition = Vector2.zero;
        public Vector2 CenterPosition
        {
            get => _centerPosition;
            set => SetCenterPosition(value);
        }
        public Vector3 CenterPosition3
        {
            get => new Vector3(_centerPosition.x, 0f, _centerPosition.y);
            set => SetCenterPosition(new Vector2(value.x, value.z));
        }

        [SerializeField]
        protected Vector2Int _resolution = Vector2Int.one * 64;
        public Vector2Int Resolution
        {
            get => _resolution;
            set => SetResolution(value);
        }

        protected abstract void SetWorldSize(Vector2 newWorldSize);
        protected abstract void SetCenterPosition(Vector2 newCenterPosition);
        protected abstract void SetResolution(Vector2Int newResolution);

        public abstract Texture2D Texture { get; }

        public abstract GraphicsFormat GraphicsFormat { get; }
    }
}
