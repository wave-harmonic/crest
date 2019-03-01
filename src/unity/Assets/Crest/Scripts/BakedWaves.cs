// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

public class BakedWaves : MonoBehaviour
{
    public Texture2D[] _waveData;

    public float _period = 10f;

    Renderer _rend;
    private void Start()
    {
        _rend = GetComponent<Renderer>();
    }

    void Update()
    {
        float prop = (Time.time % _period) / _period;
        float index = prop * _waveData.Length;
        float frac = index % 1f;
        _rend.material.SetTexture("_WaveData0", _waveData[Mathf.FloorToInt(index)]);
        _rend.material.SetTexture("_WaveData1", _waveData[(1 + Mathf.FloorToInt(index)) % _waveData.Length]);
        _rend.material.SetFloat("_WaveDataLerp", frac);
    }
}
