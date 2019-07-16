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

			#pragma enable_d3d11_debug_symbols

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "../OceanLODData.hlsl"

			float _CrestTime;

			#include "../OceanEmission.hlsl"

			float _HorizonHeight;
			float _HorizonRoll;
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
				float3 viewWS : TEXCOORD1;
			};

			Varyings Vert (Attributes input)
			{
				Varyings output;
				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.uv = input.uv;

				{
					const float2 pixelCS = input.uv * 2 - float2(1.0, 1.0);
					const float4 pixelWS = mul(_InvViewProjection, float4(pixelCS, 1.0, 1.0));
					output.viewWS = (pixelWS.xyz/pixelWS.w) - _WorldSpaceCameraPos;
				}
				return output;
			}

			sampler2D _MainTex;
			sampler2D _MaskTex;
			sampler2D _MaskDepthTex;

			// In-built Unity textures
			sampler2D _CameraDepthTexture;
			sampler2D _Normals;

			half3 ApplyUnderwaterEffect(half3 sceneColour, const float sceneZ01, const half3 view)
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
				if (sceneZ01 != 0.0)
				{
					ApplyCaustics(view, lightDir, sceneZ, _Normals, true, sceneColour);
				}
#endif // _CAUSTICS_ON

				return lerp(sceneColour, scatterCol, 1.0 - exp(-_DepthFogDensity.xyz * sceneZ));
			}

			fixed4 Frag (Varyings input) : SV_Target
			{
				half3 sceneColour = tex2D(_MainTex, input.uv).rgb;
				bool isBelowHorizon = false;
				{
					// TODO(UPP): Create a cheap and accurate equation for
					// determining if we are below the horizon that can work
					// with any camera orientation

					// NOTE: I tried to do this by checking if the y component
					// of the view vector was less than 0, but the assumption
					// that the horizon extends-out to infinity was too grand.
					// We need to workout exactly to which point the ocean
					// horizon extends.
					isBelowHorizon = input.uv.y < _HorizonHeight;
				}

				const float sceneZ01 = tex2D(_CameraDepthTexture, input.uv).x;

				bool isUnderwater = false;
				bool isSurface = false;
				{
					int mask = tex2D(_MaskTex, input.uv);
					const float maskDepth = tex2D(_MaskDepthTex, input.uv);
					isSurface = mask != 0 && (sceneZ01 < maskDepth);
					isUnderwater = mask == 2 || (isBelowHorizon && mask != 1);
				}

				if(isUnderwater)
				{
					if(!isSurface)
					{
						const half3 view = normalize(input.viewWS);
						sceneColour = ApplyUnderwaterEffect(sceneColour, sceneZ01, view);
					}
				}

				return half4(sceneColour, 1.0);
			}
			ENDCG
		}
	}
}
