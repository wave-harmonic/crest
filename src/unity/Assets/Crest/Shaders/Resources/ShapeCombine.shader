// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Ocean/Simulation/Combine Animated Wave LODs"
{
	Properties
	{
	}

	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			#include "../../Shaders/OceanLODData.hlsl"
			;

			uniform float _HorizDisplace;
			uniform float _DisplaceClamp;

			half4 frag (v2f i) : SV_Target
			{
				// go from uv out to world for the current shape texture
				const float2 worldPosXZ = LD_0_UVToWorld(i.uv);

				// sample the shape 1 texture at this world pos
				const float2 uv_1 = LD_1_WorldToUV(worldPosXZ);

				float3 result = 0.;

				// this lods waves
				SampleDisplacements(_LD_Sampler_AnimatedWaves_0, i.uv, 1.0, 0.0, 0., result);

				// waves to combine down from the next lod up the chain
				SampleDisplacements(_LD_Sampler_AnimatedWaves_1, uv_1, 1.0, 0.0, 0., result);

				// convert dynamic wave sim to displacements
				{
					half waveSimY = tex2Dlod(_LD_Sampler_DynamicWaves_0, float4(i.uv, 0., 0.)).x;
					result.y += waveSimY;

					// compute displacement from gradient of water surface - discussed in issue #18 and then in issue #47
					const float2 invRes = float2(_LD_Params_0.w, 0.);
					const half waveSimY_x = tex2Dlod(_LD_Sampler_DynamicWaves_0, float4(i.uv + invRes.xy, 0., 0.)).x;
					const half waveSimY_z = tex2Dlod(_LD_Sampler_DynamicWaves_0, float4(i.uv + invRes.yx, 0., 0.)).x;
					float2 dispXZ = _HorizDisplace * (float2(waveSimY_x, waveSimY_z) - waveSimY) / _LD_Params_0.x;

					const float maxDisp = _LD_Params_0.x * _DisplaceClamp;
					dispXZ = clamp(dispXZ, -maxDisp, maxDisp);

					result.xz += dispXZ;
				}

				return half4(result, 1.);
			}
			ENDCG
		}
	}
}
