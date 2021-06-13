// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Underwater/Post Process"
{
	CGINCLUDE

	#pragma multi_compile_instancing

	// Use multi_compile because these keywords are copied over from the ocean material. With shader_feature,
	// the keywords would be stripped from builds. Unused shader variants are stripped using a build processor.
	#pragma multi_compile_local __ _SUBSURFACESCATTERING_ON
	#pragma multi_compile_local __ _SUBSURFACESHALLOWCOLOUR_ON
	#pragma multi_compile_local __ _TRANSPARENCY_ON
	#pragma multi_compile_local __ _CAUSTICS_ON
	#pragma multi_compile_local __ _SHADOWS_ON
	#pragma multi_compile_local __ _COMPILESHADERWITHDEBUGINFO_ON

	#pragma multi_compile_local __ CREST_MENISCUS

	#pragma multi_compile_local __ _PROJECTION_PERSPECTIVE _PROJECTION_ORTHOGRAPHIC

	// Are we rendering from a geometry?
	#pragma multi_compile_local __ _GEOMETRY_EFFECT
	// Fullscreen only denotes an optimisation of whether to skip the horizon calculation. Not related to above.
	#pragma multi_compile_local __ _FULL_SCREEN_EFFECT
	#pragma multi_compile_local __ _DEBUG_VIEW_OCEAN_MASK

	#if _COMPILESHADERWITHDEBUGINFO_ON
	#pragma enable_d3d11_debug_symbols
	#endif

	#include "UnityCG.cginc"
	#include "Lighting.cginc"

	#include "../OceanGlobals.hlsl"
	#include "../OceanInputsDriven.hlsl"
	#include "../OceanShaderData.hlsl"
	#include "../OceanHelpersNew.hlsl"
	#include "../OceanShaderHelpers.hlsl"
	#include "../FullScreenTriangle.hlsl"

	half3 _AmbientLighting;

	#include "../OceanEmission.hlsl"

	float _OceanHeight;
	float4x4 _InvViewProjection;
	float4x4 _InvViewProjectionRight;
	float4 _HorizonPosNormal;
	float4 _HorizonPosNormalRight;
	half _DataSliceOffset;

	UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestCameraColorTexture);
	UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture);
	UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestOceanMaskDepthTexture);

	struct Varyings
	{
		float4 positionCS : SV_POSITION;
#if _GEOMETRY_EFFECT
		float4 screenPosition : TEXCOORD0;
#else
		float2 uv : TEXCOORD0;
#endif
		float3 viewWS : TEXCOORD1;
		UNITY_VERTEX_OUTPUT_STEREO
	};

	half3 ApplyUnderwaterEffect(half3 sceneColour, const float sceneZ01, const float sceneZ, const float3 scenePosition, const half3 view, bool isOceanSurface)
	{
		const float3 lightDir = _WorldSpaceLightPos0.xyz;

		half3 scatterCol = 0.0;
		int sliceIndex = clamp(_DataSliceOffset, 0, _SliceCount - 2);
		{
			float3 dummy;
			half sss = 0.0;
			// Offset slice so that we dont get high freq detail. But never use last lod as this has crossfading.
			const float3 uv_slice = WorldToUV(_WorldSpaceCameraPos.xz, _CrestCascadeData[sliceIndex], sliceIndex);
			SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice, 1.0, dummy, sss);

			// depth and shadow are computed in ScatterColour when underwater==true, using the LOD1 texture.
			const float depth = 0.0;
			const half shadow = 1.0;
			{
				const float meshScaleLerp = _CrestPerCascadeInstanceData[sliceIndex]._meshScaleLerp;
				const float baseCascadeScale = _CrestCascadeData[0]._scale;
				scatterCol = ScatterColour(_AmbientLighting, depth, _WorldSpaceCameraPos, lightDir, view, shadow, true, true, sss, meshScaleLerp, baseCascadeScale, _CrestCascadeData[sliceIndex]);
			}
		}

#if _CAUSTICS_ON
		if (sceneZ01 != 0.0 && !isOceanSurface)
		{
			ApplyCaustics(view, lightDir, sceneZ, scenePosition, _Normals, true, sceneColour, _CrestCascadeData[sliceIndex], _CrestCascadeData[sliceIndex + 1]);
		}
#endif // _CAUSTICS_ON

		return lerp(sceneColour, scatterCol, saturate(1.0 - exp(-_DepthFogDensity.xyz * sceneZ)));
	}

	fixed4 Frag(Varyings input) : SV_Target
	{
		// We need this when sampling a screenspace texture.
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if _GEOMETRY_EFFECT
		float2 uv = input.screenPosition.xy / input.screenPosition.w;
#else
		float2 uv = input.uv;
#endif

#if !_FULL_SCREEN_EFFECT
		// The horizon line is the intersection between the far plane and the ocean plane. The pos and normal of this
		// intersection line is passed in.
#if CREST_HANDLE_XR
		const bool isBelowHorizon = unity_StereoEyeIndex == 0 ?
			dot(uv - _HorizonPosNormal.xy, _HorizonPosNormal.zw) > 0.0 :
			dot(uv - _HorizonPosNormalRight.xy, _HorizonPosNormalRight.zw) > 0.0;
#else // CREST_HANDLE_XR
		const bool isBelowHorizon = dot(uv - _HorizonPosNormal.xy, _HorizonPosNormal.zw) > 0.0;
#endif // CREST_HANDLE_XR
#else // !_FULL_SCREEN_EFFECT
		const bool isBelowHorizon = true;
#endif // !_FULL_SCREEN_EFFECT

		const float2 uvScreenSpace = UnityStereoTransformScreenSpaceTex(uv);

		half3 sceneColour = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestCameraColorTexture, uvScreenSpace).rgb;

		float sceneZ01 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, uvScreenSpace).x;

		float mask = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, uvScreenSpace).x;
		const float oceanDepth01 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskDepthTexture, uvScreenSpace);
		bool isOceanSurface = mask != UNDERWATER_MASK_NO_MASK && (sceneZ01 < oceanDepth01);
		bool isUnderwater = mask == UNDERWATER_MASK_WATER_SURFACE_BELOW || (isBelowHorizon && mask != UNDERWATER_MASK_WATER_SURFACE_ABOVE);
		sceneZ01 = isOceanSurface ? oceanDepth01 : sceneZ01;

		float wt = 1.0;

