// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#include "../OceanConstants.hlsl"
#include "../OceanGlobals.hlsl"
#include "../OceanInputsDriven.hlsl"
#include "../OceanHelpersNew.hlsl"
// Noise functions used for jitter.
#include "../GPUNoise/GPUNoise.hlsl"

CBUFFER_START(CrestPerMaterial)
// Settings._jitterDiameterSoft, Settings._jitterDiameterHard, Settings._currentFrameWeightSoft, Settings._currentFrameWeightHard
float4 _JitterDiameters_CurrentFrameWeights;
float _SimDeltaTime;

float3 _CenterPos;
float3 _Scale;
float4x4 _MainCameraProjectionMatrix;
CBUFFER_END

struct Attributes
{
	float3 positionOS : POSITION;
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float3 positionWS : TEXCOORD0;
};

half CrestSampleShadows(const float4 i_positionWS);
half CrestComputeShadowFade(const float4 i_positionWS);

half ComputeShadow(const float4 i_positionWS, const float i_jitterDiameter)
{
	float4 positionWS = i_positionWS;

	if (i_jitterDiameter > 0.0)
	{
		// Add jitter.
		positionWS.xz += i_jitterDiameter * (hash33(uint3(abs(positionWS.xz * 10.0), _Time.y * 120.0)) - 0.5).xy;
	}

	return CrestSampleShadows(positionWS);
}

half2 Frag(Varyings input) : SV_Target
{
	half2 shadow = 0.0;

	float4 positionWS = float4(input.positionWS.xyz, 1.0);
	{
		float3 uv = WorldToUV(positionWS.xz, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);
		// Offset world position by sea level offset.
		positionWS.y += _LD_TexArray_SeaFloorDepth.SampleLevel(LODData_linear_clamp_sampler, uv, 0.0).y;
	}

	// Shadow from last frame.
	{
		const int slice0 = _LD_SliceIndex + _CrestLodChange;
		const CascadeParams cascadeData = _CrestCascadeDataSource[slice0];
		const float3 uv = WorldToUV(positionWS.xz, cascadeData, slice0);
		float depth;
		{
			float width; float height;
			_LD_TexArray_Shadow_Source.GetDimensions(width, height, depth);
		}

		// Manually implement black border.
		half2 r = abs(uv.xy - 0.5);
		const half rMax = 0.5 - cascadeData._oneOverTextureRes;
		if (max(r.x, r.y) <= rMax)
		{
			SampleShadow(_LD_TexArray_Shadow_Source, uv, 1.0, shadow);
		}
		else if (slice0 + 1.0 < depth)
		{
			const float3 uvNextLod = WorldToUV(positionWS.xz, _CrestCascadeDataSource[slice0 + 1], slice0 + 1);
			half2 r2 = abs(uvNextLod.xy - 0.5);
			if (max(r2.x, r2.y) <= rMax)
			{
				SampleShadow(_LD_TexArray_Shadow_Source, uvNextLod, 1.0, shadow);
			}
		}
	}

	// This was calculated in vertex but we have to sample sea level offset in fragment.
	float4 mainCameraCoordinates = mul(_MainCameraProjectionMatrix, positionWS);

	// Check if the current sample is visible in the main camera (and therefore the shadow map can be sampled). This is
	// required as the shadow buffer is world aligned and surrounds viewer.
	float3 projected = mainCameraCoordinates.xyz / mainCameraCoordinates.w;
	if (projected.z < 1.0 && projected.z > 0.0 && abs(projected.x) < 1.0 && abs(projected.y) < 1.0)
	{
		half2 shadowThisFrame = 1.0;

#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
		positionWS.xyz -= _WorldSpaceCameraPos.xyz;
#endif

		// Add soft shadowing data.
		shadowThisFrame[CREST_SHADOW_INDEX_SOFT] = ComputeShadow
		(
			positionWS,
			_JitterDiameters_CurrentFrameWeights[CREST_SHADOW_INDEX_SOFT]
		);

#ifdef CREST_SAMPLE_SHADOW_HARD
		// Add hard shadowing data.
		shadowThisFrame[CREST_SHADOW_INDEX_HARD] = ComputeShadow
		(
			positionWS,
			_JitterDiameters_CurrentFrameWeights[CREST_SHADOW_INDEX_HARD]
		);
#endif

		shadowThisFrame = (half2)1.0 - saturate(shadowThisFrame + CrestComputeShadowFade(positionWS));

		shadow = lerp(shadow, shadowThisFrame, _JitterDiameters_CurrentFrameWeights.zw * _SimDeltaTime * 60.0);
	}

	return shadow;
}
