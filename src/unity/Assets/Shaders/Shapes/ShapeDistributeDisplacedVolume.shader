// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Expands geoemtry along normals and distributes a given volume of water among the rasterized pixels.
// This is early/experimental and not tidied up or optimized!

Shader "Ocean/Shape/Distribute Displaced Volume"
{
	Properties
	{
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" }
		LOD 100

		Pass
		{
			Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 n : NORMAL0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 n : NORMAL0;
				float2 worldOffset : TEXCOORD1;
			};

			uniform float _displacedVPerLod;
			#define FEATHER 2.

			v2f vert (appdata v)
			{
				v2f o;

				if (abs(_displacedVPerLod) < 0.01)
				{
					o.vertex = (float4)0.;
					o.n = o.vertex.xyz;
					o.uv = o.vertex.xy;
					o.worldOffset = o.vertex.xyz;
					return o;
				}

				o.uv = v.uv;

				o.n = mul(unity_ObjectToWorld, float4(v.n, 0.)).xyz;
				o.n = normalize(o.n);

				const float cameraWidth = 2. * unity_OrthoParams.x;
				const float renderTargetRes = _ScreenParams.x;
				const float texSize = cameraWidth / renderTargetRes;

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				worldPos += o.n * FEATHER * texSize; // (.5 + .5*sin(_Time.y));
				worldPos.y = 0.;

				o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.));

				float3 centerPos = unity_ObjectToWorld._m03_m13_m23;
				o.worldOffset = worldPos.xz - centerPos.xz;

				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				fixed3 col;
				
				col.xyz = (i.n);

				// diameter is length of x axis (x scale), assumig circle crossection
				float diameter = length(unity_ObjectToWorld._m00_m10_m20);

				const float cameraWidth = 2. * unity_OrthoParams.x;
				const float renderTargetRes = _ScreenParams.x;
				const float texSize = cameraWidth / renderTargetRes;

				float feath = FEATHER * texSize;
				float r = length(i.worldOffset);
				float d = abs(2.*r - diameter);
				clip(feath - d);
				float wt = sqrt(smoothstep(feath, 0., d));

				float circum = 3.14159 * diameter;

				float texCount = circum / texSize; // approximation

				float texV = _displacedVPerLod / texCount;

				float texArea = texSize * texSize;
				float texH = wt * texV / texArea;

				return float4(.05*texH, 0., 0., 0.);
			}
			ENDCG
		}
	}
}