#if CREST_MENISCUS
		// Detect water to no water transitions which happen if mask values on below pixels are less than this mask
		if (mask <= 1.0)
		{
			// Looks at pixels below this pixel and if there is a transition from above to below, darken the pixel
			// to emulate a meniscus effect. It does a few to get a thicker line than 1 pixel. The line it produces is
			// smooth on the top side and sharp at the bottom. It might be possible to detect where the edge is and do
			// a calculation to get it smooth both above and below, but might be more complex.
			float wt_mul = 0.9;
			float4 dy = float4(0.0, -1.0, -2.0, -3.0) / _ScreenParams.y;
			wt *= (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, uvScreenSpace + dy.xy).x > mask) ? wt_mul : 1.0;
			wt *= (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, uvScreenSpace + dy.xz).x > mask) ? wt_mul : 1.0;
			wt *= (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, uvScreenSpace + dy.xw).x > mask) ? wt_mul : 1.0;
		}
#endif // CREST_MENISCUS

#if _DEBUG_VIEW_OCEAN_MASK
		if (!isOceanSurface)
		{
			return float4(sceneColour * float3(isUnderwater * 0.5, (1.0 - isUnderwater) * 0.5, 1.0), 1.0);
		}
		else
		{
			return float4(sceneColour * float3(mask == UNDERWATER_MASK_WATER_SURFACE_ABOVE, mask == UNDERWATER_MASK_WATER_SURFACE_BELOW, 0.0), 1.0);
		}
#else
		if (isUnderwater)
		{
			float3 view = normalize(input.viewWS);
			float sceneZ = CrestLinearEyeDepth(sceneZ01);

			float3 cameraForward = mul((float3x3)unity_CameraToWorld, float3(0.0, 0.0, 1.0));
			float3 scenePosition = _WorldSpaceCameraPos - view * sceneZ / dot(cameraForward, -view);
#if _GEOMETRY_EFFECT
			sceneZ -= input.screenPosition.w;
#endif
			sceneColour = ApplyUnderwaterEffect(sceneColour, sceneZ01, sceneZ, scenePosition, view, isOceanSurface);
		}

		return half4(wt * sceneColour, 1.0);
#endif // _DEBUG_VIEW_OCEAN_MASK
	}
	ENDCG

	Properties
	{
		[Header(Debug Options)]
		[Toggle] _CompileShaderWithDebugInfo("Compile Shader With Debug Info (D3D11)", Float) = 0
	}

	SubShader
	{
		Pass
		{
			// No culling or depth
			Cull Off ZWrite Off ZTest Always

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			struct Attributes
			{
				uint id : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_OUTPUT(Varyings, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.positionCS = GetFullScreenTriangleVertexPosition(input.id);
				output.uv = GetFullScreenTriangleTexCoord(input.id);

				// Compute world space view vector
				{
					const float2 pixelCS = output.uv * 2 - float2(1.0, 1.0);
#if CREST_HANDLE_XR
					const float4x4 InvViewProjection = unity_StereoEyeIndex == 0 ? _InvViewProjection : _InvViewProjectionRight;
#else
					const float4x4 InvViewProjection = _InvViewProjection;
#endif
					const float4 pixelWS_H = mul(InvViewProjection, float4(pixelCS, 1.0, 1.0));
					const float3 pixelWS = pixelWS_H.xyz / pixelWS_H.w;
					output.viewWS = _WorldSpaceCameraPos - pixelWS;
				}

				return output;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			struct Attributes
			{
				float3 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_OUTPUT(Varyings, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				// Use actual geometry instead of full screen triangle.
				output.positionCS = UnityObjectToClipPos(float4(input.positionOS, 1.0));
#if _GEOMETRY_EFFECT
				output.screenPosition = ComputeScreenPos(output.positionCS);
#endif

				// Compute world space view vector - TODO - the below code has XR considerations, and this code does not
				// work. Usually i'd expect a view vector to be (worldPos-_WorldSpaceCameraPos). And viewVS below appears to
				// take a view vector from the camera to the far plane, rather than to the geo, which likely is breaking the
				// rest of the shader...
				float3 worldPos = mul(UNITY_MATRIX_M, float4(input.positionOS, 1.0));
				output.viewWS = _WorldSpaceCameraPos - worldPos;

				return output;
			}
			ENDCG
		}
	}
}
