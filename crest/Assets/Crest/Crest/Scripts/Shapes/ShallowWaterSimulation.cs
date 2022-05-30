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
    public partial class ShallowWaterSimulation : MonoBehaviour, LodDataMgrAnimWaves.IShapeUpdatable
    {
        [Header("Settings")]
        [SerializeField] float _depth = 2f;
        [SerializeField] float _addAdditionalWater = 0f;
        [SerializeField, UnityEngine.Range(0.01f, 2f)] float _texelSize = 32f / 512f;
        [SerializeField, UnityEngine.Range(16, 1024)] int _maxResolution = 1024;
        [SerializeField, UnityEngine.Range(8, 128)] float _domainWidth = 32f;
        [SerializeField] float _drain = -0.0001f;

        [Header("Sim Settings")]
        [SerializeField] float _friction = 0.001f;
        [SerializeField] float _maxVel = 100.0f;

        [Header("Blending With Waves")]
        [SerializeField, UnityEngine.Range(-10f, 10f)] float _blendShallowMinDepth = 0f;
        [SerializeField, UnityEngine.Range(-10f, 10f)] float _blendShallowMaxDepth = 4f;
        [SerializeField, UnityEngine.Range(0f, 1f)] float _blendPushUpStrength = 0.1f;

        [Header("Advanced")]
        [SerializeField] DebugSettings _debugSettings = new DebugSettings();

        RenderTexture _rtH0, _rtH1;
        RenderTexture _rtVx0, _rtVx1;
        RenderTexture _rtVy0, _rtVy1;
        RenderTexture _rtGroundHeight;
        RenderTexture _rtSimulationMask;

        PropertyWrapperCompute _csSWSProps;

        ComputeShader _csSWS;
        int _krnlInit;
        int _krnlInitGroundHeight;
        int _krnlAdvect;
        int _krnlUpdateH;
        int _krnlUpdateVels;
        int _krnlBlurH;

        float _timeToSimulate = 0f;

        bool _firstUpdate = true;

        public int Resolution => _resolution;
        int _resolution = -1;

        void InitData()
        {
            if (_debugSettings == null)
            {
                _debugSettings = new DebugSettings();
            }

            _resolution = Mathf.CeilToInt(_domainWidth / _texelSize);
            _resolution = Mathf.Min(_resolution, _maxResolution);

            if (_rtH0 == null) _rtH0 = CreateSWSRT();
            if (_rtH1 == null) _rtH1 = CreateSWSRT();
            if (_rtVx0 == null) _rtVx0 = CreateSWSRT(true);
            if (_rtVx1 == null) _rtVx1 = CreateSWSRT(true);
            if (_rtVy0 == null) _rtVy0 = CreateSWSRT(true);
            if (_rtVy1 == null) _rtVy1 = CreateSWSRT(true);
            if (_rtGroundHeight == null) _rtGroundHeight = CreateSWSRT();
            if (_rtSimulationMask == null) _rtSimulationMask = CreateSWSRT();

            _matInjectSWSAnimWaves.SetFloat(Shader.PropertyToID("_DomainWidth"), _domainWidth);
            _matInjectSWSFlow.SetFloat(Shader.PropertyToID("_DomainWidth"), _domainWidth);
            _matInjectSWSFoam.SetFloat(Shader.PropertyToID("_DomainWidth"), _domainWidth);

            var simOrigin = SimOrigin();
            _matInjectSWSAnimWaves.SetVector(Shader.PropertyToID("_SimOrigin"), simOrigin);
            _matInjectSWSFlow.SetVector(Shader.PropertyToID("_SimOrigin"), simOrigin);
            _matInjectSWSFoam.SetVector(Shader.PropertyToID("_SimOrigin"), simOrigin);

            _matInjectSWSFoam.SetFloat(Shader.PropertyToID("_Resolution"), _resolution);
        }

        void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        // Currently sim will be placed _depth below global sea level
        Vector3 SimOrigin()
        {
            var result = transform.position;

            // Always place at sea level. Would be nice to use this for rivers and such but that will
            // require more work. Also varying sea levels complicate sim setup.
            //if (_placeSimAtGlobalSeaLevel)
            {
                result.y = OceanRenderer.Instance.transform.position.y;
            }

            // Sim origin is at 'bottom' of domain, water height/ground height are added
            result.y -= _depth;

            return result;
        }


        public void CrestUpdate(CommandBuffer buf)
        {
        }

        public void CrestUpdatePostCombine(CommandBuffer buf)
        {
            if (_firstUpdate)
            {
                Reset(buf);

                _firstUpdate = false;
            }

            InitData();

            if (_debugSettings._doUpdate)
            {
                _timeToSimulate += Time.deltaTime;

                // Populate ground height every frame to allow dynamic scene
                PopulateGroundHeight(buf);

                float fixedDt = 0.01f;
                int steps = _timeToSimulate > 0f ? Mathf.CeilToInt(_timeToSimulate / fixedDt) : 0;
                _timeToSimulate -= steps * fixedDt;

                for (int i = 0; i < steps; i++)
                {
                    // Each stage block should leave latest state in '1' buffer (H1, Vx1, Vy1)

                    _csSWSProps.SetFloat(Shader.PropertyToID("_Time"), Time.time);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_DomainWidth"), _domainWidth);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_Res"), _resolution);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_Drain"), _drain);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_Friction"), _friction);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_MaxVel"), _maxVel);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_TexelSize"), _texelSize);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_ShallowMinDepth"), _blendShallowMinDepth);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_ShallowMaxDepth"), _blendShallowMaxDepth);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_BlendPushUpStrength"), _blendPushUpStrength);
                    _csSWSProps.SetVector(Shader.PropertyToID("_SimOrigin"), SimOrigin());
                    _csSWSProps.SetVector(Shader.PropertyToID("_OceanCenterPosWorld"), OceanRenderer.Instance.transform.position);

                    // Advect
                    if (_debugSettings._doAdvect)
                    {
                        Swap(ref _rtH0, ref _rtH1);
                        Swap(ref _rtVx0, ref _rtVx1);
                        Swap(ref _rtVy0, ref _rtVy1);

                        _csSWSProps.Initialise(buf, _csSWS, _krnlAdvect);

                        _csSWSProps.SetTexture(Shader.PropertyToID("_H0"), _rtH0);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_Vx0"), _rtVx0);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_Vx1"), _rtVx1);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_Vy0"), _rtVy0);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_Vy1"), _rtVy1);

                        buf.DispatchCompute(_csSWS, _krnlAdvect, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
                    }

                    // Update H
                    if (_debugSettings._doUpdateH)
                    {
                        _csSWSProps.Initialise(buf, _csSWS, _krnlUpdateH);

                        _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_Vx1"), _rtVx1);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_Vy1"), _rtVy1);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_SimulationMask"), _rtSimulationMask);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_GroundHeightSS"), _rtGroundHeight);
                        LodDataMgrAnimWaves.Bind(_csSWSProps);

                        buf.DispatchCompute(_csSWS, _krnlUpdateH, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
                    }

                    // Update vels
                    if (_debugSettings._doUpdateVels)
                    {
                        _csSWSProps.Initialise(buf, _csSWS, _krnlUpdateVels);

                        _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_Vx1"), _rtVx1);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_Vy1"), _rtVy1);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_GroundHeightSS"), _rtGroundHeight);

                        buf.DispatchCompute(_csSWS, _krnlUpdateVels, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);

                        // This is important. Without this, there is aliasing in the injected vels which, in collaboration
                        // with the flow in the combine pass which has large period, makes annoying pops. Perhaps an
                        // alternative would be to just not add flow from SWS to big cascades. I tried something like this
                        // and it did not seem to help for me, so going with this.
                        buf.GenerateMips(_rtVx1);
                        buf.GenerateMips(_rtVy1);
                    }

                    // Blur H
                    if (_debugSettings._blurShapeForRender)
                    {
                        // Cheekily write to H0, but dont flip. This is a temporary result purely for rendering.
                        // Next update will flip and overwrite this.
                        //Swap(ref _rtH0, ref _rtH1);

                        _csSWSProps.Initialise(buf, _csSWS, _krnlBlurH);

                        _csSWSProps.SetTexture(Shader.PropertyToID("_H0"), _rtH1);
                        _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH0);

                        buf.DispatchCompute(_csSWS, _krnlBlurH, (_rtH0.width + 7) / 8, (_rtH0.height + 7) / 8, 1);
                    }
                }
            }

            Shader.SetGlobalTexture("_swsGroundHeight", _rtGroundHeight);
            Shader.SetGlobalTexture("_swsSimulationMask", _rtSimulationMask);
            Shader.SetGlobalTexture("_swsH", _rtH1);
            // If blurring is enabled, apply the blurred height which was put into H0 until next frame overwrites
            Shader.SetGlobalTexture("_swsHRender", _debugSettings._blurShapeForRender ? _rtH0 : _rtH1);
            Shader.SetGlobalTexture("_swsVx", _rtVx1);
            Shader.SetGlobalTexture("_swsVy", _rtVy1);
        }
    }

    // Separate helpers/glue/initialisation/etc
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
            public bool _doUpdateH = true;
            public bool _doUpdateVels = true;

            [Header("Output (editor only)")]
            public bool _injectShape = true;
            public bool _injectFlow = true;
            public bool _injectFoam = true;
            public bool _blurShapeForRender = true;

            [Header("Overlay")]
            public bool _showSimulationData = false;
        }

        void OnEnable()
        {
            if (_csSWS == null)
            {
                _csSWS = ComputeShaderHelpers.LoadShader("SWSUpdate");
                _csSWSProps = new PropertyWrapperCompute();

                _krnlInit = _csSWS.FindKernel("Init");
                _krnlInitGroundHeight = _csSWS.FindKernel("InitGroundHeight");
                _krnlAdvect = _csSWS.FindKernel("Advect");
                _krnlUpdateH = _csSWS.FindKernel("UpdateH");
                _krnlUpdateVels = _csSWS.FindKernel("UpdateVels");
                _krnlBlurH = _csSWS.FindKernel("BlurH");
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

        RenderTexture CreateSWSRT(bool withMips = false)
        {
            var result = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.RFloat);

            result.enableRandomWrite = true;

            if (withMips)
            {
                result.useMipMap = true;
                result.autoGenerateMips = false;
            }

            result.Create();

            return result;
        }

        public void Reset(CommandBuffer buf)
        {
            _rtH0 = _rtH1 = _rtVx0 = _rtVx1 = _rtVy0 = _rtVy1 = null;

            InitData();

            // Populate ground height - used for initial water height calculation
            PopulateGroundHeight(buf);

            // Init sim data - water heights and velocities
            {
                _csSWSProps.Initialise(buf, _csSWS, _krnlInit);

                _csSWSProps.SetTexture(Shader.PropertyToID("_GroundHeightSS"), _rtGroundHeight);
                _csSWSProps.SetTexture(Shader.PropertyToID("_SimulationMaskRW"), _rtSimulationMask);
                _csSWSProps.SetTexture(Shader.PropertyToID("_H0"), _rtH0);
                _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);
                _csSWSProps.SetTexture(Shader.PropertyToID("_Vx0"), _rtVx0);
                _csSWSProps.SetTexture(Shader.PropertyToID("_Vx1"), _rtVx1);
                _csSWSProps.SetTexture(Shader.PropertyToID("_Vy0"), _rtVy0);
                _csSWSProps.SetTexture(Shader.PropertyToID("_Vy1"), _rtVy1);

                _csSWSProps.SetFloat(Shader.PropertyToID("_Time"), Time.time);
                _csSWSProps.SetFloat(Shader.PropertyToID("_DomainWidth"), _domainWidth);
                _csSWSProps.SetFloat(Shader.PropertyToID("_Res"), _resolution);
                _csSWSProps.SetFloat(Shader.PropertyToID("_TexelSize"), _texelSize);
                _csSWSProps.SetFloat(Shader.PropertyToID("_AddAdditionalWater"), _addAdditionalWater);
                _csSWSProps.SetVector(Shader.PropertyToID("_SimOrigin"), SimOrigin());
                _csSWSProps.SetVector(Shader.PropertyToID("_OceanCenterPosWorld"), OceanRenderer.Instance.transform.position);

                buf.DispatchCompute(_csSWS, _krnlInit, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
            }
        }

        void PopulateGroundHeight(CommandBuffer buf)
        {
            _csSWSProps.Initialise(buf, _csSWS, _krnlInitGroundHeight);
            _csSWSProps.SetTexture(Shader.PropertyToID("_GroundHeightSSRW"), _rtGroundHeight);
            _csSWSProps.SetTexture(Shader.PropertyToID("_SimulationMaskRW"), _rtSimulationMask);

            LodDataMgrSeaFloorDepth.Bind(_csSWSProps);

            buf.DispatchCompute(_csSWS, _krnlInitGroundHeight, (_rtGroundHeight.width + 7) / 8, (_rtGroundHeight.height + 7) / 8, 1);
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
            if (_debugSettings._showSimulationData)
            {
                var s = 200f;
                var y = 0f;
                Rect r;

                r = new Rect(200, y, s, s); y += s;
                GUI.DrawTexture(r, _rtH1); GUI.Label(r, "_rtH1");

                r = new Rect(200, y, s, s); y += s;
                GUI.DrawTexture(r, _rtVx1); GUI.Label(r, "_rtVx1");

                r = new Rect(200, y, s, s); y += s;
                GUI.DrawTexture(r, _rtVy1); GUI.Label(r, "_rtVy1");

                r = new Rect(200, y, s, s); y += s;
                GUI.DrawTexture(r, _rtGroundHeight); GUI.Label(r, "_rtGroundHeight");

                r = new Rect(200, y, s, s); y += s;
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

    public partial class ShallowWaterSimulation : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            if (ocean == null) return true;

            var isValid = true;

            // Ensure Flow is enabled
            if (!OceanRenderer.ValidateFeatureEnabled(ocean, showMessage, ocean => ocean.CreateFlowSim,
                    LodDataMgrFlow.FEATURE_TOGGLE_NAME, LodDataMgrFlow.FEATURE_TOGGLE_LABEL, LodDataMgrFlow.MATERIAL_KEYWORD, LodDataMgrFlow.MATERIAL_KEYWORD_PROPERTY,
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
                        "Disable debug drawing.",
                        ValidatedHelper.MessageType.Info, this,
                        (so) => OceanRenderer.FixSetFeatureEnabled(so, "_debugSettings._showSimulationData", false)
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
                var buf = new CommandBuffer();

                target.Reset(buf);

                Graphics.ExecuteCommandBuffer(buf);
            }
        }
    }
#endif
}
