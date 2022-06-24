// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Runs a shallow water simulation at global sea level in a domain around its transform,
    /// and injects the results of the sim into the water data.
    /// </summary>
    [ExecuteAlways]
    public partial class ShallowWaterSimulation : MonoBehaviour, LodDataMgrAnimWaves.IShapeUpdatable
    {
        [Header("Settings")]
        [Tooltip("The width of the simulation area (m). Enable gizmos to see a wireframe outline of the domain."), SerializeField, UnityEngine.Range(8, 1024)]
        float _domainWidth = 64f;
        [Tooltip("The depth of the water in the shallow water simulation (m). Any underwater surfaces deeper than this depth will not influence the sim. Large values can lead to instabilities / jitter in the result."), SerializeField, Range(0.1f, 16.0f)]
        float _waterDepth = 2f;
        [Tooltip("Simulation resolution; width of simulation grid cell (m). Smaller values will increase resolution but take more computation time and memory, and may lead to instabilities for small values."), SerializeField, UnityEngine.Range(0.01f, 2f)]
        float _texelSize = 32f / 512f;
        [Tooltip("Maximum resolution of simulation grid. Safety limit to avoid simulation using large amount of video memory."), SerializeField, UnityEngine.Range(16, 4096)]
        int _maximumResolution = 1024;
        [Tooltip("Time step used for simulation (s). Smaller values can make simulation more stable but require more runtime computation."), SerializeField, Range(0.001f, 0.03333333f)]
        float _simulationTimeStep = 0.01f;
        [Tooltip("Rate at which to remove water at the boundaries of the domain, useful for preventing buildup of water when simulating shoreline waves."), SerializeField]
        float _drainWaterAtBoundaries = -0.01f;
        [Tooltip("Friction applied to water to prevent dampen velocities."), SerializeField]
        float _friction = 0.02f;
        [Tooltip("Stability measure - limits velocities. Default 0.5."), SerializeField]
        float _courantNumber = 0.5f;
        [Tooltip("Recompute ground heights every frame. Only enable this if terrain used by water system changes at runtime."), SerializeField]
        bool _allowDynamicSeabed = false;

        [Header("Blending With Waves")]
        [Tooltip("The minimum depth for blending (m). When the water depth is less than this value, animated waves will not contribute at all, water shape will come purely from this simulation. Negative depths are valid and occur when surfaces are above sea level."), SerializeField, UnityEngine.Range(-10f, 10f)]
        float _blendShallowMinDepth = 0f;
        [Tooltip("The maximum depth for blending (m). When the water depth is greater than this value, this simulation will not contribute at all, water shape will come purely from the normal ocean waves. Negative depths are valid and occur when surfaces are above sea level."), SerializeField, UnityEngine.Range(-10f, 10f)]
        float _blendShallowMaxDepth = 4f;
        [Tooltip("The intensity at which ocean waves inject water into the simulation."), SerializeField, UnityEngine.Range(0f, 1f)]
        float _blendPushUpStrength = 0.1f;

        [Header("Distance Culling")]
        [Tooltip("Disable simulation when viewpoint far from domain."), SerializeField]
        bool _enableDistanceCulling = false;
        [SerializeField, Predicated("_enableDistanceCulling"), DecoratedField, Range(1f, 1024f)]
        [Tooltip("Disable simulation if viewpoint (main camera or Viewpoint transform set on OceanRenderer component) is more than this distance outside simulation domain.")]
        float _cullDistance = 75.0f;

        [Header("Advanced")]
        [SerializeField]
        DebugSettings _debugSettings = new DebugSettings();

        public float DomainWidth => _domainWidth;
        public RenderTexture RTGroundHeight => _rtGroundHeight;

        RenderTexture _rtH0, _rtH1;
        RenderTexture _rtVx0, _rtVx1;
        RenderTexture _rtVy0, _rtVy1;
        RenderTexture _rtGroundHeight;
        RenderTexture _rtSimulationMask;
        RenderTexture _rtSimulationMask0;

        PropertyWrapperCompute _csSWSProps;

        ComputeShader _csSWS;
        int _krnlInit;
        int _krnlInitGroundHeight;
        int _krnlAdvect;
        int _krnlUpdateH;
        int _krnlHOvershootReduction;
        int _krnlUpdateVels;
        int _krnlBlurH;
        int _krnlBlur;

        float _timeToSimulate = 0f;

        bool _firstUpdate = true;

        public int Resolution => _resolution;
        int _resolution = -1;

        static class ShaderIDs
        {
            // Shader properties.
            public static readonly int s_DomainWidth = Shader.PropertyToID("_DomainWidth");
            public static readonly int s_SimOrigin = Shader.PropertyToID("_SimOrigin");
            public static readonly int s_Resolution = Shader.PropertyToID("_Resolution");
            public static readonly int s_TexelSize = Shader.PropertyToID("_TexelSize");
            public static readonly int s_Time = Shader.PropertyToID("_Time");
            public static readonly int s_DeltaTime = Shader.PropertyToID("_DeltaTime");
            public static readonly int s_AddAdditionalWater = Shader.PropertyToID("_AddAdditionalWater");
            public static readonly int s_DrainWaterAtBoundaries = Shader.PropertyToID("_DrainWaterAtBoundaries");
            public static readonly int s_Friction = Shader.PropertyToID("_Friction");
            public static readonly int s_MaximumVelocity = Shader.PropertyToID("_MaximumVelocity");
            public static readonly int s_ShallowMinDepth = Shader.PropertyToID("_ShallowMinDepth");
            public static readonly int s_ShallowMaxDepth = Shader.PropertyToID("_ShallowMaxDepth");
            public static readonly int s_BlendPushUpStrength = Shader.PropertyToID("_BlendPushUpStrength");
            public static readonly int s_MacCormackAdvection = Shader.PropertyToID("_MacCormackAdvection");
            public static readonly int s_MacCormackAdvectionForHeight = Shader.PropertyToID("_MacCormackAdvectionForHeight");
            public static readonly int s_UpwindHeight = Shader.PropertyToID("_UpwindHeight");
            public static readonly int s_DepthLimiter = Shader.PropertyToID("_DepthLimiter");
            public static readonly int s_OvershootReductionStrength = Shader.PropertyToID("_OvershootReductionStrength");

            // Simulation textures
            public static readonly int s_GroundHeightSS = Shader.PropertyToID("_GroundHeightSS");
            public static readonly int s_GroundHeightSSRW = Shader.PropertyToID("_GroundHeightSSRW");
            public static readonly int s_SimulationMaskRW = Shader.PropertyToID("_SimulationMaskRW");
            public static readonly int s_H0 = Shader.PropertyToID("_H0");
            public static readonly int s_H1 = Shader.PropertyToID("_H1");
            public static readonly int s_Vx0 = Shader.PropertyToID("_Vx0");
            public static readonly int s_Vx1 = Shader.PropertyToID("_Vx1");
            public static readonly int s_Vy0 = Shader.PropertyToID("_Vy0");
            public static readonly int s_Vy1 = Shader.PropertyToID("_Vy1");
            public static readonly int s_SimulationMask = Shader.PropertyToID("_SimulationMask");

            // Shader globals
            public static readonly int s_swsGroundHeight = Shader.PropertyToID("_swsGroundHeight");
            public static readonly int s_swsSimulationMask = Shader.PropertyToID("_swsSimulationMask");
            public static readonly int s_swsH = Shader.PropertyToID("_swsH");
            public static readonly int s_swsHRender = Shader.PropertyToID("_swsHRender");
            public static readonly int s_swsVx = Shader.PropertyToID("_swsVx");
            public static readonly int s_swsVy = Shader.PropertyToID("_swsVy");
        }

        void InitData()
        {
            if (_debugSettings == null)
            {
                _debugSettings = new DebugSettings();
            }

            _resolution = Mathf.CeilToInt(_domainWidth / _texelSize);
            _resolution = Mathf.Min(_resolution, _maximumResolution);

            if (_rtH0 == null) _rtH0 = CreateSWSRT("rtH0");
            if (_rtH1 == null) _rtH1 = CreateSWSRT("rtH1");
            if (_rtVx0 == null) _rtVx0 = CreateSWSRT("rtVx0", true);
            if (_rtVx1 == null) _rtVx1 = CreateSWSRT("rtVx1", true);
            if (_rtVy0 == null) _rtVy0 = CreateSWSRT("rtVy0", true);
            if (_rtVy1 == null) _rtVy1 = CreateSWSRT("rtVy1", true);
            if (_rtGroundHeight == null) _rtGroundHeight = CreateSWSRT("rtGroundHeight");
            if (_rtSimulationMask == null) _rtSimulationMask = CreateSWSRT("rtSimulationMask");
            if (_rtSimulationMask0 == null) _rtSimulationMask0 = CreateSWSRT("rtSimulationMask0");
        }

        void InitSim(CommandBuffer buf)
        {
            // Init sim data - water heights and velocities
            {
                _csSWSProps.Initialise(buf, _csSWS, _krnlInit);

                _csSWSProps.SetTexture(ShaderIDs.s_GroundHeightSS, _rtGroundHeight);
                _csSWSProps.SetTexture(ShaderIDs.s_H0, _rtH0);
                _csSWSProps.SetTexture(ShaderIDs.s_H1, _rtH1);
                _csSWSProps.SetTexture(ShaderIDs.s_Vx0, _rtVx0);
                _csSWSProps.SetTexture(ShaderIDs.s_Vx1, _rtVx1);
                _csSWSProps.SetTexture(ShaderIDs.s_Vy0, _rtVy0);
                _csSWSProps.SetTexture(ShaderIDs.s_Vy1, _rtVy1);

                _csSWSProps.SetFloat(ShaderIDs.s_Time, Time.time);
                _csSWSProps.SetFloat(ShaderIDs.s_DeltaTime, _simulationTimeStep);
                _csSWSProps.SetFloat(ShaderIDs.s_DomainWidth, _domainWidth);
                _csSWSProps.SetFloat(ShaderIDs.s_Resolution, _resolution);
                _csSWSProps.SetFloat(ShaderIDs.s_TexelSize, _texelSize);
                _csSWSProps.SetFloat(ShaderIDs.s_AddAdditionalWater, Mathf.Max(0f, _debugSettings._addAdditionalWater));
                _csSWSProps.SetVector(ShaderIDs.s_SimOrigin, SimOrigin());
                _csSWSProps.SetVector(OceanRenderer.sp_oceanCenterPosWorld, OceanRenderer.Instance.Root.position);

                buf.DispatchCompute(_csSWS, _krnlInit, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
            }
        }

        void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        // Currently sim will be placed _depth below global sea level
        public Vector3 SimOrigin()
        {
            var result = transform.position;

            // Always place at sea level. Would be nice to use this for rivers and such but that will
            // require more work. Also varying sea levels complicate sim setup.
            //if (_placeSimAtGlobalSeaLevel)
            {
                result.y = OceanRenderer.Instance.transform.position.y;
            }

            // Sim origin is at 'bottom' of domain, water height/ground height are added
            result.y -= _waterDepth;

            return result;
        }


        public void CrestUpdate(CommandBuffer buf)
        {
        }

        public void CrestUpdatePostCombine(CommandBuffer buf)
        {
            buf.BeginSample("SWS");

            var ocean = OceanRenderer.Instance;
            if (ocean == null) return;

            // NOTE: Initialisation of everything happens in update because it requires Crest to be initialised for
            // sea floor depth and numerous other state.
            if (_firstUpdate)
            {
                InitData();

                PopulateGroundHeight(buf);

                InitSim(buf);

                _firstUpdate = false;
            }

            // Distance culling
            var doUpdate = _debugSettings._doUpdate;
            if (_enableDistanceCulling)
            {
                var cullRadius = _domainWidth / 2f + _cullDistance;
                var offset = ocean.Viewpoint.position - transform.position;
                doUpdate = doUpdate && (offset.sqrMagnitude < cullRadius * cullRadius);
            }

            if (doUpdate)
            {
                // Set once per frame stuff
                {
                    // Init prop wrapper. Use any kernel as the below value types don't require a kernel.
                    _csSWSProps.Initialise(buf, _csSWS, 0);

                    _csSWSProps.SetFloat(ShaderIDs.s_Time, Time.time);
                    _csSWSProps.SetFloat(ShaderIDs.s_DeltaTime, _simulationTimeStep);
                    _csSWSProps.SetFloat(ShaderIDs.s_DomainWidth, _domainWidth);
                    _csSWSProps.SetFloat(ShaderIDs.s_Resolution, _resolution);
                    _csSWSProps.SetFloat(ShaderIDs.s_DrainWaterAtBoundaries, _drainWaterAtBoundaries);
                    _csSWSProps.SetFloat(ShaderIDs.s_Friction, _friction);
                    _csSWSProps.SetFloat(ShaderIDs.s_MaximumVelocity, _courantNumber * (_domainWidth / _resolution) / _simulationTimeStep);
                    _csSWSProps.SetFloat(ShaderIDs.s_TexelSize, _texelSize);
                    _csSWSProps.SetFloat(ShaderIDs.s_ShallowMinDepth, _blendShallowMinDepth);
                    _csSWSProps.SetFloat(ShaderIDs.s_ShallowMaxDepth, _blendShallowMaxDepth);
                    _csSWSProps.SetFloat(ShaderIDs.s_BlendPushUpStrength, _blendPushUpStrength);
                    _csSWSProps.SetVector(ShaderIDs.s_SimOrigin, SimOrigin());
                    _csSWSProps.SetVector(OceanRenderer.sp_oceanCenterPosWorld, ocean.Root.position);

                    _csSWSProps.SetInt(ShaderIDs.s_MacCormackAdvection, (_debugSettings._enableStabilityImprovements && _debugSettings._macCormackScheme) ? 1 : 0);
                    _csSWSProps.SetInt(ShaderIDs.s_MacCormackAdvectionForHeight, (_debugSettings._enableStabilityImprovements && _debugSettings._macCormackSchemeForHeight) ? 1 : 0);
                    _csSWSProps.SetInt(ShaderIDs.s_UpwindHeight, (_debugSettings._enableStabilityImprovements && _debugSettings._upwindHeight) ? 1 : 0);
                    _csSWSProps.SetInt(ShaderIDs.s_DepthLimiter, (_debugSettings._enableStabilityImprovements && _debugSettings._depthLimiter) ? 1 : 0);
                    _csSWSProps.SetFloat(ShaderIDs.s_OvershootReductionStrength, _debugSettings._overshootReductionStrength);

                    _matInjectSWSAnimWaves.SetFloat(ShaderIDs.s_DomainWidth, _domainWidth);
                    _matInjectSWSFlow.SetFloat(ShaderIDs.s_DomainWidth, _domainWidth);
                    _matInjectSWSFoam.SetFloat(ShaderIDs.s_DomainWidth, _domainWidth);

                    var simOrigin = SimOrigin();
                    _matInjectSWSAnimWaves.SetVector(ShaderIDs.s_SimOrigin, simOrigin);
                    _matInjectSWSFlow.SetVector(ShaderIDs.s_SimOrigin, simOrigin);
                    _matInjectSWSFoam.SetVector(ShaderIDs.s_SimOrigin, simOrigin);

                    _matInjectSWSFoam.SetFloat(ShaderIDs.s_Resolution, _resolution);
                }

                if (_allowDynamicSeabed)
                {
                    // Populate ground height every frame to allow dynamic scene
                    PopulateGroundHeight(buf);
                }

                // Safety first
                _simulationTimeStep = Mathf.Max(_simulationTimeStep, 0.001f);

                // Compute substeps
                _timeToSimulate += ocean.DeltaTime;
                int steps = _timeToSimulate > 0f ? Mathf.CeilToInt(_timeToSimulate / _simulationTimeStep) : 0;
                _timeToSimulate -= steps * _simulationTimeStep;

                for (int i = 0; i < steps; i++)
                {
                    // Each stage block should leave latest state in '1' buffer (H1, Vx1, Vy1)

                    // Advect
                    if (_debugSettings._doAdvect)
                    {
                        Swap(ref _rtVx0, ref _rtVx1);
                        Swap(ref _rtVy0, ref _rtVy1);
                        if (_debugSettings._advectHeights)
                        {
                            Swap(ref _rtH0, ref _rtH1);
                        }

                        _csSWSProps.Initialise(buf, _csSWS, _krnlAdvect);

                        _csSWSProps.SetTexture(ShaderIDs.s_H0, _rtH0);
                        _csSWSProps.SetTexture(ShaderIDs.s_H1, _rtH1);
                        _csSWSProps.SetTexture(ShaderIDs.s_Vx0, _rtVx0);
                        _csSWSProps.SetTexture(ShaderIDs.s_Vx1, _rtVx1);
                        _csSWSProps.SetTexture(ShaderIDs.s_Vy0, _rtVy0);
                        _csSWSProps.SetTexture(ShaderIDs.s_Vy1, _rtVy1);

                        buf.DispatchCompute(_csSWS, _krnlAdvect, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, _debugSettings._advectHeights ? 3 : 2);
                    }

                    // Update H
                    if (_debugSettings._doUpdateH)
                    {
                        Swap(ref _rtH0, ref _rtH1);

                        _csSWSProps.Initialise(buf, _csSWS, _krnlUpdateH);

                        _csSWSProps.SetTexture(ShaderIDs.s_H0, _rtH0);
                        _csSWSProps.SetTexture(ShaderIDs.s_H1, _rtH1);
                        _csSWSProps.SetTexture(ShaderIDs.s_Vx1, _rtVx1);
                        _csSWSProps.SetTexture(ShaderIDs.s_Vy1, _rtVy1);
                        _csSWSProps.SetTexture(ShaderIDs.s_SimulationMask, _rtSimulationMask);
                        _csSWSProps.SetTexture(ShaderIDs.s_GroundHeightSS, _rtGroundHeight);
                        LodDataMgrAnimWaves.Bind(_csSWSProps);
                        // TODO use sea level offset
                        //LodDataMgrSeaFloorDepth.Bind(_csSWSProps);

                        buf.DispatchCompute(_csSWS, _krnlUpdateH, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
                    }

                    // H overshoot reduction
                    if (_debugSettings._overshootReduction)
                    {
                        Swap(ref _rtH0, ref _rtH1);

                        _csSWSProps.Initialise(buf, _csSWS, _krnlHOvershootReduction);

                        _csSWSProps.SetTexture(ShaderIDs.s_H0, _rtH0);
                        _csSWSProps.SetTexture(ShaderIDs.s_H1, _rtH1);
                        _csSWSProps.SetTexture(ShaderIDs.s_GroundHeightSS, _rtGroundHeight);

                        buf.DispatchCompute(_csSWS, _krnlHOvershootReduction, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
                    }

                    // Update vels
                    if (_debugSettings._doUpdateVels)
                    {
                        _csSWSProps.Initialise(buf, _csSWS, _krnlUpdateVels);

                        _csSWSProps.SetTexture(ShaderIDs.s_H1, _rtH1);
                        _csSWSProps.SetTexture(ShaderIDs.s_Vx1, _rtVx1);
                        _csSWSProps.SetTexture(ShaderIDs.s_Vy1, _rtVy1);
                        _csSWSProps.SetTexture(ShaderIDs.s_GroundHeightSS, _rtGroundHeight);

                        buf.DispatchCompute(_csSWS, _krnlUpdateVels, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);

                        // This is important. Without this, there is aliasing in the injected vels which, in collaboration
                        // with the flow in the combine pass which has large period, makes annoying pops. Perhaps an
                        // alternative would be to just not add flow from SWS to big cascades. I tried something like this
                        // and it did not seem to help for me, so going with this.
                        buf.GenerateMips(_rtVx1);
                        buf.GenerateMips(_rtVy1);
                    }
                }

                // Blur H postprocess to smooth out render data - only needs to be done once after any simulation updates
                if (steps > 0 && _debugSettings._blurShapeForRender)
                {
                    // Cheekily write to H0, but dont flip. This is a temporary result purely for rendering.
                    // Next update will flip and overwrite this.
                    //Swap(ref _rtH0, ref _rtH1);

                    _csSWSProps.Initialise(buf, _csSWS, _krnlBlurH);

                    _csSWSProps.SetTexture(ShaderIDs.s_H0, _rtH1);
                    _csSWSProps.SetTexture(ShaderIDs.s_H1, _rtH0);
                    _csSWSProps.SetTexture(ShaderIDs.s_GroundHeightSS, _rtGroundHeight);

                    buf.DispatchCompute(_csSWS, _krnlBlurH, (_rtH0.width + 7) / 8, (_rtH0.height + 7) / 8, 1);
                }
            }

            Shader.SetGlobalTexture(ShaderIDs.s_swsGroundHeight, _rtGroundHeight);
            Shader.SetGlobalTexture(ShaderIDs.s_swsSimulationMask, _rtSimulationMask);
            Shader.SetGlobalTexture(ShaderIDs.s_swsH, _rtH1);
            // If blurring is enabled, apply the blurred height which was put into H0 until next frame overwrites
            Shader.SetGlobalTexture(ShaderIDs.s_swsHRender, _debugSettings._blurShapeForRender ? _rtH0 : _rtH1);
            Shader.SetGlobalTexture(ShaderIDs.s_swsVx, _rtVx1);
            Shader.SetGlobalTexture(ShaderIDs.s_swsVy, _rtVy1);

            buf.EndSample("SWS");
        }
    }

    // Separate helpers/glue/initialisation/etc
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Shallow Water Simulation")]
    public partial class ShallowWaterSimulation : MonoBehaviour, ILodDataInput
    {
        //[SerializeField] bool _updateInEditMode = false;

        Material _matInjectSWSAnimWaves;
        Material _matInjectSWSFlow;
        // It currently does not trigger jacobian foam term. If we added displacement to SWS waves it would prob help, but
        // the quality of the shape would have to be much better i guess!
        Material _matInjectSWSFoam;

        // Draw to all LODs
        public float Wavelength => 0f;
        public bool Enabled => true;

        [System.Serializable]
        class DebugSettings
        {
            [Header("Simulation Stages")]
            public bool _doUpdate = true;
            public bool _doAdvect = true;
            public bool _advectHeights = true;
            public bool _doUpdateH = true;
            public bool _doUpdateVels = true;

            [Header("Stability Improvements")]
            public bool _enableStabilityImprovements = true;
            public bool _macCormackScheme = false;
            public bool _macCormackSchemeForHeight = false;
            public bool _upwindHeight = true;
            public bool _depthLimiter = true;
            public bool _overshootReduction = true;
            [UnityEngine.Range(0.1f, 0.5f)]
            public float _overshootReductionStrength = 0.25f;

            [Header("Quality Improvements")]
            [Tooltip("Filters the shape prior to rendering to smooth out sharp features.")]
            public bool _blurShapeForRender = true;
            [UnityEngine.Range(0, 64)]
            public int _blurSimulationMaskIterations = 10;

            [Header("Output (editor only)")]
            [Tooltip("Add the resulting shape to the water system.")]
            public bool _injectShape = true;
            [Tooltip("Add the resulting flow velocities to the water system.")]
            public bool _injectFlow = true;
            [Tooltip("Add the resulting foam to the water system.")]
            public bool _injectFoam = true;

            [Header("Debug Overlay")]
            public bool _showSimulationData = false;

            [Header("Simulation")]
            [Tooltip("Adds additional water into the simulation domain on initialisation (m).")]
            public float _addAdditionalWater = 0f;
        }

        void OnEnable()
        {
            if (_csSWS == null)
            {
                _csSWS = ComputeShaderHelpers.LoadShader("UpdateSWS");
                _csSWSProps = new PropertyWrapperCompute();

                _krnlInit = _csSWS.FindKernel("Init");
                _krnlInitGroundHeight = _csSWS.FindKernel("InitGroundHeight");
                _krnlAdvect = _csSWS.FindKernel("Advect");
                _krnlUpdateH = _csSWS.FindKernel("UpdateH");
                _krnlHOvershootReduction = _csSWS.FindKernel("HOvershootReduction");
                _krnlUpdateVels = _csSWS.FindKernel("UpdateVels");
                _krnlBlurH = _csSWS.FindKernel("BlurH");
                _krnlBlur = _csSWS.FindKernel("Blur");
            }

            {
                _matInjectSWSAnimWaves = new Material(Shader.Find("Hidden/Crest/Inputs/Animated Waves/Inject SWS"));
                _matInjectSWSAnimWaves.hideFlags = HideFlags.HideAndDontSave;
                _matInjectSWSAnimWaves.SetFloat(RegisterLodDataInputBase.sp_Weight, 1f);
            }
            {
                _matInjectSWSFlow = new Material(Shader.Find("Hidden/Crest/Inputs/Flow/Inject SWS"));
                _matInjectSWSFlow.hideFlags = HideFlags.HideAndDontSave;
                _matInjectSWSFlow.SetFloat(RegisterLodDataInputBase.sp_Weight, 1f);
            }
            {
                _matInjectSWSFoam = new Material(Shader.Find("Hidden/Crest/Inputs/Foam/Inject SWS"));
                _matInjectSWSFoam.hideFlags = HideFlags.HideAndDontSave;
                _matInjectSWSFoam.SetFloat(RegisterLodDataInputBase.sp_Weight, 1f);
            }

            LodDataMgrAnimWaves.RegisterUpdatable(this);

            // Register shape
            {
                var registrar = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrAnimWaves));
                registrar.Remove(this);
                registrar.Add(0, this);
            }

            // Register flow
            {
                var registrar = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrFlow));
                registrar.Remove(this);
                registrar.Add(0, this);
            }

            // Register foam
            {
                var registrar = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrFoam));
                registrar.Remove(this);
                registrar.Add(0, this);
            }
        }

        void OnDisable()
        {
            LodDataMgrAnimWaves.DeregisterUpdatable(this);
        }

        RenderTexture CreateSWSRT(string name, bool withMips = false)
        {
            var result = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.RFloat);
            result.name = name;

            result.enableRandomWrite = true;

            if (withMips)
            {
                result.useMipMap = true;
                result.autoGenerateMips = false;
            }

            result.Create();

            return result;
        }

        public void Reset()
        {
            _firstUpdate = true;
        }

        void PopulateGroundHeight(CommandBuffer buf)
        {
            _csSWSProps.Initialise(buf, _csSWS, _krnlInitGroundHeight);
            _csSWSProps.SetTexture(ShaderIDs.s_GroundHeightSSRW, _rtGroundHeight);
            _csSWSProps.SetTexture(ShaderIDs.s_SimulationMaskRW, _rtSimulationMask);
            _csSWSProps.SetVector(OceanRenderer.sp_oceanCenterPosWorld, OceanRenderer.Instance.Root.position);
            _csSWSProps.SetVector(ShaderIDs.s_SimOrigin, SimOrigin());
            _csSWSProps.SetBuffer(OceanRenderer.sp_cascadeData, OceanRenderer.Instance._bufCascadeDataTgt);
            _csSWSProps.SetFloat(ShaderIDs.s_ShallowMinDepth, _blendShallowMinDepth);
            _csSWSProps.SetFloat(ShaderIDs.s_ShallowMaxDepth, _blendShallowMaxDepth);
            _csSWSProps.SetFloat(OceanRenderer.sp_sliceCount, OceanRenderer.Instance.CurrentLodCount);

            // TODO extract this out i guess
            // LOD 0 is blended in/out when scale changes, to eliminate pops. Here we set it as a global, whereas in OceanChunkRenderer it
            // is applied to LOD0 tiles only through instance data. This global can be used in compute, where we only apply this factor for slice 0.
            var needToBlendOutShape = OceanRenderer.Instance.ScaleCouldIncrease;
            var meshScaleLerp = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 0f;
            _csSWSProps.SetFloat(OceanRenderer.sp_meshScaleLerp, meshScaleLerp);

            LodDataMgrSeaFloorDepth.Bind(_csSWSProps);

            buf.DispatchCompute(_csSWS, _krnlInitGroundHeight, (_rtGroundHeight.width + 7) / 8, (_rtGroundHeight.height + 7) / 8, 1);

            // Blur simulation mask
            for (int i = 0; i < _debugSettings._blurSimulationMaskIterations; i++)
            {
                Swap(ref _rtSimulationMask0, ref _rtSimulationMask);
                _csSWSProps.Initialise(buf, _csSWS, _krnlBlur);
                _csSWSProps.SetTexture(ShaderIDs.s_H0, _rtSimulationMask0);
                _csSWSProps.SetTexture(ShaderIDs.s_H1, _rtSimulationMask);
                buf.DispatchCompute(_csSWS, _krnlBlur, (_rtSimulationMask.width + 7) / 8, (_rtSimulationMask.height + 7) / 8, 1);
            }
        }

        public void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            //if (!gameObject || !gameObject.activeInHierarchy || !enabled) return;
            buf.SetGlobalInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);

            Material injectionMat;
            if (lodData is LodDataMgrAnimWaves)
            {
                if (!_debugSettings._injectShape)
                {
                    return;
                }
                injectionMat = _matInjectSWSAnimWaves;
            }
            else if (lodData is LodDataMgrFlow)
            {
                if (!_debugSettings._injectFlow)
                {
                    return;
                }
                injectionMat = _matInjectSWSFlow;
            }
            else
            {
                if (!_debugSettings._injectFoam)
                {
                    return;
                }
                injectionMat = _matInjectSWSFoam;
            }

            buf.DrawProcedural(Matrix4x4.identity, injectionMat, 0, MeshTopology.Triangles, 3);
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (_debugSettings._showSimulationData && _rtH1 != null)
            {
                var s = 200f;
                var y = 0f;
                Rect r;

                r = new Rect(200, y, s, s); y += s + 1;
                GUI.DrawTexture(r, _rtH1); GUI.Label(r, "_rtH1");

                r = new Rect(200, y, s, s); y += s + 1;
                GUI.DrawTexture(r, _rtVx1); GUI.Label(r, "_rtVx1");

                r = new Rect(200, y, s, s); y += s + 1;
                GUI.DrawTexture(r, _rtVy1); GUI.Label(r, "_rtVy1");

                r = new Rect(200, y, s, s); y += s + 1;
                GUI.DrawTexture(r, _rtGroundHeight); GUI.Label(r, "_rtGroundHeight");

                r = new Rect(200, y, s, s); y += s + 1;
                GUI.DrawTexture(r, _rtSimulationMask); GUI.Label(r, "_rtSimulationMask");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pos = SimOrigin();

            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.Translate(pos) * Matrix4x4.Scale(new Vector3(_domainWidth, 1f, _domainWidth));
            Gizmos.color = new Color(1f, 0f, 1f, 1f);

            // Draw a rectangle at the sim base height
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0f, 1f));
            Gizmos.matrix = oldMatrix;
        }
