// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public static class TextureArrayHelpers
    {
        private const string ClearToBlackShaderName = "ClearToBlack";
        private static int krnl_ClearToBlack = -1;
        private static ComputeShader ClearToBlackShader;
        private static int sp_LD_TexArray_Target = Shader.PropertyToID("_LD_TexArray_Target");

        static TextureArrayHelpers()
        {
            if (BlackTextureArray == null)
            {
                BlackTextureArray = new Texture2DArray(
                    Texture2D.blackTexture.width, Texture2D.blackTexture.height,
                    OceanRenderer.Instance.CurrentLodCount,
                    Texture2D.blackTexture.format,
                    false,
                    false
                );

                for (int textureArrayIndex = 0; textureArrayIndex < OceanRenderer.Instance.CurrentLodCount; textureArrayIndex++)
                {
                    Graphics.CopyTexture(Texture2D.blackTexture, 0, 0, BlackTextureArray, textureArrayIndex, 0);
                }

                BlackTextureArray.name = "Black Texture2DArray";
            }

            ClearToBlackShader = Resources.Load<ComputeShader>(ClearToBlackShaderName);
            krnl_ClearToBlack = ClearToBlackShader.FindKernel(ClearToBlackShaderName);
        }

        // This is used as alternative to Texture2D.blackTexture, as using that
        // is not possible in some shaders.
        public static Texture2DArray BlackTextureArray { get; private set; }

        // Unity 2018.* does not support blitting to texture arrays, so have
        // implemented a custom version to clear to black
        public static void ClearToBlack(RenderTexture dst)
        {
            ClearToBlackShader.SetTexture(krnl_ClearToBlack, sp_LD_TexArray_Target, dst);
            ClearToBlackShader.Dispatch(
                krnl_ClearToBlack,
                OceanRenderer.Instance.LodDataResolution / PropertyWrapperCompute.THREAD_GROUP_SIZE_X,
                OceanRenderer.Instance.LodDataResolution / PropertyWrapperCompute.THREAD_GROUP_SIZE_Y,
                dst.volumeDepth
            );
        }
    }
}
