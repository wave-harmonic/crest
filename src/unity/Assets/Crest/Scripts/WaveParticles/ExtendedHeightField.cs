using UnityEngine;
using UnityEngine.Profiling;

public class ExtendedHeightField
{

    public struct HeightFieldInfo
    {
        public readonly float Width;
        public readonly float Height;
        public readonly int HoriRes;
        public readonly int VertRes;
        public readonly float UnitX;
        public readonly float UnitY;

        public HeightFieldInfo(float width, float height, int horRes, int vertRes, float unitX, float unitY)
        {
            Width = width;
            Height = height;
            HoriRes = horRes;
            VertRes = vertRes;
            UnitX = unitX;
            UnitY = unitY;
        }
    }

    public readonly HeightFieldInfo heightFieldInfo;
    public float Width { get { return heightFieldInfo.Width; } }
    public float Height { get { return heightFieldInfo.Height; } }

    public int HoriRes { get { return heightFieldInfo.HoriRes; } }
    public int VertRes { get { return heightFieldInfo.VertRes; } }

    public float UnitX { get { return heightFieldInfo.UnitX; } }
    public float UnitY { get { return heightFieldInfo.UnitY; } }

    // TODO: rethink how states are handles so that code is less messy
    // DOCUMENT HOW STATES WORK AND THEIR PURPOSE
    private enum State { IN_SYNC, GPU_DIRTY, CPU_DIRTY, GPU_UNITIALISED }
    private State state = State.GPU_UNITIALISED;

    private Texture2D _textureHeightMap;
    public Texture2D textureHeightMap
    {
        get
        {
            if (state == State.GPU_UNITIALISED)
            {
                InitialiseTexture(out _textureHeightMap);
                state = State.CPU_DIRTY;
            }
            if (state == State.CPU_DIRTY)
            {
                if (_textureHeightMap.name == "Point Map Texture Name")
                {
                    throw new System.Exception();
                }
                Color[] hm = _heightMap;
                _textureHeightMap.SetPixels(hm);
                _textureHeightMap.Apply();
                state = State.IN_SYNC;
            }
            return _textureHeightMap;
        }
    }

    private readonly Color[] _heightMap;
    public Color[] heightMap
    {
        get
        {
            if (state == State.GPU_DIRTY)
            {
                // TODO: Assert that the Texture sizes match?
                Color[] newHeigthMap = _textureHeightMap.GetPixels();
                for (int i = 0; i < _heightMap.Length; i++)
                {
                    _heightMap[i] = newHeigthMap[i];
                }
                state = State.IN_SYNC;
            }
            return _heightMap;
        }
    }
    public ExtendedHeightField(float width, float height, int horRes, int vertRes)
    {
        heightFieldInfo = new HeightFieldInfo(width, height, horRes, vertRes, ((float)width) / ((float)horRes), ((float)height) / ((float)vertRes));
        _heightMap = new Color[horRes * vertRes];
    }

    //
    public void Clear()
    {
        Profiler.BeginSample("Inside Clear");
        Profiler.BeginSample("Outside For Loop");
        for (int i = 0; i < _heightMap.Length; i++)
        {
            Profiler.BeginSample("Inside For Loop");
            _heightMap[i] = new Color(0, 0, 0, 1);
            Profiler.EndSample();
        }
        Profiler.EndSample();
        Profiler.BeginSample("Apply CPU HeightMap");
        ApplyCPUHeightMap();
        Profiler.EndSample();
        Profiler.EndSample();
    }

    /// <summary>
    /// This method must be called after any changes are made to the CPU heightmap.
    /// </summary>
    public void ApplyCPUHeightMap()
    {
        if (state != State.GPU_UNITIALISED)
        {
            state = State.CPU_DIRTY;
        }
    }

    public void InitialiseTexture(out Texture2D texture, string name = "Extended Height Field")
    {
        texture = new Texture2D(heightFieldInfo.HoriRes, heightFieldInfo.VertRes, TextureFormat.RGBAFloat, false);
        texture.anisoLevel = 1;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.name = name;
    }


    public void UpdateTexture(Texture2D newHeightMap)
    {
        //TODO: Assert that the new height map is in the correct format
        _textureHeightMap = newHeightMap;
        state = State.GPU_DIRTY;
    }
}
