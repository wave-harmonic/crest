using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Crest
{
    /// <summary>
    /// Renders relative depth of ocean floor, by rendering the relative height of tagged objects from top down. This loddata rides
    /// on the LodDataAnimatedWaves currently.
    /// </summary>
    public class LodDataSeaFloorDepth : LodData
    {
        public override SimType LodDataType { get { return SimType.SeaFloorDepth; } }
        public override SimSettingsBase CreateDefaultSettings() { return null; }
        public override void UseSettings(SimSettingsBase settings) { }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RFloat; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Color; } }
        public override RenderTexture DataTexture { get { return _rtOceanDepth; } }

        bool _oceanDepthRenderersDirty = true;

        RenderTexture _rtOceanDepth;
        CommandBuffer _bufOceanDepth = null;

        static Dictionary<Renderer, Material> _depthRenderers = new Dictionary<Renderer, Material>();
        public static void AddRenderOceanDepth(Renderer rend, Material mat)
        {
            if (OceanRenderer.Instance == null)
            {
                _depthRenderers.Clear();
                return;
            }

            _depthRenderers.Add(rend, mat);

            // notify there is a new contributor to ocean depth
            foreach (var ldsd in OceanRenderer.Instance._lodDataSeaDepths)
            {
                if (!ldsd) continue;
                ldsd._oceanDepthRenderersDirty = true;
            }
        }
        public static void RemoveRenderOceanDepth(Renderer rend)
        {
            if (OceanRenderer.Instance == null)
            {
                _depthRenderers.Clear();
                return;
            }

            _depthRenderers.Remove(rend);

            // notify there is a new contributor to ocean depth
            foreach (var ldsd in OceanRenderer.Instance._lodDataSeaDepths)
            {
                if (!ldsd) continue;
                ldsd._oceanDepthRenderersDirty = true;
            }
        }

        protected override void Start()
        {
            base.Start();

            Cam.depthTextureMode = DepthTextureMode.None;

            int res = OceanRenderer.Instance.LodDataResolution;
            _rtOceanDepth = new RenderTexture(res, res, 0);
            _rtOceanDepth.name = gameObject.name + "_oceanDepth";
            _rtOceanDepth.format = TextureFormat;
            _rtOceanDepth.useMipMap = false;
            _rtOceanDepth.anisoLevel = 0;
        }

        private void Update()
        {
            if (_oceanDepthRenderersDirty)
            {
                UpdateCmdBufOceanFloorDepth();
                _oceanDepthRenderersDirty = false;
            }
        }

        // The command buffer populates the LODs with ocean depth data. It submits any objects with a RenderOceanDepth component attached.
        // It's stateless - the textures don't have to be managed across frames/scale changes
        void UpdateCmdBufOceanFloorDepth()
        {
            // if there is nothing in the scene tagged up for depth rendering then there is no depth rendering required
            if (_depthRenderers.Count < 1)
            {
                if (_bufOceanDepth != null)
                {
                    _bufOceanDepth.Clear();
                }

                return;
            }

            if (_bufOceanDepth == null)
            {
                _bufOceanDepth = new CommandBuffer();
                Cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _bufOceanDepth);
                _bufOceanDepth.name = "Ocean Depth";
            }

            _bufOceanDepth.Clear();

            _bufOceanDepth.SetRenderTarget(_rtOceanDepth);
            _bufOceanDepth.ClearRenderTarget(false, true, Color.black);

            foreach (var entry in _depthRenderers)
            {
                _bufOceanDepth.DrawRenderer(entry.Key, entry.Value);
            }
        }

        void OnEnable()
        {
            RemoveCommandBuffers();
        }

        void OnDisable()
        {
            RemoveCommandBuffers();
        }

        void RemoveCommandBuffers()
        {
            if (_bufOceanDepth != null)
            {
                Cam.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _bufOceanDepth);
                _bufOceanDepth = null;
            }
        }
    }
}
