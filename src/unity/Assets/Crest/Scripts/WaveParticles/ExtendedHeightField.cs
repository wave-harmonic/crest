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
    private enum State { IN_SYNC, GPU_DIRTY, GPU_UNITIALISED }
    private State state = State.GPU_UNITIALISED;

    private RenderTexture _textureHeightMap;
    public RenderTexture textureHeightMap
    {
        get
        {
            if (state == State.GPU_UNITIALISED)
            {
                InitialiseTexture(out _textureHeightMap);
            }
            return _textureHeightMap;
        }
    }

    public ExtendedHeightField(float width, float height, int horRes, int vertRes)
    {
        heightFieldInfo = new HeightFieldInfo(width, height, horRes, vertRes, ((float)width) / ((float)horRes), ((float)height) / ((float)vertRes));
    }

    public void InitialiseTexture(out RenderTexture texture, string name = "Extended Height Field")
    {
        texture = new RenderTexture(heightFieldInfo.HoriRes, heightFieldInfo.VertRes, 24, RenderTextureFormat.ARGBFloat);
        texture.anisoLevel = 1;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.enableRandomWrite = true;
        texture.name = name;
    }
}
