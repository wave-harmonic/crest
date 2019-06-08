using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Crest
{

public class TextureArray
{
    // This is used as alternative to Texture2D.blackTexture, as using that
    // is not possible in some shaders.
    public static Texture2DArray blackTextureArray { get {
        if(_blackTextureArray == null)
        {
            _blackTextureArray = new Texture2DArray(
                Texture2D.blackTexture.width, Texture2D.blackTexture.height,
                OceanRenderer.Instance.CurrentLodCount,
                Texture2D.blackTexture.format,
                false,
                false
            );

            for(int textureArrayIndex = 0; textureArrayIndex < OceanRenderer.Instance.CurrentLodCount; textureArrayIndex++)
            {
                Graphics.CopyTexture(Texture2D.blackTexture, 0, 0, _blackTextureArray, textureArrayIndex, 0);
            }
            _blackTextureArray.name = "Black Texture2DArray";
        }
        return _blackTextureArray;
    } }
    private static Texture2DArray _blackTextureArray = null;
}

}