#endif
    }

    // Validation
    public partial class ShallowWaterSimulation : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            if (ocean == null) return true;

            var isValid = true;

            // Ensure Flow is enabled
            if (!OceanRenderer.ValidateFeatureEnabled(ocean, showMessage, ocean => ocean.CreateFlowSim,
                    LodDataMgrFlow.FEATURE_TOGGLE_LABEL, LodDataMgrFlow.FEATURE_TOGGLE_NAME, LodDataMgrFlow.MATERIAL_KEYWORD, LodDataMgrFlow.MATERIAL_KEYWORD_PROPERTY,
                    LodDataMgrFlow.ERROR_MATERIAL_KEYWORD_MISSING, LodDataMgrFlow.ERROR_MATERIAL_KEYWORD_MISSING_FIX))
            {
                isValid = false;
            }

            // Other features which usually contribute on but not validated:
            // - Foam - this is on by default and if it's off it is obvious that it is off
            // - AnimWaves - always on
            // - Depths - actually this can work without terrain depths, like for a "swimming" pool case where sea floor is trivially flat.

            if (_debugSettings != null)
            {
                if (!_debugSettings._doUpdate || !_debugSettings._doAdvect || !_debugSettings._doUpdateH || !_debugSettings._doUpdateVels)
                {
                    showMessage
                    (
                        "Debug options currently disable one or more stages of the simulation.",
                        "Enable all simulation stages.",
                        ValidatedHelper.MessageType.Warning, this,
                        (so) =>
                        {
                            OceanRenderer.FixSetFeatureEnabled(so, "_debugSettings._doUpdate", true);
                            OceanRenderer.FixSetFeatureEnabled(so, "_debugSettings._doAdvect", true);
                            OceanRenderer.FixSetFeatureEnabled(so, "_debugSettings._doUpdateH", true);
                            OceanRenderer.FixSetFeatureEnabled(so, "_debugSettings._doUpdateVels", true);
                        }
                    );
                }

                if (_debugSettings._showSimulationData)
                {
                    showMessage
                    (
                        "Debug drawing of simulation data currently active.",
                        "Disable debug drawing when debugging is done to hide the overlay.",
                        ValidatedHelper.MessageType.Info, this,
                        (so) => OceanRenderer.FixSetFeatureEnabled(so, "_debugSettings._showSimulationData", false)
                    );
                }

                if (!EditorApplication.isPlaying)
                {
                    showMessage
                    (
                        "<i>Shallow Water Simulation</i> only works in Play Mode.",
                        "Enter Play Mode to see simulation running.",
                        ValidatedHelper.MessageType.Info
                    );
                }
            }

            return isValid;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ShallowWaterSimulation))]
    class ShallowWaterSimulationEditor : ValidatedEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var target = this.target as ShallowWaterSimulation;

            GUILayout.Label($"Resolution: {target.Resolution}");

            if (GUILayout.Button("Reset"))
            {
                target.Reset();
            }
        }
    }
#endif
}
