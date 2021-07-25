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
    
            var lastFrame = _targetBakedData != null ? _targetBakedData._parameters._frameCount - 1 : 0;
            _frameToPreview = EditorGUILayout.IntSlider(_frameToPreview, 0, lastFrame);
            EditorGUILayout.EndHorizontal();
        }
    
        public override string GetInfoString()
        {
            var data = _targetBakedData;
            if (data == null) return "";
            return $"{data.name}, {data._parameters._textureResolution}x({data._parameters._textureResolution}x{data._parameters._lodCount}), {_frameToPreview+1}/{data._parameters._frameCount}";
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
                var allFramesAllPixels = _targetBakedData._framesFlattenedNative;
                var singleFrameSize = allFramesAllPixels.Length / _targetBakedData._parameters._frameCount;

                var rawDataNative = new NativeArray<float>(singleFrameSize * 4, Allocator.Temp);
                
                for (int i = 0; i < singleFrameSize / FFTBakedData.kFloatsPerPoint; i++)
                {
                    // map to 0-1 range for visualisation purposes
                    var indexX = _frameToPreview * singleFrameSize + i * FFTBakedData.kFloatsPerPoint + 0;
                    var indexY = _frameToPreview * singleFrameSize + i * FFTBakedData.kFloatsPerPoint + 1;
                    var indexZ = _frameToPreview * singleFrameSize + i * FFTBakedData.kFloatsPerPoint + 2;
                    var alphaX = Mathf.InverseLerp(_targetBakedData._smallestValue, _targetBakedData._largestValue, allFramesAllPixels[indexX]);
                    var alphaY = Mathf.InverseLerp(_targetBakedData._smallestValue, _targetBakedData._largestValue, allFramesAllPixels[indexY]);
                    var alphaZ = Mathf.InverseLerp(_targetBakedData._smallestValue, _targetBakedData._largestValue, allFramesAllPixels[indexZ]);
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

                if (rawDataNative.Length == 0)
                    return;
    
                _previewTexture ??= new Texture2D(
                    _targetBakedData._parameters._textureResolution,
                    _targetBakedData._parameters._textureResolution * _targetBakedData._parameters._lodCount,
                    TextureFormat.RGBAFloat, false, true);
    
                _previewTexture.LoadRawTextureData(rawDataNative);
                _previewTexture.Apply();

                rawDataNative.Dispose();
            }
    
            GUI.DrawTexture(r, _previewTexture, ScaleMode.ScaleToFit, false);
            // targetAnimatedWaves._bakedDataFrameToPreview = (_frameToPreview + 1) % targetBakedData._frameCount;
        }
    }
}
