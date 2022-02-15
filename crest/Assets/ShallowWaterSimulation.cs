using Crest;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class ShallowWaterSimulation : MonoBehaviour, ILodDataInput
{
    [SerializeField] bool _updateInEditMode = false;

    RenderTexture _rtH;
    PropertyWrapperCompute _csUpdateHProps;
    CommandBuffer _buf;
    Material _matInjectSWSAnimWaves;

    ComputeShader _csUpdateH;
    int _krnlInitH;
    int _krnlUpdateH;

    public float Wavelength => 0f;

    public bool Enabled => true;

    void OnEnable()
    {
        InitData();

        if (_csUpdateH == null)
        {
            _csUpdateH = ComputeShaderHelpers.LoadShader("SWEUpdateH");
            _csUpdateHProps = new PropertyWrapperCompute();

            _krnlInitH = _csUpdateH.FindKernel("InitH");
            _krnlUpdateH = _csUpdateH.FindKernel("UpdateH");
        }

        {
            _matInjectSWSAnimWaves = new Material(Shader.Find("Hidden/Crest/Inputs/Animated Waves/Inject SWS"));
            _matInjectSWSAnimWaves.hideFlags = HideFlags.HideAndDontSave;
            _matInjectSWSAnimWaves.SetFloat(RegisterLodDataInputBase.sp_Weight, 1f);
        }

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;

        {
            var registrar = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrAnimWaves));
            registrar.Remove(this);
            registrar.Add(0, this);
        }

        Reset();
    }

    void InitData()
    {
        if (_rtH == null)
        {
            _rtH = new RenderTexture(512, 512, 0, RenderTextureFormat.RFloat);
            _rtH.enableRandomWrite = true;
            _rtH.Create();
        }

        if (_buf == null)
        {
            _buf = new CommandBuffer();
            _buf.name = "UpdateShallowWaterSim";
        }
    }

    void Reset()
    {
        InitData();

        _buf.Clear();

        {
            _csUpdateHProps.Initialise(_buf, _csUpdateH, _krnlInitH);
            _csUpdateHProps.SetTexture(Shader.PropertyToID("_H"), _rtH);
            _csUpdateHProps.SetFloat(Shader.PropertyToID("_Time"), Time.time);

            _buf.DispatchCompute(_csUpdateH, _krnlInitH, (_rtH.width + 7) / 8, (_rtH.height + 7) / 8, 1);
        }

        Graphics.ExecuteCommandBuffer(_buf);
    }

    void Update()
    {
        InitData();

        _buf.Clear();

        {
            _csUpdateHProps.Initialise(_buf, _csUpdateH, _krnlUpdateH);
            _csUpdateHProps.SetTexture(Shader.PropertyToID("_H"), _rtH);
            _csUpdateHProps.SetFloat(Shader.PropertyToID("_Time"), Time.time);

            _buf.DispatchCompute(_csUpdateH, _krnlUpdateH, (_rtH.width + 7) / 8, (_rtH.height + 7) / 8, 1);
        }

        Graphics.ExecuteCommandBuffer(_buf);

        Shader.SetGlobalTexture("_swsH", _rtH);
    }

    void EditorUpdate()
    {
        if (_updateInEditMode && !EditorApplication.isPlaying)
        {
            Update();
        }
    }

    public void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
    {
        buf.SetGlobalInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
        buf.DrawProcedural(Matrix4x4.identity, _matInjectSWSAnimWaves, 0, MeshTopology.Triangles, 3);
    }
}
