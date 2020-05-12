Shader "Crest/Material/WindowInside"
{
	Properties
	{
		_Albedo ("Albedo", Color) = (1,1,1,1)
		_SurfaceNormal ("Normal", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags { "Queue" = "Geometry+511" "RenderType"="Transparent" }

		GrabPass {
			Name "CrestOceanGrabPass"
		}

		Pass
		{
			Name "CrestApplyFog"
			Blend Off
			CGPROGRAM
				#pragma vertex Vert
				#pragma fragment Frag
				#include "UnityCG.cginc"
				#include "Lighting.cginc"


				struct Attributes
				{
					float4 positionOS : POSITION;
					float2 uv         : TEXCOORD0;
				};

				struct Varyings {
					float4 positionCS : POSITION;
					float4 screenPos  : TEXCOORD0;
					float3 worldPos     : TEXCOORD1;
				};

				Varyings Vert (Attributes input)
				{
					Varyings output;
					output.positionCS = UnityObjectToClipPos(input.positionOS);
					output.screenPos = ComputeScreenPos(output.positionCS);
					output.worldPos = mul(unity_ObjectToWorld, input.positionOS);
					return output;
				}

				#pragma shader_feature _SUBSURFACESCATTERING_ON
				#pragma shader_feature _SUBSURFACESHALLOWCOLOUR_ON
				#pragma shader_feature _TRANSPARENCY_ON
				#pragma shader_feature _CAUSTICS_ON
				#pragma shader_feature _SHADOWS_ON
				#pragma shader_feature _COMPILESHADERWITHDEBUGINFO_ON
				#pragma shader_feature _MENISCUS_ON

				#pragma multi_compile __ _FULL_SCREEN_EFFECT
				#pragma multi_compile __ _DEBUG_VIEW_OCEAN_MASK

				#if _COMPILESHADERWITHDEBUGINFO_ON
				#pragma enable_d3d11_debug_symbols
				#endif

				#pragma enable_d3d11_debug_symbols


				half3 _CrestAmbientLighting;
				#include "../../../Crest/Shaders/OceanConstants.hlsl"
				#include "../../../Crest/Shaders/OceanInputsDriven.hlsl"
				#include "../../../Crest/Shaders/OceanGlobals.hlsl"
				#include "../../../Crest/Shaders/OceanLODData.hlsl"
				#include "../../../Crest/Shaders/OceanHelpersNew.hlsl"
				#include "../../../Crest/Shaders/OceanEmission.hlsl"

				float4 _CrestHorizonPosNormal;
				sampler2D _CrestOceanMaskTexture;
				sampler2D _CrestOceanMaskDepthTexture;

				#include "../../../Crest/Shaders/ApplyUnderwaterEffect.hlsl"

				sampler2D _Normals;
				sampler2D _CameraDepthTexture;

				void CrestApplyUnderwaterFog (in float2 uvScreenSpace, in float3 viewWS, in float surfaceZ01, inout fixed4 sceneColour)
				{
					// TODO(TRC):Now, break this all out into a helpfer function that will
					// also compute fog
					float oceanMask = tex2D(_CrestOceanMaskTexture, uvScreenSpace).x;
					float sceneZ01 =  tex2D(_CameraDepthTexture, uvScreenSpace).x;
					float oceanSceneZ01 =  tex2D(_CrestOceanMaskDepthTexture, uvScreenSpace).x;
					bool isOceanSurface = false;
					if(oceanSceneZ01 > sceneZ01)
					{
						sceneZ01 = oceanSceneZ01;
						isOceanSurface = true;
					}
					const bool isBelowHorizon = dot(uvScreenSpace - _CrestHorizonPosNormal.xy, _CrestHorizonPosNormal.zw) > 0.0;
					bool isUnderwater = oceanMask == UNDERWATER_MASK_WATER_SURFACE_BELOW || (isBelowHorizon && oceanMask != UNDERWATER_MASK_WATER_SURFACE_ABOVE);

					float sceneDepth = LinearEyeDepth(sceneZ01);
					float fogDistance = sceneDepth - LinearEyeDepth(surfaceZ01);

					// TODO(TRC):Now Figure out how to get the to the right value if this is occluded by another transparency.
					if(isUnderwater)
					{
						half3 view = normalize(viewWS);
						sceneColour.xyz = ApplyUnderwaterEffect(
							_LD_TexArray_AnimatedWaves,
							_Normals,
							_WorldSpaceCameraPos,
							_CrestAmbientLighting,
							sceneColour.xyz,
							sceneDepth,
							fogDistance,
							view,
							_DepthFogDensity,
							isOceanSurface
						);
					}

				}

				sampler2D _GrabTexture;
				float4 _GrabTexture_TexelSize;

				half4 Frag( Varyings input ) : COLOR
				{
					float2 uvScreenSpace = input.screenPos.xy / input.screenPos.w;
					half4 color = tex2D(_GrabTexture, uvScreenSpace);
					CrestApplyUnderwaterFog(uvScreenSpace, _WorldSpaceCameraPos - input.worldPos, input.positionCS.z, color);
					return color;
				}


			ENDCG
		}

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:blend

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _SurfaceNormal;

		struct Input
		{
			float2 uv_SurfaceNormal;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Albedo;
		#pragma enable_d3d11_debug_symbols

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			// Albedo comes from a texture tinted by color
			fixed4 c = fixed4(0.7607843, 0.7607843, 0.7607843, 0.2745098);//_Albedo;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = .7519999;
			o.Smoothness = 1;
			o.Normal = tex2D (_SurfaceNormal, IN.uv_SurfaceNormal * 3);
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
