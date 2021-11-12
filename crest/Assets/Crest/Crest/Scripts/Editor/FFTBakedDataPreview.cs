// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if CREST_UNITY_MATHEMATICS

using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Shows a preview of baked FFT data
    /// </summary>
    [CustomPreview(typeof(FFTBakedData))]
    public class FFTBakedDataPreview : ObjectPreview
    {
        private Texture2D _previewTexture;
        private int _frameToPreview = 0;
        private Object _previousTarget;

        private FFTBakedData TargetBakedData => target as FFTBakedData;

        public override bool HasPreviewGUI() => true;

        /// <summary>
        /// Tries to draw a GUI in the preview header bar (frame selector). Behaviour is a bit funky but
        /// hopefully ok.
        /// </summary>
        public override void OnPreviewSettings()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(30f));
            GUILayout.Label("Frame");

            var lastFrame = TargetBakedData != null ? TargetBakedData._parameters._frameCount - 1 : 0;
            _frameToPreview = EditorGUILayout.IntSlider(_frameToPreview, 0, lastFrame);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Text displayed on top of preview.
        /// </summary>
        public override string GetInfoString()
        {
            var data = TargetBakedData;
            if (data == null) return "";
            return $"{data.name}, {data._parameters._textureResolution}x({data._parameters._textureResolution}x{data._parameters._lodCount}), {_frameToPreview + 1}/{data._parameters._frameCount}";
        }

        /// <summary>
        /// Draws selected baked data frame.
        /// </summary>
        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);

            if (TargetBakedData == null) return;

            if (Mathf.Approximately(r.width, 1f)) return;

            if (target != _previousTarget)
            {
                var allFramesAllPixels = TargetBakedData._framesFlattenedNative;
                var singleFrameSize = allFramesAllPixels.Length / TargetBakedData._parameters._frameCount;

                var rawDataNative = new NativeArray<float>(singleFrameSize * 4, Allocator.Temp);

                for (int i = 0; i < singleFrameSize / FFTBakedData.kFloatsPerPoint; i++)
                {
                    // map to 0-1 range for visualisation purposes
                    var indexX = _frameToPreview * singleFrameSize + i * FFTBakedData.kFloatsPerPoint + 0;
                    var indexY = _frameToPreview * singleFrameSize + i * FFTBakedData.kFloatsPerPoint + 1;
                    var indexZ = _frameToPreview * singleFrameSize + i * FFTBakedData.kFloatsPerPoint + 2;
                    var alphaX = Mathf.InverseLerp(TargetBakedData._smallestValue, TargetBakedData._largestValue, allFramesAllPixels[indexX]);
                    var alphaY = Mathf.InverseLerp(TargetBakedData._smallestValue, TargetBakedData._largestValue, allFramesAllPixels[indexY]);
                    var alphaZ = Mathf.InverseLerp(TargetBakedData._smallestValue, TargetBakedData._largestValue, allFramesAllPixels[indexZ]);
                    var mappedValueX = Mathf.Lerp(-6f, 6f, alphaX);
                    var mappedValueY = Mathf.Lerp(-6f, 6f, alphaY);
                    var mappedValueZ = Mathf.Lerp(-6f, 6f, alphaZ);

                    // convert to grayscale (if there's a prettier way, feel free)
                    var nativeIndex = i * 4;
                    rawDataNative[nativeIndex + 0] = Mathf.Clamp01(mappedValueX);
                    rawDataNative[nativeIndex + 1] = Mathf.Clamp01(mappedValueY);
                    rawDataNative[nativeIndex + 2] = Mathf.Clamp01(mappedValueZ);
                    rawDataNative[nativeIndex + 3] = 1f;
                }

                if (rawDataNative.Length == 0) return;

                if (_previewTexture == null)
                {
                    _previewTexture = new Texture2D(
                        TargetBakedData._parameters._textureResolution,
                        TargetBakedData._parameters._textureResolution * TargetBakedData._parameters._lodCount,
                        TextureFormat.RGBAFloat, false, true);
                }

                _previewTexture.LoadRawTextureData(rawDataNative);
                _previewTexture.Apply();

                rawDataNative.Dispose();
            }

            GUI.DrawTexture(r, _previewTexture, ScaleMode.ScaleToFit, false);
        }
    }
}

#endif // CREST_UNITY_MATHEMATICS
