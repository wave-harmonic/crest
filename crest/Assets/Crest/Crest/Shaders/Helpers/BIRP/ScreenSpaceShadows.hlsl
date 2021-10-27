// Crest Ocean System

// Screen-space shadow helpers.

// Taken and adapted from:
// DefaultResourcesExtra/Internal-ScreenSpaceShadows.shader

// Main changes is that now only world position is required. Specialised for the shadow LOD data.

// Add multi_compile_shadowcollector pragma to get SHADOWS_SPLIT_SPHERES and SHADOWS_SINGLE_CASCADE.
// https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html

UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);

float4 _ShadowMapTexture_TexelSize;
#define SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

#include "UnityShadowLibrary.cginc"

//
// Keywords based defines
//
#if defined (SHADOWS_SPLIT_SPHERES)
    #define GET_CASCADE_WEIGHTS(wpos)    getCascadeWeights_splitSpheres(wpos)
#else
    #define GET_CASCADE_WEIGHTS(wpos)    getCascadeWeights(wpos)
#endif

#if defined (SHADOWS_SINGLE_CASCADE)
    #define GET_SHADOW_COORDINATES(wpos) getShadowCoord_SingleCascade(wpos)
#else
    #define GET_SHADOW_COORDINATES(wpos) getShadowCoord(wpos)
#endif

/**
 * Gets the cascade weights based on the world position of the fragment.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
inline fixed4 getCascadeWeights(float3 wpos)
{
    // Calculate depth. Normally this would be depth from the depth buffer.
    float z = dot(wpos - _WorldSpaceCameraPos.xyz, unity_CameraToWorld._m02_m12_m22);
    fixed4 zNear = float4( z >= _LightSplitsNear );
    fixed4 zFar = float4( z < _LightSplitsFar );
    fixed4 weights = zNear * zFar;
    return weights;
}

/**
 * Gets the cascade weights based on the world position of the fragment and the poisitions of the split spheres for each cascade.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
inline fixed4 getCascadeWeights_splitSpheres(float3 wpos)
{
    float3 fromCenter0 = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
    float4 distances2 = float4(dot(fromCenter0,fromCenter0), dot(fromCenter1,fromCenter1), dot(fromCenter2,fromCenter2), dot(fromCenter3,fromCenter3));
    fixed4 weights = float4(distances2 < unity_ShadowSplitSqRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);
    return weights;
}

/**
 * Returns the shadowmap coordinates for the given fragment based on the world position and z-depth.
 * These coordinates belong to the shadowmap atlas that contains the maps for all cascades.
 */
inline float4 getShadowCoord(float4 wpos)
{
    fixed4 cascadeWeights = GET_CASCADE_WEIGHTS(wpos);
    float3 sc0 = mul (unity_WorldToShadow[0], wpos).xyz;
    float3 sc1 = mul (unity_WorldToShadow[1], wpos).xyz;
    float3 sc2 = mul (unity_WorldToShadow[2], wpos).xyz;
    float3 sc3 = mul (unity_WorldToShadow[3], wpos).xyz;
    float4 shadowMapCoordinate = float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);
#if defined(UNITY_REVERSED_Z)
    float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
    shadowMapCoordinate.z += noCascadeWeights;
#endif
    return shadowMapCoordinate;
}

/**
 * Same as the getShadowCoord; but optimized for single cascade
 */
inline float4 getShadowCoord_SingleCascade( float4 wpos )
{
    return float4(mul(unity_WorldToShadow[0], wpos).xyz, 0);
}
