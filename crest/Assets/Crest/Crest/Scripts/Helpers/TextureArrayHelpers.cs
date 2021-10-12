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
        static Texture2DArray _blackTextureArray = null;
        public static Texture2DArray BlackTextureArray
        {
            get
            {
                if (_blackTextureArray == null)
                {
                    CreateBlackTexArray();
                }
                return _blackTextureArray;
            }
        }

        // Custom implementation of clear to black instead of blitting to a texture array as the latter breaks Xbox One
        // and Xbox Series X. See #857 which changed to Graphics.Blit and #868 which reverts that change. Or see commit:
        // https://github.com/wave-harmonic/crest/commit/9160898972051a276f12eff0bd9b832d2992ae62
        public static void ClearToBlack(RenderTexture dst)
        {
            if (s_clearToBlackShader == null)
            {
                return;
            }
            s_clearToBlackShader.SetTexture(krnl_ClearToBlack, sp_LD_TexArray_Target, dst);
            s_clearToBlackShader.Dispatch(
                krnl_ClearToBlack,
                OceanRenderer.Instance.LodDataResolution / LodDataMgr.THREAD_GROUP_SIZE_X,
                OceanRenderer.Instance.LodDataResolution / LodDataMgr.THREAD_GROUP_SIZE_Y,
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
                // There is a bug using Graphics.CopyTexture with Texture2DArray when "Texture Quality"
                // (QualitySettings.masterTextureLimit) is not "Full Res" (0) where result is junk (white from what I
                // have seen). Changing this setting at runtime might cause a hitch so use SetPixels for now.
                // Reported to Unity on 2021.09.15.
                // https://issuetracker.unity3d.com/product/unity/issues/guid/1365775
                array.SetPixels(texture.GetPixels(0), textureArrayIndex, 0);
            }

            array.Apply();

            return array;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            if (OceanRenderer.RunningWithoutGPU)
            {
                // No texture arrays when no graphics card..
                return;
            }

            // Init here from 2019.3 onwards

            if (_blackTextureArray == null)
            {
                CreateBlackTexArray();
            }

            if (s_clearToBlackShader == null)
            {
                s_clearToBlackShader = ComputeShaderHelpers.LoadShader(CLEAR_TO_BLACK_SHADER_NAME);
            }
            if (s_clearToBlackShader != null)
            {
                krnl_ClearToBlack = s_clearToBlackShader.FindKernel(CLEAR_TO_BLACK_SHADER_NAME);
            }
        }

        static void CreateBlackTexArray()
        {
            _blackTextureArray = CreateTexture2DArray(Texture2D.blackTexture);
            _blackTextureArray.name = "Black Texture2DArray";
        }
    }
}
