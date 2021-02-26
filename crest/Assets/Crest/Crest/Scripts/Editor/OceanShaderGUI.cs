// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Editor
{
    using UnityEngine;
    using UnityEditor;

    public class OceanShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            base.OnGUI(editor, properties);
            MaterialHelper.MigrateKeywordsGUI((Material)editor.target, editor.serializedObject);
        }
    }
}
