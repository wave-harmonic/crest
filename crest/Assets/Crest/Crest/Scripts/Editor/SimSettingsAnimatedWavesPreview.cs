using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Crest
{
    [CustomPreview(typeof(SimSettingsAnimatedWaves))]
    public class SimSettingsAnimatedWavesPreview : ObjectPreview
    {
        private Texture2D _previewTexture;
        private int _frameToPreview = 0;
        private Object _previousTarget;
        private SimSettingsAnimatedWaves _targetAnimatedWaves => target as SimSettingsAnimatedWaves;
        private FFTBakedData _targetBakedData => _targetAnimatedWaves?._bakedFFTData;

        public override bool HasPreviewGUI()
        {
            return _targetAnimatedWaves.CollisionSource == SimSettingsAnimatedWaves.CollisionSources.BakedFFT && _targetBakedData != null;
        }

        public override void OnPreviewSettings()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(30f));
            GUILayout.Label("Frame");

            var lastFrame = _targetBakedData != null ? _targetBakedData._frameCount - 1 : 0;
            _frameToPreview = EditorGUILayout.IntSlider(_frameToPreview, 0, lastFrame);
            EditorGUILayout.EndHorizontal();
        }

        public override string GetInfoString()
        {
            var data = _targetBakedData;
            if (data == null) return "";
            return $"{data.name}, {data._textureResolution}x{data._textureResolution}, {_frameToPreview+1}/{data._frameCount}";
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);

            if (_targetBakedData == null)
                return;

            if (Mathf.Approximately(r.width, 1f))
                return;

            if (target != _previousTarget)
            {
                var allFramesAllPixels = _targetBakedData._framesFlattened;
                var singleFrameSize = allFramesAllPixels.Length / _targetBakedData._frameCount;

                var rawData = allFramesAllPixels.Skip(_frameToPreview * singleFrameSize).Take(singleFrameSize);
                if (!rawData.Any())
                    return;

                var rawDataNativeGray = new NativeArray<float>(singleFrameSize * 4, Allocator.Temp);
                
                for (int i = 0; i < singleFrameSize; i++)
                {
                    // map to 0-1 range for visualisation purposes
                    var alpha = Mathf.InverseLerp(_targetBakedData._smallestValue, _targetBakedData._largestValue, rawData.ElementAt(i));
                    var mappedValue = Mathf.Lerp(0f, 1f, alpha);

                    // convert to grayscale (if there's a prettier way, feel free)
                    var nativeIndex = i * 4;
                    rawDataNativeGray[nativeIndex] =
                        rawDataNativeGray[nativeIndex + 1] =
                            rawDataNativeGray[nativeIndex + 2] =
                                rawDataNativeGray[nativeIndex + 3] =
                                    mappedValue;
                }

                _previewTexture ??= new Texture2D(_targetBakedData._textureResolution, _targetBakedData._textureResolution,
                    TextureFormat.RGBAFloat, false, true);

                _previewTexture.LoadRawTextureData(rawDataNativeGray);
                _previewTexture.Apply();
            }

            GUI.DrawTexture(r, _previewTexture, ScaleMode.ScaleToFit, false);
            // targetAnimatedWaves._bakedDataFrameToPreview = (_frameToPreview + 1) % targetBakedData._frameCount;
        }
    }
}
