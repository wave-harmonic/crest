// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// persistent foam sim
Shader "Ocean/Shape/Sim/Foam"
{
	Properties {
	}

	Category
	{
		// Base simulation runs first on geometry queue, no blending.
		// Any interactions will additively render later in the transparent queue.
		Tags { "Queue" = "Geometry" }

		SubShader {
			Pass {

				Name "BASE"
				Tags{ "LightMode" = "Always" }

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"
				#include "../../Shaders/OceanLODData.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float4 uv : TEXCOORD0;
					float2 uv_uncompensated : TEXCOORD1;
				};

				uniform float3 _CameraPositionDelta;

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					// compute uncompensated uv
					o.uv_uncompensated.xy = o.vertex.xy;
					o.uv_uncompensated.y = -o.uv_uncompensated.y;
					o.uv_uncompensated.xy = 0.5*o.uv_uncompensated.xy + 0.5;

					// compensate for camera motion - adjust lookup uv to get texel from last frame sim
					o.uv.xy = o.uv_uncompensated;
					o.uv.zw = float2(1., 1.) / _ScreenParams.xy;
					const float texelSize = 2. * unity_OrthoParams.x * o.uv.z; // assumes square RT
					o.uv.xy += o.uv.zw * _CameraPositionDelta.xz / texelSize;

					return o;
				}

				// respects the gui option to freeze time
				uniform float _MyTime;
				uniform float _MyDeltaTime;

				uniform sampler2D _FoamLastFrame;

				half frag(v2f i) : SV_Target
				{
					float4 uv = float4(i.uv.xy, 0., 0.);
					float3 e = float3(i.uv.zw, 0.);

					// sampler will clamp the uv currently
					half last = tex2Dlod(_FoamLastFrame, uv).x;
					half2 r = abs(uv.xy - 0.5);
					if (max(r.x, r.y) > 0.5)
					{
						// no border wrap mode for RTs in unity it seems, so make any off-texture reads 0 manually
						last = 0.;
					}

					// sample waves
					const float3 dd = float3(_WD_Params_0.w, 0.0, _WD_Params_0.x);
					half3 s = tex2Dlod(_WD_Sampler_0, uv).xyz;
					half3 sx = tex2Dlod(_WD_Sampler_0, uv + dd.xyyy).xyz;
					half3 sz = tex2Dlod(_WD_Sampler_0, uv + dd.yxyy).xyz;

					float3 disp = s.xyz;
					float3 disp_x = dd.zyy + sx.xyz;
					float3 disp_z = dd.yyz + sz.xyz;

					// The determinant of the displacement Jacobian is a good measure for turbulence:
					// > 1: Stretch
					// < 1: Squash
					// < 0: Overlap
					float4 du = float4(disp_x.xz, disp_z.xz) - disp.xzxz;
					float det = (du.x * du.w - du.y * du.z) / (_WD_Params_0.x * _WD_Params_0.x);

					const float _WaveFoamStrength = 2.;
					const float _WaveFoamCoverage = 0.95;
					last += _MyDeltaTime * _WaveFoamStrength * saturate(_WaveFoamCoverage - det);

					const float foamFadeRate = 0.4;
					last *= max(0.0, 1.0 - foamFadeRate * _MyDeltaTime);

					return last;
				}
				ENDCG
			}
		}
	}
}
