// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// A single Gerstner Octave
Shader "Ocean/Shape/Wave Particle"
{
	Properties
	{
		_Amplitude( "Amplitude", float ) = 1
		_Radius( "Radius", float) = 3
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
				Blend One One
			
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 worldOffsetNorm : TEXCOORD0;
				};

				uniform float _TexelsPerWave;
				uniform float _Radius;

				bool SamplingIsAdequate( float minWavelengthInShape )
				{
					const float cameraWidth = 2. * unity_OrthoParams.x;
					const float renderTargetRes = _ScreenParams.x;
					const float texSize = cameraWidth / renderTargetRes;
					const float minWavelength = texSize * _TexelsPerWave;
					return minWavelengthInShape > minWavelength;
				}

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );

					float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
					o.worldOffsetNorm = worldPos - mul( unity_ObjectToWorld, float4(0., 0., 0., 1.) ).xyz;
					o.worldOffsetNorm /= _Radius;

					// if wavelength is too small, kill this quad so that it doesnt render any shape
					float wavelength = 2. * _Radius;
					if( !SamplingIsAdequate(wavelength) )
						o.vertex.xy *= 0.;

					return o;
				}

				uniform float _Amplitude;

				float4 frag (v2f i) : SV_Target
				{
					float4 disp;
					disp.xyz = (float3)0.;
					disp.w = 1.;

					// power 4 smoothstep - no normalize needed
					float r2 = dot( i.worldOffsetNorm.xz, i.worldOffsetNorm.xz );
					if( r2 < 1. )
					{
						r2 = 1. - r2;
						disp.y = r2 * r2 * _Amplitude;
						disp.xz = -.35 * r2 * i.worldOffsetNorm.xz * _Radius;
					}

					return disp;
				}

				ENDCG
			}
		}
	}
}
