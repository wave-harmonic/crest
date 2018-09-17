// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders alpha geometry overlaid on ocean surface. Samples the ocean shape texture in the vertex shader to track
// the surface. Requires the right texture to be assigned (see RenderAlphaOnSurface script).
Shader "Ocean/Underwater Skirt"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		//_Alpha("Alpha Multiplier", Range(0.0, 1.0)) = 1.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			//ZWrite Off
			// Depth offset to stop intersection with water. "Factor" and "Units". typical seems to be (-1,-1). (-0.5,0) gives
			// pretty good results for me when alpha geometry is fairly well matched but fails when alpha geo is too low res.
			// the ludicrously large value below seems to work in most of my tests.
			//Offset 0, -1000000
			//Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "../../Crest/Shaders/OceanLODData.hlsl"

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

			sampler2D _MainTex;
			float4 _MainTex_ST;

			// MeshScaleLerp, FarNormalsWeight, LODIndex (debug), unused
			uniform float4 _InstanceData;

			v2f vert (appdata v)
			{
				v2f o;

				// actually this should follow camera around
				float3 right   = mul((float3x3)unity_CameraToWorld, float3(1., 0., 0.));
				float3 up      = mul((float3x3)unity_CameraToWorld, float3(0., 1., 0.));
				float3 forward = mul((float3x3)unity_CameraToWorld, float3(0., 0., 1.));

				float3 center = _WorldSpaceCameraPos + forward * _ProjectionParams.y * 1.01;
				// todo - constant needs to depend on FOV
				float3 worldPos = center
					+ 3. * right * v.vertex.x
					+ up * v.vertex.z;

				// isolate topmost edge
				if (v.vertex.z > 0.45)
				{
					half2 nxz_dummy = (half2)0.;

					float2 sampleXZ = worldPos.xz;
					float3 disp;
					for (int i = 0; i < 6; i++)
					{
						// sample displacement textures, add results to current world pos / normal / foam
						disp = float3(sampleXZ.x, _OceanCenterPosWorld.y, sampleXZ.y);
						SampleDisplacements(_LD_Sampler_AnimatedWaves_0, LD_0_WorldToUV(sampleXZ), 1.0, _LD_Params_0.w, _LD_Params_0.x, disp, nxz_dummy);
						float3 nearestPointOnUp = worldPos + up * dot(disp - worldPos, up);
						float2 error = disp.xz - nearestPointOnUp.xz;
						sampleXZ -= error;
					}

					worldPos = disp;
				}
				else
				{
					worldPos -= 8. * up;
				}

				// view-projection
				o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.));

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			half4 frag(v2f i) : SV_Target
			{
				half4 col = half4(.05, .25, .3, .85);

				return col;
			}
			ENDCG
		}
	}
}
