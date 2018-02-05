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

			#include "../../Shaders/OceanLODData.cginc"
			;

			half4 frag (v2f i) : SV_Target
			{
				// go from uv out to world for the current shape texture
				float2 worldPos = WD_uvToWorld(i.uv, _WD_Pos_0, _WD_Params_0.y, _WD_Params_0.x);

				// sample the shape 1 texture at this world pos
				float2 uv_1 = WD_worldToUV(worldPos, _WD_Pos_1, _WD_Params_1.y, _WD_Params_1.x);

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
