// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public static class TextureArrayHelpers
    {
        private const string CLEAR_TO_BLACK_SHADER_NAME = "ClearToBlack";
        private const int SMALL_TEXTURE_DIM = 4;

        private static int krnl_ClearToBlack = -1;
        private static ComputeShader s_clearToBlackShader = null;
        private static int sp_LD_TexArray_Target = Shader.PropertyToID("_LD_TexArray_Target");

        static TextureArrayHelpers()
        {
            InitStatics();
        }

        // This is used as alternative to Texture2D.blackTexture, as using that
        // is not possible in some shaders.
        public static Texture2DArray BlackTextureArray { get; private set; }

        // Unity 2018.* does not support blitting to texture arrays, so have
        // implemented a custom version to clear to black
        public static void ClearToBlack(RenderTexture dst)
        {
            if(s_clearToBlackShader == null)
            {
                return;
            }
            s_clearToBlackShader.SetTexture(krnl_ClearToBlack, sp_LD_TexArray_Target, dst);
            s_clearToBlackShader.Dispatch(
                krnl_ClearToBlack,
                OceanRenderer.Instance.LodDataResolution / PropertyWrapperCompute.THREAD_GROUP_SIZE_X,
                OceanRenderer.Instance.LodDataResolution / PropertyWrapperCompute.THREAD_GROUP_SIZE_Y,
                dst.volumeDepth
            );
        }

        public static Texture2D CreateTexture2D(Color color, TextureFormat format)
        {
            var texture = new Texture2D(SMALL_TEXTURE_DIM, SMALL_TEXTURE_DIM, format, false, false);
            Color[] pixels = new Color[texture.height * texture.width];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static Texture2DArray CreateTexture2DArray(Texture2D texture)
        {
            var array = new Texture2DArray(
                SMALL_TEXTURE_DIM, SMALL_TEXTURE_DIM,
                LodDataMgr.MAX_LOD_COUNT,
                texture.format,
                false,
                false
            );

            for (int textureArrayIndex = 0; textureArrayIndex < LodDataMgr.MAX_LOD_COUNT; textureArrayIndex++)
            {
                Graphics.CopyTexture(texture, 0, 0, array, textureArrayIndex, 0);
            }

            return array;
        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            sp_LD_TexArray_Target = Shader.PropertyToID("_LD_TexArray_Target");

            if (BlackTextureArray == null)
            {
                BlackTextureArray = CreateTexture2DArray(Texture2D.blackTexture);
                BlackTextureArray.name = "Black Texture2DArray";
            }

            s_clearToBlackShader = ComputeShaderHelpers.LoadShader(CLEAR_TO_BLACK_SHADER_NAME);
            if(s_clearToBlackShader != null)
            {
                krnl_ClearToBlack = s_clearToBlackShader.FindKernel(CLEAR_TO_BLACK_SHADER_NAME);
            }
        }
    }
}
