using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Crest {

public class Whirlpool : MonoBehaviour {


    [Range(0, 1000)] public float amplitude = 10f;
    [Range(0, 1000)] public float radius = 20f;
    [Range(0, 1000)] public float eyeRadius = 1f;
    [Range(0, 1000)] public float maxSpeed = 70f;

    private GameObject _flow;
    private GameObject _displacement;
    private Material _flowMaterial;
    private Material _displacementMaterial;

    private void UpdateMaterials() {
        _flowMaterial.SetFloat("_EyeRadiusProportion", eyeRadius/radius);
        _flowMaterial.SetFloat("_MaxSpeed", maxSpeed);

        _displacementMaterial.SetFloat("_Radius", radius * 0.5f);
        _displacementMaterial.SetFloat("_Amplitude", amplitude);
    }

    // Use this for initialization
    void Start () {
        _flow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _flow.name = "Swirl";
        _flow.transform.parent = transform;
        _flow.transform.position = new Vector3(0f, -100f, 0f);
        _flow.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        _flow.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        _flow.AddComponent<ApplyLayers>();
        {
            ApplyLayers applyLayers = _flow.GetComponent<ApplyLayers>();
            applyLayers._layerName = "LodDataFlow";
        }
        _flowMaterial = new Material(Shader.Find("Ocean/Shape/Whirlpool Flow"));
        _flow.GetComponent<Renderer>().material = _flowMaterial;


        _displacement = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _displacement.name = "Dip";
        _displacement.transform.parent = transform;
        _displacement.transform.position = new Vector3(0f, -100f, 0f);
        _displacement.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        _displacement.AddComponent<ApplyLayers>();
        {
            ApplyLayers applyLayers = _displacement.GetComponent<ApplyLayers>();
            applyLayers._layerName = "LodDataAnimatedWaves";
        }
        _displacementMaterial = new Material(Shader.Find("Ocean/Shape/Whirlpool Displacement"));
        _displacement.GetComponent<Renderer>().material = _displacementMaterial;

        UpdateMaterials();
    }

    // Update is called once per frame
    void Update () {
        OceanRenderer.Instance.ReportMaxDisplacementFromShape(0, amplitude);
        UpdateMaterials();
    }
}

}
