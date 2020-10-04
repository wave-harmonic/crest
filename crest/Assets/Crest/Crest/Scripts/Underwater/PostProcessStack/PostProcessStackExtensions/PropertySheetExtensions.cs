using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.PostProcessing
{
    public static class PropertySheetExtensions
    {
        public static void CopyPropertiesFromMaterial(this PropertySheet propertySheet, Material material) => propertySheet.material.CopyPropertiesFromMaterial(material);
    }
}
