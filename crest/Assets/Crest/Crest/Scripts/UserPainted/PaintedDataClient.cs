// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    // Interface that this component uses to find clients and determine their data requirements
    public interface IPaintedDataClient
    {
        GraphicsFormat GraphicsFormat { get; }

        void ClearData();

        bool Paint(Vector3 paintPosition3, Vector2 paintDir, float paintWeight, bool remove);

        CPUTexture2DBase Texture { get; }

        Vector2 WorldSize { get; }
        float PaintRadius { get; }

        Transform Transform { get; }
    }
}
