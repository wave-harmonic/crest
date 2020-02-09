// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the clip surface simulation. Attach this to GameObjects that you want to use to
    /// clip the surface of the ocean.
    /// </summary>
    public class RegisterClipSurfaceInput : RegisterLodDataInput<LodDataMgrClipSurface>
    {
        [Tooltip("Uses the 'clip from geometry' shader. There are other clip shaders available.")]
        [SerializeField] bool _assignClipSurfaceMaterial = true;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(0f, 1f, 1f, 0.5f);

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_assignClipSurfaceMaterial)
            {
                var rend = GetComponent<Renderer>();
                rend.material = new Material(Shader.Find("Crest/Inputs/Clip Surface/Add From Geometry"));
            }
        }
    }
}
