// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_CONSTANTS_H
#define CREST_CONSTANTS_H

// NOTE: these MUST match the values in PropertyWrapper.cs
#define THREAD_GROUP_SIZE_X 8
#define THREAD_GROUP_SIZE_Y 8

// NOTE: This must match the value in LodDataMgr.cs, as it is used to allow the
// C# code to check if any parameters are within the MAX_LOD_COUNT limits
#define MAX_LOD_COUNT 15

// How light is attenuated deep in water
#define DEPTH_OUTSCATTER_CONSTANT 0.25

// NOTE: Must match k_DepthBaseline in LodDataMgrSeaFloorDepth.cs.
// Bias ocean floor depth so that default (0) values in texture are not interpreted as shallow and generating foam everywhere
#define CREST_OCEAN_DEPTH_BASELINE 1000.0

// Soft shadows is red, hard shadows is green.
#define CREST_SHADOW_INDEX_SOFT 0
#define CREST_SHADOW_INDEX_HARD 1

#define CREST_SSS_MAXIMUM 0.6
#define CREST_SSS_RANGE 0.12

// Water rendered from below.
#define UNDERWATER_MASK_BELOW_SURFACE -1.0
// Water rendered from above.
#define UNDERWATER_MASK_ABOVE_SURFACE  1.0

// No mask.
#define UNDERWATER_MASK_NONE 0.0

// The maximum distance the meniscus will be rendered. Only valid when rendering underwater from geometry. The value is
// used to scale the meniscus as it is calculate using a pixel offset which can make the meniscus large at a distance.
#define MENISCUS_MAXIMUM_DISTANCE 15.0


#if defined(STEREO_INSTANCING_ON) || defined(STEREO_MULTIVIEW_ON)
#define CREST_HANDLE_XR 1
#else
#define CREST_HANDLE_XR 0
#endif

#if defined(CREST_WATER_VOLUME_3D) || defined(CREST_WATER_VOLUME_VOLUME)
#define CREST_WATER_VOLUME_HAS_BACKFACE 1
#endif
#if defined(CREST_WATER_VOLUME_2D) || defined(CREST_WATER_VOLUME_3D)
#define CREST_WATER_VOLUME_IS_FRONTFACE 1
#endif
#if defined(CREST_WATER_VOLUME_HAS_BACKFACE) || defined(CREST_WATER_VOLUME_IS_FRONTFACE)
#define CREST_WATER_VOLUME 1
#endif


#endif // CREST_CONSTANTS_H
