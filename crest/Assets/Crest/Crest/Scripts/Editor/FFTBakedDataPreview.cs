using System.Diagnostics;
using System.Linq;
using Crest;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Crest
{
    [CustomPreview(typeof(FFTBakedData))]
    public class FFTBakedDataPreview : ObjectPreview
    {
        private Texture2D _previewTexture;
        private int _previousFrame = -1;
        private Object _previousTarget;

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);

            var targetBakedData = target as FFTBakedData;

            if (targetBakedData == null)
                return;

            if (targetBakedData._frameToPreview != _previousFrame || // new frame selected, generate a texture 
                target != _previousTarget)
            {
                var allFramesAllPixels = targetBakedData._framesFlattened;
                var singleFrameSize = allFramesAllPixels.Length / targetBakedData._frameCount;

                var rawData = allFramesAllPixels.Skip(targetBakedData._frameToPreview * singleFrameSize).Take(singleFrameSize).ToArray();

                // rescale for benefit of visualisation. lug i'm sure you have a much fancier way of doing this
                for (int i = 0; i < rawData.Length; i++)
                {
                    var scale = 0.05f;
                    rawData[i] = 0.5f + scale * rawData[i];
                }

                var rawDataNative = new NativeArray<float>(rawData, Allocator.Temp);

                _previewTexture ??= new Texture2D(targetBakedData._textureResolution, targetBakedData._textureResolution,
                    TextureFormat.RFloat, false, true);

                _previewTexture.LoadRawTextureData(rawDataNative);
                _previewTexture.Apply();
            }

            GUI.DrawTexture(r, _previewTexture, ScaleMode.ScaleToFit, false);
            _previousFrame = targetBakedData._frameToPreview;
            _previousTarget = target;
        }
    }
}
