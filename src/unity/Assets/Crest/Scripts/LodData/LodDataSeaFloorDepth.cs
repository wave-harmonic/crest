using UnityEngine;
using UnityEngine.Rendering;

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
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Color; } }
        public override RenderTexture DataTexture { get { return _rtOceanDepth; } }

        Material _matOceanDepth;
        CommandBuffer _bufOceanDepth = null;

        RenderTexture _rtOceanDepth;

        bool _oceanDepthRenderersDirty = true;
        /// <summary>Called when one or more objects that will render into depth are created, so that all objects are registered.</summary>
        public void OnOceanDepthRenderersChanged() { _oceanDepthRenderersDirty = true; }

        protected override void Start()
        {
            base.Start();

            Cam.depthTextureMode = DepthTextureMode.None;

            _matOceanDepth = new Material(Shader.Find("Ocean/Ocean Depth"));

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
            var objs = FindObjectsOfType<RenderOceanDepth>();

            // if there is nothing in the scene tagged up for depth rendering then there is no depth rendering required
            if (objs.Length < 1)
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
            _bufOceanDepth.ClearRenderTarget(false, true, Color.red * 10000f);

            foreach (var obj in objs)
            {
                if (!obj.enabled)
                    continue;

                var r = obj.GetComponent<Renderer>();
                if (r == null)
                {
                    Debug.LogError("GameObject '" + obj.gameObject.name + "' must have a renderer component attached. Unity Terrain objects are not supported - these must be captured by an Ocean Depth Cache.", obj);
                }
                else if (obj.transform.parent != null && obj.transform.parent.GetComponent<OceanDepthCache>() != null)
                {
                    _bufOceanDepth.DrawRenderer(r, r.material);
                }
                else
                {
                    _bufOceanDepth.DrawRenderer(r, _matOceanDepth);
                }
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
