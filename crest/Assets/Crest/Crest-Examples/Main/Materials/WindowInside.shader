Shader "Crest/Material/WindowInside"
{
	Properties
	{
		_Albedo ("Albedo", Color) = (1,1,1,1)
		_Normal ("Normal", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags { "Queue" = "Geometry+511" "RenderType"="Transparent" }
		GrabPass {
			Name "OceanGrab"
		}

		Pass
		{
			Name "ApplyFog"
			Blend Off
			HLSLPROGRAM
				#pragma vertex Vert
				#pragma fragment Frag
				#include "UnityCG.cginc"

				struct Attributes
				{
					float4 positionOS : POSITION;
					float2 uv         : TEXCOORD0;
				};

				struct Varyings {
					float4 positionCS : POSITION;
					float4 screenPos  : TEXCOORD0;
					float3 viewWS     : TEXCOORD1;
				};

				Varyings Vert (Attributes input)
				{
					Varyings output;
					output.positionCS = UnityObjectToClipPos(input.positionOS);
					output.screenPos = ComputeScreenPos(output.positionCS);
					output.viewWS = _WorldSpaceCameraPos -  mul(unity_ObjectToWorld, float4(input.positionOS.xyz, 1.0));
					return output;
				}

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

				void CrestApplyUnderwaterFog (in float4 screenPos, in float3 viewWS, inout fixed4 sceneColour)
				{
					float2 uvScreenSpace = screenPos.xy / screenPos.w;
					float surfaceZ = screenPos.z / screenPos.w;

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

					float sceneDepth = LinearEyeDepth(sceneZ01) - surfaceZ;

					if(isUnderwater)
					{
						// TODO(TRC):Now
						half3 view = normalize(viewWS);
						sceneColour.xyz = ApplyUnderwaterEffect(
							_LD_TexArray_AnimatedWaves,
							_Normals,
							_WorldSpaceCameraPos,
							_CrestAmbientLighting,
							sceneColour.xyz,
							sceneDepth,
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
					half4 color = tex2D(_GrabTexture, input.screenPos.xy / input.screenPos.w);
					CrestApplyUnderwaterFog(input.screenPos, input.viewWS, color);
					return color;
				}


			ENDHLSL
		}

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:blend

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _Normal;

		struct Input
		{
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Albedo;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			// Albedo comes from a texture tinted by color
			fixed4 c = _Albedo;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Normal = UnpackNormal(tex2D (_Normal, IN.uv_MainTex));
			o.Alpha = 0;//c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
