Shader "Crest/Underwater Post Process"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma shader_feature _SUBSURFACESCATTERING_ON
			#pragma shader_feature _SUBSURFACESHALLOWCOLOUR_ON
			#pragma shader_feature _TRANSPARENCY_ON
			#pragma shader_feature _CAUSTICS_ON
			#pragma shader_feature _SHADOWS_ON
			#pragma shader_feature _COMPILESHADERWITHDEBUGINFO_ON

			#pragma multi_compile __ _FULL_SCREEN_EFFECT
			#pragma multi_compile __ _DEBUG_VIEW_OCEAN_MASK

			#if _COMPILESHADERWITHDEBUGINFO_ON
			#pragma enable_d3d11_debug_symbols
			#endif

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "../OceanLODData.hlsl"

			float _CrestTime;
			half3 _AmbientLighting;

			#include "../OceanEmission.hlsl"

			float _OceanHeight;
			float4x4 _InvViewProjection;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert (Attributes input)
			{
				Varyings output;
				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.uv = input.uv;
				return output;
			}

			sampler2D _MainTex;
			sampler2D _MaskTex;
			sampler2D _MaskDepthTex;

			// In-built Unity textures
			sampler2D _CameraDepthTexture;
			sampler2D _Normals;

			half3 ApplyUnderwaterEffect(half3 sceneColour, const float sceneZ01, const half3 view, bool isOceanSurface)
			{
				const float sceneZ = LinearEyeDepth(sceneZ01);
				const float3 lightDir = _WorldSpaceLightPos0.xyz;

				float3 surfaceAboveCamPosWorld = 0.0;
				half3 scatterCol = 0.0;
				{
					half sss = 0.;
					const float3 uv_slice = WorldToUV(_WorldSpaceCameraPos.xz);
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice, 1.0, surfaceAboveCamPosWorld, sss);
					surfaceAboveCamPosWorld.y += _OceanCenterPosWorld.y;

					// depth and shadow are computed in ScatterColour when underwater==true, using the LOD1 texture.
					const float depth = 0.0;
					const half shadow = 1.0;

					scatterCol = ScatterColour(surfaceAboveCamPosWorld, depth, _WorldSpaceCameraPos, lightDir, view, shadow, true, true, sss);
				}

#if _CAUSTICS_ON
				if (sceneZ01 != 0.0 && !isOceanSurface)
				{
					ApplyCaustics(view, lightDir, sceneZ, _Normals, true, sceneColour);
				}
#endif // _CAUSTICS_ON

				return lerp(sceneColour, scatterCol, 1.0 - exp(-_DepthFogDensity.xyz * sceneZ));
			}

			fixed4 Frag (Varyings input) : SV_Target
			{
				float3 viewWS;
				float farPlanePixelHeight;
				{
					// We calculate these values in the pixel shader as
					// calculating them in the vertex shader results in
					// precision errors.
					const float2 pixelCS = input.uv * 2 - float2(1.0, 1.0);
					const float4 pixelWS_H = mul(_InvViewProjection, float4(pixelCS, 1.0, 1.0));
					const float3 pixelWS = pixelWS_H.xyz / pixelWS_H.w;
					viewWS = _WorldSpaceCameraPos - pixelWS;
					farPlanePixelHeight = pixelWS.y;
				}

				float wt = 1.0;
				{
					//
					const float2 pixelCS = input.uv * 2 - float2(1.0, 1.0);
					const float4 pixelWS_H = mul(_InvViewProjection, float4(pixelCS, -1.0, 1.0));
					const float3 pixelWS = pixelWS_H.xyz / pixelWS_H.w;

					half sss = 0.;
					float3 x = pixelWS;
					float3 disp = 0.0;
					for (int i = 0; i < 3; i++)
					{
						disp = 0.0;
						SampleDisplacements(_LD_TexArray_AnimatedWaves, WorldToUV_BiggerLod(x.xz), 1.0, disp, sss);
						x.xz -= (x.xz + disp.xz) - pixelWS.xz;
					}

					wt = clamp(abs(pixelWS.y - (_OceanCenterPosWorld.y+disp.y))*100.0 + 0.5, 0.0, 1.0);
				}

				#if !_FULL_SCREEN_EFFECT
				const bool isBelowHorizon = (farPlanePixelHeight <= _OceanHeight);
				#else
				const bool isBelowHorizon = true;
				#endif

				half3 sceneColour = tex2D(_MainTex, input.uv).rgb;

				float sceneZ01 = tex2D(_CameraDepthTexture, input.uv).x;
				bool isUnderwater = false;
				bool isOceanSurface = false;
				{
					int mask = tex2D(_MaskTex, input.uv);
					const float oceanDepth01 = tex2D(_MaskDepthTex, input.uv);
					isOceanSurface = mask != 0 && (sceneZ01 < oceanDepth01);
					isUnderwater = mask == 2 || (isBelowHorizon && mask != 1);
					sceneZ01 = isOceanSurface ? oceanDepth01 : sceneZ01;
				}
#if _DEBUG_VIEW_OCEAN_MASK
				int mask = tex2D(_MaskTex, input.uv);
				if(!isOceanSurface)
				{
					return float4(sceneColour * float3(isUnderwater * 0.5, (1.0 - isUnderwater) * 0.5, 1.0), 1.0);
				}
				else
				{
					return float4(sceneColour * float3(mask == 1, mask == 2, 0.0), 1.0);
				}
#else
				if(isUnderwater)
				{
					const half3 view = normalize(viewWS);
					sceneColour = ApplyUnderwaterEffect(sceneColour, sceneZ01, view, isOceanSurface);
				}

				return half4(wt * sceneColour, 1.0);
#endif // _DEBUG_VIEW_OCEAN_MASK
			}
			ENDCG
		}
	}
}
