// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Shape/Sim/Gen Foam From Waves"
{
	Properties
	{
		_DispTex ("Texture", 2D) = "black" {}
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Blend One One
		Cull Off Lighting Off ZWrite Off
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "../../Shaders/OceanLODData.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 screenPos : TEXCOORD0;
			};

			sampler2D _DispTex;
			uniform float4 _DispTex_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				// disp texture and foam texture are assumed to be exactly aligned, so just copy texture to texture.
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			half frag (v2f i) : SV_Target
			{
				//float4 uv = float4(i.screenPos, 0., 0.);

				////float3 dd = float3(i_geomSquareSize / (i_texelSize*i_res), 0.0, i_geomSquareSize);
				//float2 dd = float2(_DispTex_TexelSize.x, 0.);

				//float4 s = tex2Dlod(_DispTex, uv);
				//float4 sx = tex2Dlod(_DispTex, uv + dd.xyyy);
				//float4 sz = tex2Dlod(_DispTex, uv + dd.yxyy);

				//float3 disp = s.xyz;
				//float3 disp_x = dd.zyy + sx.xyz;
				//float3 disp_z = dd.yyz + sz.xyz;

				//// The determinant of the displacement Jacobian is a good measure for turbulence:
				//// > 1: Stretch
				//// < 1: Squash
				//// < 0: Overlap
				//float4 du = float4(disp_x.xz, disp_z.xz) - disp.xzxz;
				//float det = (du.x * du.w - du.y * du.z) / (dd.z * dd.z);

				//half _WaveFoamStrength = 1., _WaveFoamCoverage = 0.5;
				//half addFoam = _WaveFoamStrength * saturate(_WaveFoamCoverage - det);

				//return 0.98 * tex2D(_DispTex, i.screenPos).x + addFoam;

				return 0.5;
			}
			ENDCG
		}
	}
}
