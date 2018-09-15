using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Crest {

public class Whirlpool : MonoBehaviour {


    [Range(0, 1000)] public float amplitude = 10f;
    [Range(0, 1000)] public float radius = 10f;

    GameObject swirl;
    GameObject dip;

    ApplyLayers applyLayers;
    // Use this for initialization
    void Start () {
        swirl = GameObject.CreatePrimitive(PrimitiveType.Quad);
        swirl.name = "Swirl";
        swirl.transform.parent = transform;
        swirl.transform.position = new Vector3(0f, -100f, 0f);
        swirl.AddComponent<ApplyLayers>();
        applyLayers = swirl.GetComponent<ApplyLayers>();
        applyLayers._layerName = "LodDataFlow";

        Material flowMaterial = new Material(Shader.Find("Ocean/Shape/Whirlpool"));
        flowMaterial.SetFloat("_Radius", radius * 2f);
        swirl.GetComponent<Renderer>().material = flowMaterial;


        dip = GameObject.CreatePrimitive(PrimitiveType.Quad);
        dip.name = "Dip";
        dip.transform.parent = transform;
        dip.transform.position = new Vector3(0f, -100f, 0f);
        dip.AddComponent<ApplyLayers>();
        applyLayers = dip.GetComponent<ApplyLayers>();
        applyLayers._layerName = "LodDataAnimatedWaves";

        Material dipMaterial = new Material(Shader.Find("Ocean/Shape/Wave Particle"));
        dipMaterial.SetFloat("_Radius", radius);
        dipMaterial.SetFloat("_Amplitude", amplitude * -1f);
        dip.GetComponent<Renderer>().material = dipMaterial;
    }

    // Update is called once per frame
    void Update () {
        //OceanRenderer.Instance.ReportMaxDisplacementFromShape(radius, -1.0f * amplitude);
    }
}

}
