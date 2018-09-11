Shader "Ocean/ShadowUpdate"
{
	Properties
	{
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

			#pragma enable_d3d11_debug_symbols

			// this turns on all the shadowy stuff
			#define SHADOW_COLLECTOR_PASS

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				V2F_SHADOW_COLLECTOR;
			};

			uniform float3 _CenterPos;
			uniform float3 _Scale;
			uniform float3 _CamPos;
			uniform float3 _CamForward;
			uniform float3 _OceanCenterPosWorld;

			v2f vert (appdata v)
			{
				v2f o;

				// the code below is baked out and specialised from C:\Program Files\Unity\Editor\Data\CGIncludes\UnityCG.cginc
				// TRANSFER_SHADOW_COLLECTOR

				o.pos = UnityObjectToClipPos(v.vertex);

				// world pos from [0,1] quad
				float4 wpos = float4(float3(v.vertex.x - 0.5, 0.0, v.vertex.y - 0.5) * _Scale.xzy * 4. + _CenterPos, 1.);

				// TODO sea level... or maybe wave height/disp??
				wpos.y = _OceanCenterPosWorld.y;

				o._WorldPosViewZ.xyz = wpos.xyz;
				o._WorldPosViewZ.w = dot(wpos.xyz - _CamPos, _CamForward);

				o._ShadowCoord0 = mul(unity_WorldToShadow[0], wpos).xyz;
				o._ShadowCoord1 = mul(unity_WorldToShadow[1], wpos).xyz;
				o._ShadowCoord2 = mul(unity_WorldToShadow[2], wpos).xyz;
				o._ShadowCoord3 = mul(unity_WorldToShadow[3], wpos).xyz;

				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				SHADOW_COLLECTOR_FRAGMENT(i);
			}
			ENDCG
		}
	}
}
