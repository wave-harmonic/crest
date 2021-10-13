// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders convex hull to the clip surface texture.

Shader "Crest/Inputs/Clip Surface/Convex Hull"
{
	Properties
	{
		[Toggle] _Inverted("Inverted", Float) = 0
	}

	CGINCLUDE
	#pragma vertex Vert
	#pragma fragment Frag

	// For SV_IsFrontFace.
	#pragma target 3.0

	#include "UnityCG.cginc"
	#include "../../OceanGlobals.hlsl"
	#include "../../OceanInputsDriven.hlsl"
	#include "../../OceanVertHelpers.hlsl"
	#include "../../OceanHelpersNew.hlsl"
	#include "../../OceanHelpersDriven.hlsl"

	CBUFFER_START(CrestPerOceanInput)
	uint _DisplacementSamplingIterations;
	float _Inverted;
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

	Varyings Vert(Attributes input)
	{
		Varyings o;
		o.positionCS = UnityObjectToClipPos(input.positionOS);
		o.positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
		return o;
	}

	float4 Frag(Varyings input, const bool isFrontFace : SV_IsFrontFace) : SV_Target
	{
		float3 surfacePositionWS = SampleOceanDataDisplacedToWorldPosition
		(
			_LD_TexArray_AnimatedWaves,
			input.positionWS,
			_DisplacementSamplingIterations
		);

		float3 uv = WorldToUV(input.positionWS.xz, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);
		half seaLevelOffset = _LD_TexArray_SeaFloorDepth.SampleLevel(LODData_linear_clamp_sampler, uv, 0.0).y;

		// Move to sea level.
		surfacePositionWS.y += _OceanCenterPosWorld.y + seaLevelOffset;

		// Clip if above water.
		if (input.positionWS.y > surfacePositionWS.y)
		{
			clip(-1.0);
		}

		// To add clipping, back face must write one and front face must write zero.
		return float4(isFrontFace == _Inverted ? 1.0 : 0.0, 0.0, 0.0, 1.0);
	}
	ENDCG

	SubShader
	{
		ZWrite Off
		ColorMask R

		Pass
		{
			Cull Front
			// Here so CGINCLUDE works.
			CGPROGRAM
			ENDCG
		}

		Pass
		{
			Cull Back
			// Here so CGINCLUDE works.
			CGPROGRAM
			ENDCG
		}
	}
}
