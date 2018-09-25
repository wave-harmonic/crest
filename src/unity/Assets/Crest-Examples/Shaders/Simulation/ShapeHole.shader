// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Shape/Hole"
{
	Properties
	{
	}

	SubShader
	{
		// this shader adds interaction forces on top of the simulation result.
		Tags{ "Queue" = "Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			//Blend On

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// TODO - why doesnt this suppress the divide by 0 warning??
			// https://docs.microsoft.com/en-us/windows/desktop/direct3dhlsl/hlsl-errors-and-warnings
			#pragma warning disable 4008

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				return float4((float3)-1./0., 1.);
			}
			ENDCG
		}
	}
}
