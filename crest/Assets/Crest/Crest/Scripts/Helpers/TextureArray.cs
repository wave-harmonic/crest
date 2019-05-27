using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Crest
{

public class TextureArray
{
    public static Texture2DArray Black { get {
        if(_blackTextureArray == null)
        {
            // TODO(MRT): Make sure that we setup this black texture2D array standin correctly.
            // TODO(MRT): Set slice count appropriately
            _blackTextureArray = new Texture2DArray(
                Texture2D.blackTexture.width, Texture2D.blackTexture.height,
                OceanRenderer.Instance.CurrentLodCount,
                Texture2D.blackTexture.format,
                false,
                false
                );

            // TODO(MRT): Do this initialisation on load time to prevent hitching first time this is called?
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
