// This shader takes a simulation result, zooms in on it (2X scale), and then adds it to the target. Accumulation is done into the Z channel
// of the target, so that it won't interfere with the simulation itself (which uses channels x,y,w).
// This is run on each sim lod from largest to smallest, to accumulate the results downwards.
Shader "Ocean/Shape/Sim/Combine"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
		Blend One One

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
			
			sampler2D _MainTex;

			// shape data
			// Params: float3(texel size, texture resolution, shape weight multiplier)
			#define SHAPE_LOD_PARAMS(LODNUM) \
					uniform sampler2D _WD_Sampler_##LODNUM; \
					uniform float3 _WD_Params_##LODNUM; \
					uniform float2 _WD_Pos_##LODNUM; \
					uniform float2 _WD_Pos_Cont_##LODNUM; \
					uniform int _WD_LodIdx_##LODNUM;

			SHAPE_LOD_PARAMS(0)
			SHAPE_LOD_PARAMS(1)
			;

			float2 worldToUV(in float2 i_samplePos, in float2 i_centerPos, in float i_res, in float i_texelSize)
			{
				return (i_samplePos - i_centerPos) / (i_texelSize*i_res) + 0.5;
			}

			float2 uvToWorld(in float2 i_uv, in float2 i_centerPos, in float i_res, in float i_texelSize)
			{
				return i_texelSize * i_res * (i_uv - 0.5) + i_centerPos;
			}

			half4 frag (v2f i) : SV_Target
			{
				// go from uv out to world for the current shape texture
				float2 worldPos = uvToWorld(i.uv, _WD_Pos_0, _WD_Params_0.y, _WD_Params_0.x);

				// sample the shape 1 texture at this world pos
				float2 uv_1 = worldToUV(worldPos, _WD_Pos_1, _WD_Params_1.y, _WD_Params_1.x);

				half4 simData = tex2D(_MainTex, uv_1);

				// combine simulation results into w channel. dont mess with xyz - this would mess with the simulation
				half4 result = half4(0., 0., simData.x + simData.z, simData.w); // ( h, hprev, cumulative h, cumulative foam )
				
				// this fades out the big lod, so that it is not noticeable when it pops in/out when height changes
				return _WD_Params_1.z * result;
			}
			ENDCG
		}
	}
}
