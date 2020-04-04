// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;

// This interface exists to allow UnderwaterPostProcess utils to interact with
// the per-camera data that is needed by both the HDRP and built-in implementation
// of underwater post-processing, but done using different classes.
public interface IUnderwaterPostProcessPerCameraData
{
    // NOTE: We keep a list of ocean chunks to render for a given frame
    // (which ocean chunks add themselves to) and reset it each frame by
    // setting the currentChunkCount to 0.
    List<Renderer> OceanChunksToRender { get; }
    List<Renderer> GeneralUnderwaterMasksToRender { get; }
}
