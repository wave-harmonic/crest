// Crest Ocean System

// Adds functions from SRP.

#ifndef UNITY_COMMON_INCLUDED
#define UNITY_COMMON_INCLUDED

// Add "real" alias for "fixed". Helps with including files downstream.

#define real fixed
#define real2 fixed2
#define real3 fixed3
#define real4 fixed4

// Commented lines have no "fixed" equivalent.

#define real2x2 fixed2x2
// #define real2x3 fixed2x3
// #define real2x4 fixed2x4
// #define real3x2 fixed3x2
#define real3x3 fixed3x3
// #define real3x4 fixed3x4
// #define real4x3 fixed4x3
#define real4x4 fixed4x4

//
// MACROS
//

#define ZERO_INITIALIZE(type, name) UNITY_INITIALIZE_OUTPUT(type,name)
#define TransformObjectToHClip(positionOS) UnityObjectToClipPos(float4(positionOS, 1.0))

// Taken from:
// com.unity.render-pipelines.core@10.5.0/ShaderLibrary/API/D3D11.hlsl
// com.unity.render-pipelines.core@10.5.0/ShaderLibrary/API/Metal.hlsl
// com.unity.render-pipelines.core@10.5.0/ShaderLibrary/API/Switch.hlsl
// com.unity.render-pipelines.core@10.5.0/ShaderLibrary/API/Vulkan.hlsl

// GameCore, PSSL etc require an NDA so hard to confirm how some of these APIs are implemented, but the PPv2 package has
// some of APIs (the ones we use) and they are the same:
// com.unity.postprocessing/PostProcessing/Shaders/API/

// Texture abstraction.

#define TEXTURE2D(textureName)                Texture2D textureName
#define TEXTURE2D_ARRAY(textureName)          Texture2DArray textureName
// #define TEXTURECUBE(textureName)              TextureCube textureName
// #define TEXTURECUBE_ARRAY(textureName)        TextureCubeArray textureName
// #define TEXTURE3D(textureName)                Texture3D textureName

// #ifdef SHADER_API_D3D11

// #define TEXTURE2D_FLOAT(textureName)          TEXTURE2D(textureName)
// #define TEXTURE2D_ARRAY_FLOAT(textureName)    TEXTURE2D_ARRAY(textureName)
// #define TEXTURECUBE_FLOAT(textureName)        TEXTURECUBE(textureName)
// #define TEXTURECUBE_ARRAY_FLOAT(textureName)  TEXTURECUBE_ARRAY(textureName)
// #define TEXTURE3D_FLOAT(textureName)          TEXTURE3D(textureName)

// #define TEXTURE2D_HALF(textureName)           TEXTURE2D(textureName)
// #define TEXTURE2D_ARRAY_HALF(textureName)     TEXTURE2D_ARRAY(textureName)
// #define TEXTURECUBE_HALF(textureName)         TEXTURECUBE(textureName)
// #define TEXTURECUBE_ARRAY_HALF(textureName)   TEXTURECUBE_ARRAY(textureName)
// #define TEXTURE3D_HALF(textureName)           TEXTURE3D(textureName)

// #else // !SHADER_API_D3D11

// #define TEXTURE2D_FLOAT(textureName)          Texture2D_float textureName
// #define TEXTURE2D_ARRAY_FLOAT(textureName)    Texture2DArray_float textureName
// #define TEXTURECUBE_FLOAT(textureName)        TextureCube_float textureName
// #define TEXTURECUBE_ARRAY_FLOAT(textureName)  TextureCubeArray_float textureName
// #define TEXTURE3D_FLOAT(textureName)          Texture3D_float textureName

// #define TEXTURE2D_HALF(textureName)           Texture2D_half textureName
// #define TEXTURE2D_ARRAY_HALF(textureName)     Texture2DArray_half textureName
// #define TEXTURECUBE_HALF(textureName)         TextureCube_half textureName
// #define TEXTURECUBE_ARRAY_HALF(textureName)   TextureCubeArray_half textureName
// #define TEXTURE3D_HALF(textureName)           Texture3D_half textureName

// #endif // SHADER_API_D3D11

// #define TEXTURE2D_SHADOW(textureName)         TEXTURE2D(textureName)
// #define TEXTURE2D_ARRAY_SHADOW(textureName)   TEXTURE2D_ARRAY(textureName)
// #define TEXTURECUBE_SHADOW(textureName)       TEXTURECUBE(textureName)
// #define TEXTURECUBE_ARRAY_SHADOW(textureName) TEXTURECUBE_ARRAY(textureName)

