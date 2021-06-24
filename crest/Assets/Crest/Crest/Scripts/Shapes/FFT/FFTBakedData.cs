using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Crest
{
    [PreferBinarySerialization] // improves filesize almost 4-fold, doesn't seem to impact editor performance
    public class FFTBakedData : ScriptableObject
    {
        // TODO: these fields are useful in the inspector, but they should be read only (grayed out), how? property drawer?
        [SerializeField] private float _period;
        [SerializeField] private float _timeResolution;
        [HideInInspector] public float[] _framesFlattened;
        [HideInInspector] public int _frameCount;
        [HideInInspector] public int _textureResolution;
        public int _frameToPreview = 0;

        public void Initialize(float period, float timeResolution, float[][] frames, int textureResolution)
        {
            _period = period;
            _timeResolution = timeResolution;
            _textureResolution = textureResolution;
            _framesFlattened = frames.SelectMany(x => x).ToArray();
            _frameCount = frames.Length;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}