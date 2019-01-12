Shader "Ocean/Inputs/Flow/Fixed Direction"
{
	Properties
	{
		_Speed("Speed", Range(0.0, 10.0)) = 1.0
		_Direction("Direction", Range(0.0, 1.0)) = 0.0
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 vel : TEXCOORD0;
			};

			float _Speed;
			float _Direction;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.vel = _Speed * float2(cos(_Direction * 6.283185), sin(_Direction * 6.283185));
				return o;
			}
			
			float2 frag(v2f i) : SV_Target
			{
				return i.vel;
			}
			ENDCG
		}
	}
}
