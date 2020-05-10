Shader "Crest/Examples/SubmarineWindow"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags { "Queue" = "Geometry+511" "RenderType"="Transparent" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:blend finalcolor:CrestApplyUnderwaterFog

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.5

		// texture arrays are not available everywhere,
		// only compile shader on platforms where they are
		#pragma require 2darray

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		struct Input
		{
			float2 uv_MainTex;
			float4 screenPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void surf(Input IN, inout SurfaceOutputStandard surfaceOutput)
		{
			// Albedo comes from a texture tinted by color
			//fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			fixed4 color = fixed4(.3, .25, .2, .3);

			// TODO(TRC):Now compute fog
			surfaceOutput.Albedo = color.rgb;
			// Metallic and smoothness come from slider variables
			surfaceOutput.Metallic = _Metallic;
			surfaceOutput.Smoothness = _Glossiness;
			surfaceOutput.Alpha = color.a;
		}

		half3 _AmbientLighting;
		#include "../../../Crest/Shaders/OceanConstants.hlsl"
#ifdef SHADER_API_D3D11
		#include "../../../Crest/Shaders/OceanInputsDriven.hlsl"
		#include "../../../Crest/Shaders/OceanGlobals.hlsl"
		#include "../../../Crest/Shaders/OceanLODData.hlsl"
		#include "../../../Crest/Shaders/OceanHelpersNew.hlsl"
		#include "../../../Crest/Shaders/OceanEmission.hlsl"
#else
		uniform half4 _DepthFogDensity;
#endif

		float4 _CrestHorizonPosNormal;
		sampler2D _CrestOceanMaskTexture;
		sampler2D _CrestOceanMaskDepthTexture;

#ifdef SHADER_API_D3D11
		#include "../../../Crest/Shaders/ApplyUnderwaterEffect.hlsl"
#endif

		sampler2D _Normals;
		sampler2D _CameraDepthTexture;

		void CrestApplyUnderwaterFog (Input input, SurfaceOutputStandard surfaceOutput, inout fixed4 color)
		{
			float2 uvScreenSpace = input.screenPos.xy / input.screenPos.w;
			float surfaceZ = input.screenPos.z / input.screenPos.w;

			// TODO(TRC):Now, break this all out into a helpfer function that will
			// also compute fog
			float oceanMask = tex2D(_CrestOceanMaskTexture, uvScreenSpace).x;
			float sceneZ01 =  tex2D(_CameraDepthTexture, uvScreenSpace).x;
			float oceanSceneZ01 =  tex2D(_CrestOceanMaskDepthTexture, uvScreenSpace).x;
			if(oceanSceneZ01 > sceneZ01)
			{
				sceneZ01 = oceanSceneZ01;
			}
			const bool isBelowHorizon = dot(uvScreenSpace - _CrestHorizonPosNormal.xy, _CrestHorizonPosNormal.zw) > 0.0;
			bool isUnderwater = oceanMask == UNDERWATER_MASK_WATER_SURFACE_BELOW || (isBelowHorizon && oceanMask != UNDERWATER_MASK_WATER_SURFACE_ABOVE);

			float sceneDepth = LinearEyeDepth(sceneZ01) - surfaceZ;
			float fog = saturate(1.0 - exp(-_DepthFogDensity.xyz * sceneDepth));

			if(isUnderwater)
			{
				color.a += (sceneDepth) * 0.008;
				color.r = 1.0;
			}

			// sceneColour = ApplyUnderwaterEffect(
			// 	_LD_TexArray_AnimatedWaves,
			// 	_Normals,
			// 	_WorldSpaceCameraPos,
			// 	_AmbientLighting,
			// 	sceneColour,
			// 	sceneZ01,
			// 	view,
			// 	_DepthFogDensity,
			// 	isOceanSurface
			// );
		}
		ENDCG
	}
	FallBack "Diffuse"
}
