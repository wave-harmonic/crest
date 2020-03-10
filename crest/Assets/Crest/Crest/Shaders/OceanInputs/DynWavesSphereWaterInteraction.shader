// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Dynamic Waves/Sphere-Water Interaction"
{
	Properties
	{
		_Strength("Strength", Range(0., 1000.)) = 0.2
	}

	SubShader
	{
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
			float3 _Velocity;
			float _SimDeltaTime;
			float _Strength;
			float _Weight;

			float _Radius;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 offsetXZ : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				const float quadExpand = 4.0;

				float3 vertexWorldPos = mul(unity_ObjectToWorld, float4(input.positionOS * quadExpand, 1.0));
				float3 centerPos = unity_ObjectToWorld._m03_m13_m23;

				o.offsetXZ = vertexWorldPos.xz - centerPos.xz;

				o.positionCS = mul(UNITY_MATRIX_VP, float4(vertexWorldPos, 1.0));

				return o;
			}

			// Signed distance field for sphere.
			void SphereSDF(float2 offsetXZ, out float signedDist, out float2 normal)
			{
				float dist = length(offsetXZ);
				signedDist = dist - _Radius;
				normal = dist > 0.0001 ? offsetXZ / dist : float2(1.0, 0.0);
			}

			half4 Frag(Varyings input) : SV_Target
			{
				// Compute signed distance to sphere (sign gives inside/outside), and outwards normal to sphere surface
				float signedDist;
				float2 sdfNormal;
				SphereSDF(input.offsetXZ, signedDist, sdfNormal);

				// Forces from up/down motion. Push in same direction as vel inside sphere, and opposite dir outside.
				float forceUpDown = _Velocity.y;
				if (signedDist > 0.0)
				{
					forceUpDown *= -exp(-signedDist * signedDist * 4.0);
				}

				// Forces from horizontal motion - push water up in direction of motion, pull down behind.
				float forceHoriz = -0.75 * dot(sdfNormal, _Velocity.xz);
				if (signedDist > 0.0)
				{
					forceHoriz *= -exp(-signedDist * signedDist);
				}

				// Add to velocity (y-channel) to accelerate water.
				return _Weight * half4(0.0, (forceUpDown + forceHoriz) * _SimDeltaTime * _Strength, 0.0, 0.0);
			}
			ENDCG
		}
	}
}