#define RW_TEXTURE2D(type, textureName)       RWTexture2D<type> textureName
#define RW_TEXTURE2D_ARRAY(type, textureName) RWTexture2DArray<type> textureName
// #define RW_TEXTURE3D(type, textureName)       RWTexture3D<type> textureName

#define SAMPLER(samplerName)                  SamplerState samplerName
// #define SAMPLER_CMP(samplerName)              SamplerComparisonState samplerName
// #define ASSIGN_SAMPLER(samplerName, samplerValue) samplerName = samplerValue

// #define TEXTURE2D_PARAM(textureName, samplerName)                 TEXTURE2D(textureName),         SAMPLER(samplerName)
// #define TEXTURE2D_ARRAY_PARAM(textureName, samplerName)           TEXTURE2D_ARRAY(textureName),   SAMPLER(samplerName)
// #define TEXTURECUBE_PARAM(textureName, samplerName)               TEXTURECUBE(textureName),       SAMPLER(samplerName)
// #define TEXTURECUBE_ARRAY_PARAM(textureName, samplerName)         TEXTURECUBE_ARRAY(textureName), SAMPLER(samplerName)
// #define TEXTURE3D_PARAM(textureName, samplerName)                 TEXTURE3D(textureName),         SAMPLER(samplerName)

// #define TEXTURE2D_SHADOW_PARAM(textureName, samplerName)          TEXTURE2D(textureName),         SAMPLER_CMP(samplerName)
// #define TEXTURE2D_ARRAY_SHADOW_PARAM(textureName, samplerName)    TEXTURE2D_ARRAY(textureName),   SAMPLER_CMP(samplerName)
// #define TEXTURECUBE_SHADOW_PARAM(textureName, samplerName)        TEXTURECUBE(textureName),       SAMPLER_CMP(samplerName)
// #define TEXTURECUBE_ARRAY_SHADOW_PARAM(textureName, samplerName)  TEXTURECUBE_ARRAY(textureName), SAMPLER_CMP(samplerName)

// #define TEXTURE2D_ARGS(textureName, samplerName)                textureName, samplerName
// #define TEXTURE2D_ARRAY_ARGS(textureName, samplerName)          textureName, samplerName
// #define TEXTURECUBE_ARGS(textureName, samplerName)              textureName, samplerName
// #define TEXTURECUBE_ARRAY_ARGS(textureName, samplerName)        textureName, samplerName
// #define TEXTURE3D_ARGS(textureName, samplerName)                textureName, samplerName

// #define TEXTURE2D_SHADOW_ARGS(textureName, samplerName)         textureName, samplerName
// #define TEXTURE2D_ARRAY_SHADOW_ARGS(textureName, samplerName)   textureName, samplerName
// #define TEXTURECUBE_SHADOW_ARGS(textureName, samplerName)       textureName, samplerName
// #define TEXTURECUBE_ARRAY_SHADOW_ARGS(textureName, samplerName) textureName, samplerName

#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2)                               textureName.Sample(samplerName, coord2)
// #define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod)                      textureName.SampleLevel(samplerName, coord2, lod)
// #define SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, coord2, bias)                    textureName.SampleBias(samplerName, coord2, bias)
// #define SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, coord2, dpdx, dpdy)              textureName.SampleGrad(samplerName, coord2, dpdx, dpdy)
#define SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)                  textureName.Sample(samplerName, float3(coord2, index))
// #define SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, index, lod)         textureName.SampleLevel(samplerName, float3(coord2, index), lod)
// #define SAMPLE_TEXTURE2D_ARRAY_BIAS(textureName, samplerName, coord2, index, bias)       textureName.SampleBias(samplerName, float3(coord2, index), bias)
// #define SAMPLE_TEXTURE2D_ARRAY_GRAD(textureName, samplerName, coord2, index, dpdx, dpdy) textureName.SampleGrad(samplerName, float3(coord2, index), dpdx, dpdy)
// #define SAMPLE_TEXTURECUBE(textureName, samplerName, coord3)                             textureName.Sample(samplerName, coord3)
// #define SAMPLE_TEXTURECUBE_LOD(textureName, samplerName, coord3, lod)                    textureName.SampleLevel(samplerName, coord3, lod)
// #define SAMPLE_TEXTURECUBE_BIAS(textureName, samplerName, coord3, bias)                  textureName.SampleBias(samplerName, coord3, bias)
// #define SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)                textureName.Sample(samplerName, float4(coord3, index))
// #define SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, index, lod)       textureName.SampleLevel(samplerName, float4(coord3, index), lod)
// #define SAMPLE_TEXTURECUBE_ARRAY_BIAS(textureName, samplerName, coord3, index, bias)     textureName.SampleBias(samplerName, float4(coord3, index), bias)
// #define SAMPLE_TEXTURE3D(textureName, samplerName, coord3)                               textureName.Sample(samplerName, coord3)
// #define SAMPLE_TEXTURE3D_LOD(textureName, samplerName, coord3, lod)                      textureName.SampleLevel(samplerName, coord3, lod)

