// This shader takes a simulation result, zooms in on it (2X scale), and then adds it to the target. Accumulation is done into the W channel
// of the target, so that it won't interfere with the simulation itself (which uses channels x,y,z).
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
				o.uv = 0.25 + 0.5*v.uv;
				return o;
			}
			
			sampler2D _MainTex;

			half4 frag (v2f i) : SV_Target
			{
				half4 simData = tex2D(_MainTex, i.uv);

				// combine simulation results into w channel. dont mess with xyz - this would mess with the simulation
				return half4( 0., 0., 0., simData.x + simData.w );
			}
			ENDCG
		}
	}
}
