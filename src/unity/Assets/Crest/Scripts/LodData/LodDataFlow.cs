// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A persistent flow simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataFlow : LodData
    {
        public override SimType LodDataType { get { return SimType.Flow; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Nothing; } }
        public override RenderTexture DataTexture { get { return CART.RT; } }
        public override bool RequiresCamera { get { return false; } }

        [SerializeField]
        protected SimSettingsFlow _settings;
        public override void UseSettings(SimSettingsBase settings) { _settings = settings as SimSettingsFlow; }
        public SimSettingsFlow Settings { get { return _settings as SimSettingsFlow; } }
        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFlow>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }


        static List<Renderer> _drawList = new List<Renderer>();

        public static void AddDraw(Renderer rend)
        {
            if (OceanRenderer.Instance == null)
            {
                _drawList.Clear();
                return;
            }

            _drawList.Add(rend);
        }

        public static void RemoveDraw(Renderer rend)
        {
            // If ocean has unloaded, clear out
            if (OceanRenderer.Instance == null)
            {
                _drawList.Clear();
                return;
            }

            _drawList.Remove(rend);
        }


        protected override void Start()
        {
            base.Start();

#if UNITY_EDITOR
            if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_FLOW_ON"))
            {
                Debug.LogWarning("Flow is not enabled on the current ocean material and will not be visible.", this);
            }
#endif
        }

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            buf.SetRenderTarget(DataTexture);
            buf.ClearRenderTarget(false, true, Color.black);
            buf.SetViewProjectionMatrices(_worldToCameraMatrix, _projectionMatrix);

            foreach(var draw in _drawList)
            {
                buf.DrawRenderer(draw, draw.material);
            }
        }
    }
}