// #define SAMPLE_TEXTURE2D_SHADOW(textureName, samplerName, coord3)                    textureName.SampleCmpLevelZero(samplerName, (coord3).xy, (coord3).z)
// #define SAMPLE_TEXTURE2D_ARRAY_SHADOW(textureName, samplerName, coord3, index)       textureName.SampleCmpLevelZero(samplerName, float3((coord3).xy, index), (coord3).z)
// #define SAMPLE_TEXTURECUBE_SHADOW(textureName, samplerName, coord4)                  textureName.SampleCmpLevelZero(samplerName, (coord4).xyz, (coord4).w)
// #define SAMPLE_TEXTURECUBE_ARRAY_SHADOW(textureName, samplerName, coord4, index)     textureName.SampleCmpLevelZero(samplerName, float4((coord4).xyz, index), (coord4).w)

#undef SAMPLE_DEPTH_TEXTURE
// #undef SAMPLE_DEPTH_TEXTURE_LOD
#define SAMPLE_DEPTH_TEXTURE(textureName, samplerName, coord2)          SAMPLE_TEXTURE2D(textureName, samplerName, coord2).r
// #define SAMPLE_DEPTH_TEXTURE_LOD(textureName, samplerName, coord2, lod) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod).r

#define LOAD_TEXTURE2D(textureName, unCoord2)                                   textureName.Load(int3(unCoord2, 0))
// #define LOAD_TEXTURE2D_LOD(textureName, unCoord2, lod)                          textureName.Load(int3(unCoord2, lod))
// #define LOAD_TEXTURE2D_MSAA(textureName, unCoord2, sampleIndex)                 textureName.Load(unCoord2, sampleIndex)
#define LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, index)                      textureName.Load(int4(unCoord2, index, 0))
// #ifndef SHADER_API_SWITCH
// #define LOAD_TEXTURE2D_ARRAY_MSAA(textureName, unCoord2, index, sampleIndex)    textureName.Load(int3(unCoord2, index), sampleIndex)
// #endif
// #define LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, index, lod)             textureName.Load(int4(unCoord2, index, lod))
// #define LOAD_TEXTURE3D(textureName, unCoord3)                                   textureName.Load(int4(unCoord3, 0))
// #define LOAD_TEXTURE3D_LOD(textureName, unCoord3, lod)                          textureName.Load(int4(unCoord3, lod))

// #define GATHER_TEXTURE2D(textureName, samplerName, coord2)                textureName.Gather(samplerName, coord2)
// #define GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)   textureName.Gather(samplerName, float3(coord2, index))
// #define GATHER_TEXTURECUBE(textureName, samplerName, coord3)              textureName.Gather(samplerName, coord3)
// #define GATHER_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index) textureName.Gather(samplerName, float4(coord3, index))
// #define GATHER_RED_TEXTURE2D(textureName, samplerName, coord2)            textureName.GatherRed(samplerName, coord2)
// #define GATHER_GREEN_TEXTURE2D(textureName, samplerName, coord2)          textureName.GatherGreen(samplerName, coord2)
// #define GATHER_BLUE_TEXTURE2D(textureName, samplerName, coord2)           textureName.GatherBlue(samplerName, coord2)
// #define GATHER_ALPHA_TEXTURE2D(textureName, samplerName, coord2)          textureName.GatherAlpha(samplerName, coord2)


//
// FUNCTIONS
//

// Taken and modified from:
// com.unity.render-pipelines.core@10.5.0/ShaderLibrary/Common.hlsl
float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
{
	float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
	// positionCS.y was flipped here but that is SRP specific to solve flip baked into matrix.
	return positionCS;
}

// Taken from:
// com.unity.render-pipelines.core@10.5.0/ShaderLibrary/Common.hlsl
float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)
{
	float4 positionCS  = ComputeClipSpacePosition(positionNDC, deviceDepth);
	float4 hpositionWS = mul(invViewProjMatrix, positionCS);
	return hpositionWS.xyz / hpositionWS.w;
}

#endif // UNITY_COMMON_INCLUDED
