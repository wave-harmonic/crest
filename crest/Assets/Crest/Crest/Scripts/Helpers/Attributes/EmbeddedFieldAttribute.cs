namespace Crest
{
    using UnityEngine;
    using Crest.EditorHelpers;

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class EmbeddedFieldAttribute : PropertyAttribute
    {
        internal EmbeddeAssetEditor editor;

        public EmbeddedFieldAttribute()
        {
            editor = new EmbeddeAssetEditor();
        }
    }
}
