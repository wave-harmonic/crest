// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Dynamic Waves/Procedural Object Interaction"
{
	Properties
	{
		_FactorParallel("FactorParallel", Range(0., 8.)) = 0.2
		_FactorOrthogonal("FactorOrthogonal", Range(0., 4.)) = 0.2
		_Strength("Strength", Range(0., 1000.)) = 0.2
		_Velocity("Velocity", Vector) = (0,0,0,0)
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

			float _FactorParallel;
			float _FactorOrthogonal;
			float3 _Velocity;
			float _SimDeltaTime;
			float _Strength;
			float _Weight;

			float _Radius;

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

			half4 Frag(Varyings input) : SV_Target
			{
				float dist = length(input.offsetXZ);
				float signedDist = dist - _Radius;
				float2 sdfNormal = input.offsetXZ / dist;

				float force = 1.0;
				if (signedDist > 0.0)
				{
					force = -exp(-signedDist * signedDist);
				}

				force *= _Velocity.y;

				return _Weight * half4(0., force * _SimDeltaTime * _Strength, 0., 0.);
			}
			ENDCG
		}
	}
}
