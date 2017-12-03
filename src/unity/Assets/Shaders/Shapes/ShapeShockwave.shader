// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Shape/Shockwave"
{
	Properties
	{
		_Amplitude( "Amplitude", float ) = 1
		_Radius( "Radius", float) = 3
		_Velocity( "Velocity", Vector) = (0,0,0,0)
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
				//Blend One One
			
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
					float4 worldOffsetScaled : TEXCOORD0;
				};

				uniform float _TexelsPerWave;
				uniform float _Radius;
				uniform float2 _Velocity;

				bool SamplingIsAppropriate( float wavelengthInShape )
				{
					const float cameraWidth = 2. * unity_OrthoParams.x;
					const float renderTargetRes = _ScreenParams.x;
					const float texSize = cameraWidth / renderTargetRes;
					const float minWavelength = texSize * _TexelsPerWave;
					return wavelengthInShape > minWavelength && wavelengthInShape <= 2.*minWavelength;
				}

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );

					float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;

					o.worldOffsetScaled.xy = worldPos.xz - unity_ObjectToWorld._m03_m23;
					o.worldOffsetScaled.zw = o.worldOffsetScaled.xy + _Velocity.xy;

					o.worldOffsetScaled /= _Radius;

					// if wavelength is too small, kill this quad so that it doesnt render any shape
					float wavelength = 2. * _Radius;
					if( !SamplingIsAppropriate(wavelength) )
						o.vertex.xy *= 0.;

					return o;
				}

				uniform float _Amplitude;
				uniform float _MyDeltaTime;

				float cheapstep2D( float2 scaledOffset )
				{
					// power 4 smoothstep - no normalize needed
					float r2 = dot(scaledOffset, scaledOffset);
					if (r2 > 1.) return 0.;
					r2 = 1. - r2;
					return r2 * r2;
				}

				float4 frag (v2f i) : SV_Target
				{
					float y, ylast;
					y = 2.5*cheapstep2D(i.worldOffsetScaled.xy*float2(1.5,1.5)) - 1. * cheapstep2D(i.worldOffsetScaled.xy*.6);
					ylast = 2.5*cheapstep2D(i.worldOffsetScaled.zw*float2(1.5, 1.5)) - 1. * cheapstep2D(i.worldOffsetScaled.zw*.6);;
					//y = sin(i.worldOffsetScaled.x*3.14);
					//ylast = sin(i.worldOffsetScaled.z*3.14);

					// treat as an acceleration - dt^2
					return _Amplitude * _MyDeltaTime * _MyDeltaTime * float4( y, ylast, 0., 0.);
				}

				ENDCG
			}
		}
	}
}
