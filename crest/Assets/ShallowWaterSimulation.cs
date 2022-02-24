using Crest;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public partial class ShallowWaterSimulation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, UnityEngine.Range(1, 500)] int _stepsPerFrame = 1;
    [SerializeField, UnityEngine.Range(16, 1024)] int _resolution = 512;
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

    [Header("Inputs")]
    [SerializeField] Transform _obstacleSphere1 = null;

    RenderTexture _rtH0, _rtH1;
    RenderTexture _rtVx0, _rtVx1;
    RenderTexture _rtVy0, _rtVy1;
    RenderTexture _rtGroundHeight;

    PropertyWrapperCompute _csSWSProps;
    CommandBuffer _buf;

    ComputeShader _csSWS;
    int _krnlInit;
    int _krnlInitGroundHeight;
    int _krnlAdvect;
    int _krnlUpdateH;
    int _krnlUpdateVels;
    int _krnlBlurH;

    void InitData()
    {
        if (_rtH0 == null) _rtH0 = CreateSWSRT();
        if (_rtH1 == null) _rtH1 = CreateSWSRT();
        if (_rtVx0 == null) _rtVx0 = CreateSWSRT();
        if (_rtVx1 == null) _rtVx1 = CreateSWSRT();
        if (_rtVy0 == null) _rtVy0 = CreateSWSRT();
        if (_rtVy1 == null) _rtVy1 = CreateSWSRT();
        if (_rtGroundHeight == null) _rtGroundHeight = CreateSWSRT();

        if (_buf == null)
        {
            _buf = new CommandBuffer();
            _buf.name = "UpdateShallowWaterSim";
        }

        _matInjectSWSAnimWaves.SetFloat("_DomainWidth", _domainWidth);
        _matInjectSWSFlow.SetFloat("_DomainWidth", _domainWidth);
    }

    void Swap<T>(ref T a, ref T b)
    {
        T temp = a;
        a = b;
        b = temp;
    }

    void Update()
    {
        InitData();

        if (_doUpdate)
        {
            _buf.Clear();

            Shader.SetGlobalVector("_ObstacleSphere1Pos", _obstacleSphere1.position);
            Shader.SetGlobalFloat("_ObstacleSphere1Radius", _obstacleSphere1.lossyScale.x / 2f);

            // Populate ground height every frame to allow dynamic scene
            PopulateGroundHeight(_buf);

            for (int i = 0; i < _stepsPerFrame; i++)
            {
                // Each stage block should leave latest state in '1' buffer (H1, Vx1, Vy1)

                // Advect
                if (_doAdvect)
                {
                    Swap(ref _rtH0, ref _rtH1);
                    Swap(ref _rtVx0, ref _rtVx1);
                    Swap(ref _rtVy0, ref _rtVy1);

                    _csSWSProps.Initialise(_buf, _csSWS, _krnlAdvect);

                    _csSWSProps.SetTexture(Shader.PropertyToID("_H0"), _rtH0);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_Vx0"), _rtVx0);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_Vx1"), _rtVx1);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_Vy0"), _rtVy0);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_Vy1"), _rtVy1);

                    _csSWSProps.SetFloat(Shader.PropertyToID("_Time"), Time.time);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_DomainWidth"), _domainWidth);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_Res"), _resolution);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_MaxVel"), _maxVel);

                    _buf.DispatchCompute(_csSWS, _krnlAdvect, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
                }

                // Update H
                if (_doUpdateH)
                {
                    _csSWSProps.Initialise(_buf, _csSWS, _krnlUpdateH);

                    _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_Vx1"), _rtVx1);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_Vy1"), _rtVy1);

                    _csSWSProps.SetFloat(Shader.PropertyToID("_Time"), Time.time);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_DomainWidth"), _domainWidth);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_Res"), _resolution);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_Drain"), _drain);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_Friction"), _friction);

                    _buf.DispatchCompute(_csSWS, _krnlUpdateH, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
                }

                // Update vels
                if (_doUpdateVels)
                {
                    _csSWSProps.Initialise(_buf, _csSWS, _krnlUpdateVels);

                    _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_Vx1"), _rtVx1);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_Vy1"), _rtVy1);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_GroundHeight"), _rtGroundHeight);

                    _csSWSProps.SetFloat(Shader.PropertyToID("_Time"), Time.time);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_DomainWidth"), _domainWidth);
                    _csSWSProps.SetFloat(Shader.PropertyToID("_Res"), _resolution);

                    _buf.DispatchCompute(_csSWS, _krnlUpdateVels, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
                }

                // Blur H
                if (_doBlurH)
                {
                    Swap(ref _rtH0, ref _rtH1);

                    _csSWSProps.Initialise(_buf, _csSWS, _krnlBlurH);

                    _csSWSProps.SetTexture(Shader.PropertyToID("_H0"), _rtH0);
                    _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);

                    _csSWSProps.SetFloat(Shader.PropertyToID("_Res"), _resolution);

                    _buf.DispatchCompute(_csSWS, _krnlBlurH, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
                }
            }

            Graphics.ExecuteCommandBuffer(_buf);
        }

        Shader.SetGlobalTexture("_swsGroundHeight", _rtGroundHeight);
        Shader.SetGlobalTexture("_swsH", _rtH1);
        Shader.SetGlobalTexture("_swsVx", _rtVx1);
        Shader.SetGlobalTexture("_swsVy", _rtVy1);
    }
}

