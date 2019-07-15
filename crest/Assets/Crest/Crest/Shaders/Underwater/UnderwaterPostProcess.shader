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

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "../OceanLODData.hlsl"
			#include "UnderwaterShared.hlsl"

			float _CrestTime;

			#include "../OceanEmission.hlsl"

			float _HorizonHeight;
			float _HorizonRoll;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				// The pixel position in world space
				float3 positionWS : TEXCOORD1;
			};

			Varyings Vert (Attributes input)
			{
				Varyings output;
				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.uv = input.uv;

				// TODO(UPP): Properly calculate the output position of a pixel in
				// worldspace
				{
					// view coordinate frame for camera
					const float3 right   = unity_CameraToWorld._11_21_31;
					const float3 up      = unity_CameraToWorld._12_22_32;
					const float3 forward = unity_CameraToWorld._13_23_33;

					const float3 nearPlaneCenter = _WorldSpaceCameraPos + forward * _ProjectionParams.y * 1.001;
					// Spread verts across the near plane.
					const float aspect = _ScreenParams.x / _ScreenParams.y;
					output.positionWS = nearPlaneCenter
						+ 2.6 * unity_CameraInvProjection._m11 * aspect * right * input.positionOS.x * _ProjectionParams.y
						+ up * input.positionOS.z * _ProjectionParams.y;
				}
				return output;
			}

			sampler2D _MainTex;
			sampler2D _MaskTex;
			sampler2D _MaskDepthTex;

			// In-built Unity textures
			sampler2D _CameraDepthTexture;
			sampler2D _Normals;

			half3 ApplyUnderwaterEffect(half3 sceneColour, const float sceneZ01, const float3 positionWS)
			{
				const float sceneZ = LinearEyeDepth(sceneZ01);
				const half3 view = normalize(_WorldSpaceCameraPos - positionWS);
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
						sceneColour = ApplyUnderwaterEffect(sceneColour, sceneZ01, input.positionWS);
					}
				}

				return half4(sceneColour, 1.0);
			}
			ENDCG
		}
	}
}
