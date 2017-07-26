// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Shape/Gerstner Waves"
{
	Properties
	{
	}

	Category
	{
		Tags { "Queue"="Geometry" }

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode" = "Always" }
			
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"
				#define PI 3.141592653

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 worldPos : TEXCOORD0;
				};

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );
					o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
					return o;
				}

				// respects the gui option to freeze time
				uniform float _MyTime;

				void AddGerstner( float3 worldPos, float amp, float2 k, float wavelength, float speed, inout float3 disp )
				{
					// Hacked in for now. Wavelength and wave speed should be dependent on eachother and gravity.
					float s = dot( k, worldPos.xz ) + speed*_MyTime;
					disp.xz += amp*(.35 * k * sin( 2.*PI*s / wavelength ));
					disp.y += amp*(-.25 * cos( 2.*PI*s / wavelength ));
				}

				float4 frag (v2f i) : SV_Target
				{
					i.worldPos.y = 0.;

					float3 disp = (float3)0.;

					// Unoptimized hardcoded gerstner waves.

					AddGerstner( i.worldPos, 20., normalize( float2(-1., -1.3) ), 150., .2, disp );
					AddGerstner( i.worldPos, 11., normalize( float2(-3., -1.3) ), 93., .6, disp );
					AddGerstner( i.worldPos, 5.3, normalize( float2(1.4, -1.3) ), 43., .9, disp );
					AddGerstner( i.worldPos, 2.3, normalize( float2(0., -1.3) ), 23., 1.6, disp );
					AddGerstner( i.worldPos, 1.5, normalize( float2(-0.6, -1.3) ), 13., 2.3, disp );

					return float4( disp, 1.0 );
				}

				ENDCG
			}
		}
	}
}