// Separate helpers/glue/initialisation/etc
public partial class ShallowWaterSimulation : MonoBehaviour, ILodDataInput
{
    [Space, Header("Debug")]
    [SerializeField] bool _updateInEditMode = false;

    Material _matInjectSWSAnimWaves;
    Material _matInjectSWSFlow;

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

#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
#endif

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

        Reset();
    }

    RenderTexture CreateSWSRT()
    {
        var result = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.RFloat);
        result.enableRandomWrite = true;
        result.Create();
        return result;
    }

    public void Reset()
    {
        _rtH0 = _rtH1 = _rtVx0 = _rtVx1 = _rtVy0 = _rtVy1 = null;

        InitData();

        _buf.Clear();

        // Populate ground height - used for initial water heigh calculation
        PopulateGroundHeight(_buf);

        // Init sim data - water heights and velocities
        {
            _csSWSProps.Initialise(_buf, _csSWS, _krnlInit);

            _csSWSProps.SetTexture(Shader.PropertyToID("_GroundHeight"), _rtGroundHeight);

            _csSWSProps.SetTexture(Shader.PropertyToID("_H0"), _rtH0);
            _csSWSProps.SetTexture(Shader.PropertyToID("_H1"), _rtH1);
            _csSWSProps.SetTexture(Shader.PropertyToID("_Vx0"), _rtVx0);
            _csSWSProps.SetTexture(Shader.PropertyToID("_Vx1"), _rtVx1);
            _csSWSProps.SetTexture(Shader.PropertyToID("_Vy0"), _rtVy0);
            _csSWSProps.SetTexture(Shader.PropertyToID("_Vy1"), _rtVy1);

            _csSWSProps.SetFloat(Shader.PropertyToID("_Time"), Time.time);
            _csSWSProps.SetFloat(Shader.PropertyToID("_DomainWidth"), _domainWidth);
            _csSWSProps.SetFloat(Shader.PropertyToID("_Res"), _resolution);

            _buf.DispatchCompute(_csSWS, _krnlInit, (_rtH1.width + 7) / 8, (_rtH1.height + 7) / 8, 1);
        }

        Graphics.ExecuteCommandBuffer(_buf);
    }

    void PopulateGroundHeight(CommandBuffer buf)
    {
        _csSWSProps.Initialise(_buf, _csSWS, _krnlInitGroundHeight);
        _csSWSProps.SetVector(Shader.PropertyToID("_ObstacleSphere1Pos"), _obstacleSphere1.position);
        _csSWSProps.SetFloat(Shader.PropertyToID("_ObstacleSphere1Radius"), _obstacleSphere1.lossyScale.x / 2f);
        _csSWSProps.SetTexture(Shader.PropertyToID("_GroundHeightRW"), _rtGroundHeight);
        buf.DispatchCompute(_csSWS, _krnlInitGroundHeight, (_rtGroundHeight.width + 7) / 8, (_rtGroundHeight.height + 7) / 8, 1);
    }

    public void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
    {
        buf.SetGlobalInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);

        var mat = (lodData is LodDataMgrAnimWaves) ? _matInjectSWSAnimWaves : _matInjectSWSFlow;

        buf.DrawProcedural(Matrix4x4.identity, mat, 0, MeshTopology.Triangles, 3);
    }

#if UNITY_EDITOR
    void EditorUpdate()
    {
        if (_updateInEditMode && !EditorApplication.isPlaying)
        {
            Update();
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

        if (GUILayout.Button("Reset"))
        {
            (target as ShallowWaterSimulation).Reset();
        }
    }
}
#endif
