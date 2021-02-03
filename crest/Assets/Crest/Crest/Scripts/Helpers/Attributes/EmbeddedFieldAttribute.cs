// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using Crest.EditorHelpers;
    using UnityEngine;

    public class EmbeddedFieldAttribute : PropertyAttribute
    {
        internal EmbeddedAssetEditor editor;

        public EmbeddedFieldAttribute()
        {
            editor = new EmbeddedAssetEditor();
        }
    }
}
