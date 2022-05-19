using UnityEngine;

public class Turbine : MonoBehaviour
{
    [SerializeField, Range(0f, 10f)]
    float _weight = 0.05f;
    public float Weight => _weight;

    [SerializeField, Range(0f, 10f)] float _velocity = 1f;
    public Vector3 Velocity => transform.up * _velocity;

    public float Radius => transform.localScale.x / 2f;

    [SerializeField] bool _debugRender = true;
    bool _vis = true;

    void SetVis(bool vis)
    {
        var rs = gameObject.GetComponentsInChildren<MeshRenderer>();
        foreach(var r in  rs)
        {
            r.enabled = vis;
        }
    }

    private void Update()
    {
        if (_debugRender != _vis)
        {
            SetVis(_vis);
            _debugRender = _vis;
        }
    }

    public static void SetShaderParams(Turbine turbine, Crest.PropertyWrapperCompute csSWSProps, int idx)
    {
        if (turbine != null && turbine.enabled && turbine.gameObject.activeInHierarchy)
        {
            csSWSProps.SetVector(Shader.PropertyToID($"_Turbine{idx}Position"), turbine.transform.position);
            csSWSProps.SetVector(Shader.PropertyToID($"_Turbine{idx}Velocity"), turbine.Velocity);
            csSWSProps.SetFloat(Shader.PropertyToID($"_Turbine{idx}Radius"), turbine.Radius);
            csSWSProps.SetFloat(Shader.PropertyToID($"_Turbine{idx}Weight"), turbine.Weight);
        }
        else
        {
            csSWSProps.SetFloat(Shader.PropertyToID($"_Turbine{idx}Weight"), 0f);
        }
    }
}
