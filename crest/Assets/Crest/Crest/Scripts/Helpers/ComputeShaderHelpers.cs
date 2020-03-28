

using UnityEngine;

namespace Crest
{
    public static class ComputeShaderHelpers
    {

        public static ComputeShader LoadShader(string path)
        {
            // We provide this helper function to ensure the user gets a friendly error message in this error case
            ComputeShader computeShader = Resources.Load<ComputeShader>(path);
            Debug.AssertFormat(computeShader != null, "The shader {0} failed to load, this is likely due to an import error. Please reimport Crest to fix this", path);
            return computeShader;
        }
    }
}
