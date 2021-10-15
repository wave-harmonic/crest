// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Underwater/Underwater Effect"
{
	HLSLINCLUDE
	#pragma vertex Vert
	#pragma fragment Frag

	// Use multi_compile because these keywords are copied over from the ocean material. With shader_feature,
	// the keywords would be stripped from builds. Unused shader variants are stripped using a build processor.
	#pragma multi_compile_local __ _SUBSURFACESCATTERING_ON
	#pragma multi_compile_local __ _SUBSURFACESHALLOWCOLOUR_ON
	#pragma multi_compile_local __ _CAUSTICS_ON
	#pragma multi_compile_local __ _SHADOWS_ON
	#pragma multi_compile_local __ _COMPILESHADERWITHDEBUGINFO_ON
	#pragma multi_compile_local __ _PROJECTION_PERSPECTIVE _PROJECTION_ORTHOGRAPHIC

	#pragma multi_compile_local __ CREST_MENISCUS
	// Both "__" and "_FULL_SCREEN_EFFECT" are fullscreen triangles. The latter only denotes an optimisation of
	// whether to skip the horizon calculation.
	#pragma multi_compile_local __ _FULL_SCREEN_EFFECT CREST_BOUNDARY_2D CREST_BOUNDARY_3D CREST_BOUNDARY_VOLUME
	#pragma multi_compile_local __ _DEBUG_VIEW_OCEAN_MASK

#if _COMPILESHADERWITHDEBUGINFO_ON
	#pragma enable_d3d11_debug_symbols
#endif

	#include "UnityCG.cginc"
	#include "Lighting.cginc"

	#include "../../Helpers/BIRP/Core.hlsl"
	#include "../../Helpers/BIRP/InputsDriven.hlsl"

	#include "../../OceanGlobals.hlsl"
	#include "../../OceanInputsDriven.hlsl"
	#include "../../OceanShaderData.hlsl"
	#include "../../OceanHelpersNew.hlsl"
	#include "../../OceanShaderHelpers.hlsl"
	#include "../../FullScreenTriangle.hlsl"
	#include "../../OceanEmission.hlsl"

	TEXTURE2D_X(_CrestCameraColorTexture);
	TEXTURE2D_X(_CrestOceanMaskTexture);
	TEXTURE2D_X(_CrestOceanMaskDepthTexture);

	#include "../UnderwaterEffectShared.hlsl"

	struct Attributes
	{
#if CREST_BOUNDARY
		float3 positionOS : POSITION;
#else
		uint id : SV_VertexID;
#endif
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct Varyings
	{
		float4 positionCS : SV_POSITION;
#if CREST_BOUNDARY
		float4 screenPosition : TEXCOORD0;
#else
		float2 uv : TEXCOORD0;
#endif
		UNITY_VERTEX_OUTPUT_STEREO
	};

	Varyings Vert (Attributes input)
	{
		Varyings output;

		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_INITIALIZE_OUTPUT(Varyings, output);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if CREST_BOUNDARY
		// Use actual geometry instead of full screen triangle.
		output.positionCS = UnityObjectToClipPos(float4(input.positionOS, 1.0));
		output.screenPosition = ComputeScreenPos(output.positionCS);
#else
		output.positionCS = GetFullScreenTriangleVertexPosition(input.id);
		output.uv = GetFullScreenTriangleTexCoord(input.id);
#endif

		return output;
	}

	fixed4 Frag (Varyings input) : SV_Target
	{
		// We need this when sampling a screenspace texture.
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if CREST_BOUNDARY
		float2 uv = input.screenPosition.xy / input.screenPosition.w;
#else
		float2 uv = input.uv;
#endif

		const int2 positionSS = input.positionCS.xy;
		half3 sceneColour = LOAD_TEXTURE2D_X(_CrestCameraColorTexture, positionSS).rgb;
		float rawDepth = LOAD_TEXTURE2D_X(_CameraDepthTexture, positionSS).r;
		const float mask = LOAD_TEXTURE2D_X(_CrestOceanMaskTexture, positionSS).r;
		const float rawOceanDepth = LOAD_TEXTURE2D_X(_CrestOceanMaskDepthTexture, positionSS).r;

		float rawGeometryDepth = 0.0;
#if CREST_BOUNDARY_VOLUME
		rawGeometryDepth = input.positionCS.z;
#elif CREST_BOUNDARY_3D
		rawGeometryDepth = LOAD_DEPTH_TEXTURE_X(_CrestWaterBoundaryGeometryInnerTexture, positionSS).r;
#endif

		bool isOceanSurface; bool isUnderwater; float sceneZ;
		GetOceanSurfaceAndUnderwaterData(positionSS, rawOceanDepth, rawGeometryDepth, mask, rawDepth, isOceanSurface, isUnderwater, sceneZ, 0.0);

#if CREST_BOUNDARY_VOLUME
		const float frontFaceBoundaryDepth01 = LOAD_TEXTURE2D_X(_CrestWaterBoundaryGeometryOuterTexture, positionSS).r;
		bool isBeforeFrontFaceBoundary = false;

		if (frontFaceBoundaryDepth01 != 0)
		{
			// Scene is before front face boundary.
			if (rawDepth > frontFaceBoundaryDepth01)
			{
				// Bail early to avoid meniscus.
				return float4(sceneColour, 1.0);
			}
			else
			{
				isBeforeFrontFaceBoundary = true;
			}
		}
#endif

		float wt = ComputeMeniscusWeight(positionSS, mask, _HorizonNormal, sceneZ);

#if _DEBUG_VIEW_OCEAN_MASK
		return DebugRenderOceanMask(isOceanSurface, isUnderwater, mask, sceneColour);
#endif

		if (isUnderwater)
		{
			// Position needs to be reconstructed in the fragment shader to avoid precision issues as per
			// Unity's lead. Fixes caustics stuttering when far from zero.
			const float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
			const half3 view = normalize(_WorldSpaceCameraPos - positionWS);
			float3 scenePos = _WorldSpaceCameraPos - view * sceneZ / dot(unity_CameraToWorld._m02_m12_m22, -view);

#if CREST_BOUNDARY_VOLUME
			if (isBeforeFrontFaceBoundary)
			{
				sceneZ -= CrestLinearEyeDepth(frontFaceBoundaryDepth01);
			}
#elif CREST_BOUNDARY_IS_FRONTFACE
			sceneZ -= CrestLinearEyeDepth(input.positionCS.z);
#endif // CREST_BOUNDARY

			const float3 lightDir = _WorldSpaceLightPos0.xyz;
			const half3 lightCol = _LightColor0;
			sceneColour = ApplyUnderwaterEffect(scenePos, sceneColour, lightCol, lightDir, rawDepth, sceneZ, view, isOceanSurface);
		}

		return half4(wt * sceneColour, 1.0);
	}
	ENDHLSL

	SubShader
	{
		// These will be "Off" for fullscreen.
		Cull [_CullMode]
		ZTest [_ZTest]
		ZWrite Off

		Pass
		{
			HLSLPROGRAM
			ENDHLSL
		}
	}
}
