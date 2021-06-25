// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public static class TextureArrayHelpers
    {
        private const int SMALL_TEXTURE_DIM = 4;

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

        // Unity 2018.* does not support blitting to texture arrays, so have
        // implemented a custom version to clear to black
        public static void ClearToBlack(RenderTexture dst)
        {
            Graphics.Blit(Texture2D.blackTexture, dst);
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            if (OceanRenderer.RunningWithoutGPU)
            {
                // No texture arrays when no graphics card..
                return;
            }

            if (_blackTextureArray == null)
            {
                CreateBlackTexArray();
            }
        }

        static void CreateBlackTexArray()
        {
            _blackTextureArray = CreateTexture2DArray(Texture2D.blackTexture);
            _blackTextureArray.name = "Black Texture2DArray";
        }
    }
}
