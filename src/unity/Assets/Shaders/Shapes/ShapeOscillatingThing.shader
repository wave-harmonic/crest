// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Shape/Oscillating Thing"
{
	Properties
	{
		_Amplitude( "Amplitude", float ) = 1
		_Radius( "Radius", float) = 3
		_Omega( "Omega", float) = 3
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
				uniform float _MyDeltaTime;

				uniform float _TexelsPerWave;
				uniform float _Radius;
				uniform float _Omega;

				float ComputeTexelSize()
				{
					const float cameraWidth = 2. * unity_OrthoParams.x;
					const float renderTargetRes = _ScreenParams.x;
					return cameraWidth / renderTargetRes;
				}

				bool SamplingIsAppropriate( float wavelengthInShape, float texSize )
				{
					const float minWavelength = texSize * _TexelsPerWave;
					return wavelengthInShape > minWavelength && wavelengthInShape <= 2.*minWavelength;
				}

				v2f vert( appdata_t v )
				{
					v2f o;

					float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
					float3 centerPos = unity_ObjectToWorld._m03_m13_m23;
					o.worldOffset = worldPos - centerPos;

					// shape is symmetric around center with known radius - fix the vert positions to perfectly wrap the shape.
					o.worldOffset.x = _Radius * sign(o.worldOffset.x);
					o.worldOffset.y = 0.;
					o.worldOffset.z = _Radius * sign(o.worldOffset.z);
					o.vertex = mul(UNITY_MATRIX_VP, float4(centerPos + o.worldOffset, 1.));

					// clamp worldPos to be
					// if wavelength is too small, kill this quad so that it doesnt render any shape
					o.texSize = ComputeTexelSize();
					// this shape function below is weird - it has multiple components at different scales. each component
					// is based on a smoothstep, with radius equal to _Radius*i.texSize.
					float wavelength = 2. * _Radius;
					if( !SamplingIsAppropriate( wavelength, o.texSize ) )
						o.vertex.xy *= 0.;

					return o;
				}

				uniform float _Amplitude;

				float4 frag( v2f i ) : SV_Target
				{
					// core shape
					float y = smoothstep(_Radius, 0.25*_Radius, length(i.worldOffset.xz));

					// oscillation
					y *= sin(_MyTime * _Omega);

					// amplitude
					y *= _Amplitude;

					// treat as an acceleration - dt^2
					return float4( _MyDeltaTime * _MyDeltaTime * y, 0., 0., 0. );
				}

				ENDCG
			}
		}
	}
}
