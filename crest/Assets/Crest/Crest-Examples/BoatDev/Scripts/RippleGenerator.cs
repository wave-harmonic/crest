// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;

[AddComponentMenu(Crest.Internal.Constants.MENU_PREFIX_EXAMPLE + "Ripple Generator")]
public class RippleGenerator : MonoBehaviour
{
    /// <summary>
    /// The version of this asset. Can be used to migrate across versions. This value should
    /// only be changed when the editor upgrades the version.
    /// </summary>
    [SerializeField, HideInInspector]
#pragma warning disable 414
    int _version = 0;
#pragma warning restore 414

    public bool _animate = true;
    public float _warmUp = 3f;
    public float _onTime = 0.2f;
    public float _period = 4f;

    Renderer _rend;
    MaterialPropertyBlock _mpb;

    RegisterDynWavesInput _rdwi;

    void Start()
    {
        _rdwi = GetComponent<RegisterDynWavesInput>();

        if (OceanRenderer.Instance == null || !OceanRenderer.Instance.CreateDynamicWaveSim || _rdwi == null)
        {
            enabled = false;
            return;
        }

        _rend = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (OceanRenderer.Instance == null)
        {
            return;
        }

        if (_animate)
        {
            float t = OceanRenderer.Instance.CurrentTime;
            if (t < _warmUp)
                return;
            t -= _warmUp;
            t = Mathf.Repeat(t, _period);
            _rdwi.enabled = t < _onTime;
        }

        // which lod is this object in (roughly)?
        Rect thisRect = new Rect(new Vector2(transform.position.x, transform.position.z), Vector3.zero);
        int minLod = LodDataMgrAnimWaves.SuggestDataLOD(thisRect);
        if (minLod == -1)
        {
            // outside all lods, nothing to update!
            return;
        }

        // how many active wave sims currently apply to this object - ideally this would eliminate sims that are too
        // low res, by providing a max grid size param
        LodDataMgrDynWaves.CountWaveSims(minLod, out var simsPresent, out var simsActive);
        if (simsPresent == 0)
        {
            enabled = false;
            return;
        }

        var dt = 1 / OceanRenderer.Instance._lodDataDynWaves.Settings._simulationFrequency;

        _rend.GetPropertyBlock(_mpb);

        _mpb.SetFloat("_SimCount", simsActive);
        _mpb.SetFloat("_SimDeltaTime", dt);

        _rend.SetPropertyBlock(_mpb);
    }
}
