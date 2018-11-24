// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Inputs/Animated Waves/Wave Particle"
{
	Properties
	{
		_Amplitude( "Amplitude", float ) = 1
		_Radius( "Radius", float) = 3
	}

	Category
	{
		Tags { "Queue"="Transparent" "DisableBatching" = "True" }

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode" = "Always" }
				Blend One One
			
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 worldOffsetScaledXZ : TEXCOORD0;
				};

				uniform float _Radius;

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );

					float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
					float3 centerPos = unity_ObjectToWorld._m03_m13_m23;
					o.worldOffsetScaledXZ = worldPos.xz - centerPos.xz;

					// shape is symmetric around center with known radius - fix the vert positions to perfectly wrap the shape.
					o.worldOffsetScaledXZ = sign(o.worldOffsetScaledXZ);
					float4 newWorldPos = float4(centerPos, 1.);
					newWorldPos.xz += o.worldOffsetScaledXZ * _Radius;
					o.vertex = mul(UNITY_MATRIX_VP, newWorldPos);

					return o;
				}

				uniform float _Amplitude;

				float4 frag (v2f i) : SV_Target
				{
					// power 4 smoothstep - no normalize needed
					// credit goes to stubbe's shadertoy: https://www.shadertoy.com/view/4ldSD2
					float r2 = dot( i.worldOffsetScaledXZ, i.worldOffsetScaledXZ);
					if( r2 > 1. )
						return (float4)0.;

					r2 = 1. - r2;

					float y = r2 * r2 * _Amplitude;

					return float4(0., y, 0., 0.);
				}

				ENDCG
			}
		}
	}
}
