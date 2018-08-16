// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Shape/Sim/Wave Add To Disps"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 screenPos : TEXCOORD0;
			};

			sampler2D _MainTex;
			uniform float _HorizDisplace;
			uniform float _DisplaceClamp;
			uniform float _TexelWidth;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				// sim texture and displacement texture are assumed to be exactly aligned, so just copy texture to texture.
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				half simData = tex2D(_MainTex, i.screenPos).x;
				
				// compute displacement from gradient of water surface - discussed in issue #18 and then in issue #47
				float2 invRes = float2(1. / _ScreenParams.x, 0.);
				half simData_x = tex2D(_MainTex, i.screenPos + invRes.xy).x;
				half simData_z = tex2D(_MainTex, i.screenPos + invRes.yx).x;
				float2 dispXZ = _HorizDisplace * (float2(simData_x, simData_z) - simData) / _TexelWidth;
				dispXZ = clamp(dispXZ, -_TexelWidth * _DisplaceClamp, _TexelWidth * _DisplaceClamp);

				return half4(dispXZ.x, simData, dispXZ.y, 0.);
			}
			ENDCG
		}
	}
}
