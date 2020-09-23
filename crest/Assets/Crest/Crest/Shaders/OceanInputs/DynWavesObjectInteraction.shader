// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Dynamic Waves/Object Interaction"
{
	Properties
	{
		_FactorParallel("FactorParallel", Range(0., 8.)) = 0.2
		_FactorOrthogonal("FactorOrthogonal", Range(0., 4.)) = 0.2
		_Strength("Strength", Range(0., 1000.)) = 0.2
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" }

		Pass
		{
			Blend One One
			ZTest Always
			ZWrite Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			CBUFFER_START(CrestPerOceanInput)
			float _FactorParallel;
			float _FactorOrthogonal;
			float3 _Velocity;
			float _SimDeltaTime;
			float _Strength;
			float _Weight;
			float3 _DisplacementAtInputPosition;
			CBUFFER_END
			
			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 normal : NORMAL;
				float4 col : COLOR;
				float offsetDist : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				float3 vertexWorldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
				// Correct for displacement
				vertexWorldPos.xz -= _DisplacementAtInputPosition.xz;

				o.normal = normalize(mul(unity_ObjectToWorld, float4(input.normal, 0.)).xyz);

				float3 vel = _Velocity /= 30.;

				float velMag = max(length(vel), 0.001);
				float3 velN = vel / velMag;
				float angleFactor = dot(velN, o.normal);

				if (angleFactor < 0.)
				{
					// this helps for when velocity exactly perpendicular to some faces
					if (angleFactor < -0.0001)
					{
						vel = -vel;
					}

					angleFactor *= -1.;
				}

				float3 offset = o.normal * _FactorOrthogonal * pow(saturate(1. - angleFactor), .2) * velMag;
				offset += vel * _FactorParallel * pow(angleFactor, .5);
				o.offsetDist = length(offset);
				vertexWorldPos += offset;

				o.positionCS = mul(UNITY_MATRIX_VP, float4(vertexWorldPos, 1.));

				o.col = 1.0;

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				half4 col = (half4)0.;
				col.x = _Strength * (length(input.offsetDist)) * abs(input.normal.y) * sqrt(length(_Velocity)) / 10.;

				if (dot(input.normal, _Velocity) < -0.1)
				{
					col.x *= -.5;
				}

				// Accelerated velocities
				return _Weight * half4(0., col.x*_SimDeltaTime, 0., 0.);
			}
			ENDCG
		}
	}
}
