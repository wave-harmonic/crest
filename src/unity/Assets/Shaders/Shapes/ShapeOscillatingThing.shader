// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Shape/Oscillating Thing"
{
	Properties
	{
		_Amplitude( "Amplitude", float ) = 1
		_Radius( "Radius", float) = 3
	}

	Category
	{
		// base simulation runs on the Geometry queue, before this shader.
		// this shader adds interaction forces on top of the simulation result.
		Tags { "Queue"="Transparent" }

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
					float3 worldOffset : TEXCOORD0;
					float texSize : TEXCOORD1;
				};

				// respects the gui option to freeze time
				uniform float _MyTime;

				uniform float _TexelsPerWave;
				uniform float _Radius;

				float ComputeTexelSize()
				{
					const float cameraWidth = 2. * unity_OrthoParams.x;
					const float renderTargetRes = _ScreenParams.x;
					return cameraWidth / renderTargetRes;
				}

				bool SamplingIsAdequate( float minWavelengthInShape, float texSize )
				{
					const float minWavelength = texSize * _TexelsPerWave;
					return minWavelengthInShape > minWavelength;
				}

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );

					float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
					o.worldOffset = worldPos - mul(unity_ObjectToWorld, float4(0., 0., 0., 1.)).xyz;

					// if wavelength is too small, kill this quad so that it doesnt render any shape
					o.texSize = ComputeTexelSize();
					// this shape function below is weird - it has multiple components at different scales. each component
					// is based on a smoothstep, with radius equal to _Radius*i.texSize.
					float wavelength = 2. * _Radius * o.texSize;
					if( !SamplingIsAdequate( wavelength, o.texSize ) )
						o.vertex.xy *= 0.;

					return o;
				}

				uniform float _Amplitude;

				float4 frag( v2f i ) : SV_Target
				{
					// core shape. note this has a dependency on sampling res (texSize), so will produce different shape components depending on
					// shape texture res.
					float y = smoothstep( _Radius*i.texSize, 0.25*_Radius*i.texSize, length(i.worldOffset - i.texSize));

					// oscillation
					y *= sin(10.*_MyTime / sqrt(i.texSize) + i.texSize);

					// amplitude, also has scale dependency
					y *= _Amplitude * sqrt(i.texSize);

					float dt = 1. / 60.;

					return float4( dt * y, 0., 0., 0.);
				}

				ENDCG
			}
		}
	}
}
