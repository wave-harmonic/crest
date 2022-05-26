// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public partial class ShallowWaterSimulation : MonoBehaviour, LodDataMgrAnimWaves.IShapeUpdatable
{
    [Header("Settings")]
    [SerializeField] bool _placeSimAtGlobalSeaLevel = true;
    [SerializeField] float _depth = 2f;
    [SerializeField] float _addAdditionalWater = 0f;
    [SerializeField, UnityEngine.Range(0.01f, 2f)] float _texelSize = 32f / 512f;
    [SerializeField, UnityEngine.Range(16, 1024)] int _maxResolution = 1024;
    [SerializeField, UnityEngine.Range(8, 128)] float _domainWidth = 32f;
    [SerializeField] float _drain = -0.0001f;

    [Header("Sim Settings")]
    [SerializeField] float _friction = 0.001f;
    [SerializeField] float _maxVel = 100.0f;

    [Header("Sim Controls")]
    [SerializeField] bool _doUpdate = true;
    [SerializeField] bool _doAdvect = true;
    [SerializeField] bool _doUpdateH = true;
    [SerializeField] bool _doUpdateVels = true;
    [SerializeField] bool _doBlurH = true;

    [Header("Blending With Waves")]
    [SerializeField, UnityEngine.Range(-10f, 10f)] float _blendShallowMinDepth = 0f;
    [SerializeField, UnityEngine.Range(-10f, 10f)] float _blendShallowMaxDepth = 4f;
    [SerializeField, UnityEngine.Range(0f, 1f)] float _blendPushUpStrength = 0.1f;

    [Header("Inputs")]
    [SerializeField] Transform _obstacleSphere1 = null;
    [SerializeField] Turbine _turbine1 = null;
    [SerializeField] Turbine _turbine2 = null;

    [Header("Debug")]
    [SerializeField] bool _showTextures = false;

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
        _resolution = Mathf.CeilToInt(_domainWidth / _texelSize);
        _resolution = Mathf.Min(_resolution, _maxResolution);

        if (_rtH0 == null) _rtH0 = CreateSWSRT();
        if (_rtH1 == null) _rtH1 = CreateSWSRT();
        if (_rtVx0 == null) _rtVx0 = CreateSWSRT();
        if (_rtVx1 == null) _rtVx1 = CreateSWSRT();
        if (_rtVy0 == null) _rtVy0 = CreateSWSRT();
        if (_rtVy1 == null) _rtVy1 = CreateSWSRT();
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

        if (_placeSimAtGlobalSeaLevel)
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

        if (_doUpdate)
        {
            _timeToSimulate += Time.deltaTime;

            Shader.SetGlobalVector("_ObstacleSphere1Pos", _obstacleSphere1.position);
            Shader.SetGlobalFloat("_ObstacleSphere1Radius", _obstacleSphere1.lossyScale.x / 2f);

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
                if (_doAdvect)
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
                if (_doUpdateH)
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
                if (_doUpdateVels)
                {
                    _csSWSProps.Initialise(buf, _csSWS, _krnlUpdateVels);

                    _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_Vx1"), _rtVx1);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_Vy1"), _rtVy1);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_GroundHeightSS"), _rtGroundHeight);

                    // Turbines
                    Turbine.SetShaderParams(_turbine1, _csSWSProps, 1);
                    Turbine.SetShaderParams(_turbine2, _csSWSProps, 2);

                    buf.DispatchCompute(_csSWS, _krnlUpdateVels, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
                }

                // Blur H
                if (_doBlurH)
                {
                    Swap(ref _rtH0, ref _rtH1);

                    _csSWSProps.Initialise(buf, _csSWS, _krnlBlurH);

                    _csSWSProps.SetTexture(Shader.PropertyToID("_H0"), _rtH0);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);

                    buf.DispatchCompute(_csSWS, _krnlBlurH, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
                }
            }
        }

        Shader.SetGlobalTexture("_swsGroundHeight", _rtGroundHeight);
        Shader.SetGlobalTexture("_swsSimulationMask", _rtSimulationMask);
        Shader.SetGlobalTexture("_swsH", _rtH1);
        Shader.SetGlobalTexture("_swsVx", _rtVx1);
        Shader.SetGlobalTexture("_swsVy", _rtVy1);
    }
}

// Separate helpers/glue/initialisation/etc
public partial class ShallowWaterSimulation : MonoBehaviour, ILodDataInput
{
    [Space, Header("Debug")]
    //[SerializeField] bool _updateInEditMode = false;

    Material _matInjectSWSAnimWaves;
    Material _matInjectSWSFlow;
    // It currently does not trigger jacobian foam term. If we added displacement to SWS waves it would prob help, but
    // the quality of the shape would have to be much better i guess!
    Material _matInjectSWSFoam;

    // Draw to all LODs
    public float Wavelength => 0f;
    public bool Enabled => true;

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

    RenderTexture CreateSWSRT()
    {
        var result = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.RFloat);
        result.enableRandomWrite = true;
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
        _csSWSProps.SetVector(Shader.PropertyToID("_ObstacleSphere1Pos"), _obstacleSphere1.position);
        _csSWSProps.SetFloat(Shader.PropertyToID("_ObstacleSphere1Radius"), _obstacleSphere1.lossyScale.x / 2f);
        _csSWSProps.SetTexture(Shader.PropertyToID("_GroundHeightSSRW"), _rtGroundHeight);
        _csSWSProps.SetTexture(Shader.PropertyToID("_SimulationMaskRW"), _rtSimulationMask);

        LodDataMgrSeaFloorDepth.Bind(_csSWSProps);

        buf.DispatchCompute(_csSWS, _krnlInitGroundHeight, (_rtGroundHeight.width + 7) / 8, (_rtGroundHeight.height + 7) / 8, 1);
    }

    public void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
    {
        //if (!gameObject || !gameObject.activeInHierarchy || !enabled) return;
        buf.SetGlobalInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);

        // TODO better way to do this?
        var mat = (lodData is LodDataMgrAnimWaves) ? _matInjectSWSAnimWaves
            : ((lodData is LodDataMgrFlow) ? _matInjectSWSFlow
            : _matInjectSWSFoam);

        buf.DrawProcedural(Matrix4x4.identity, mat, 0, MeshTopology.Triangles, 3);
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (_showTextures)
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
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(ShallowWaterSimulation))]
class ShallowWaterSimulationEditor : Editor
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
