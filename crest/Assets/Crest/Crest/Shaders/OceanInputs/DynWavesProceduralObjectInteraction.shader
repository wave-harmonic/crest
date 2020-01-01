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
			
			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float force : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				float3 vertexWorldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
				o.positionCS = mul(UNITY_MATRIX_VP, float4(vertexWorldPos, 1.0));

				float2 posXZ = input.positionOS.xy;
				o.force = length(posXZ) < 0.5 ? 1.0 : 0.0;

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return _Weight * half4(0., input.force*_SimDeltaTime, 0., 0.);
			}
			ENDCG
		}
	}
}
