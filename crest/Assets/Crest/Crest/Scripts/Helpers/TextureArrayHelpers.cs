// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public static class TextureArrayHelpers
    {
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
        }

        // This is used as alternative to Texture2D.blackTexture, as using that
        // is not possible in some shaders.
        public static Texture2DArray BlackTextureArray { get; private set; }
    }
}
