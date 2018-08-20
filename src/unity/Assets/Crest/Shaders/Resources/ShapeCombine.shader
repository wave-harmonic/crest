// This shader takes a shape result, zooms in on it (2X scale), and then adds it to the target.
// This is run on each sim lod from largest to smallest, to accumulate the results downwards.
Shader "Ocean/Shape/Combine"
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
				float2 worldPos = LD_0_UVToWorld(i.uv);

				// sample the shape 1 texture at this world pos
				float2 uv_1 = LD_1_WorldToUV(worldPos);

				// return the shape data to be additively blended down the lod chain
				return tex2D(_MainTex, uv_1);
			}
			ENDCG
		}
	}
}
