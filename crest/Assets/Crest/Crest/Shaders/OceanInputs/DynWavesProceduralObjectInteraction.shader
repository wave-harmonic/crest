// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Dynamic Waves/Procedural Object Interaction"
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

				float forceUpDown = _Velocity.y;
				if (signedDist > 0.0)
				{
					forceUpDown *= -exp(-signedDist * signedDist);
				}

				float forceHoriz = -0.75 * dot(sdfNormal, _Velocity.xz);
				if (signedDist > 0.0)
				{
					forceHoriz *= -exp(-signedDist * signedDist);
				}

				return _Weight * half4(0., (forceUpDown + forceHoriz) * _SimDeltaTime * _Strength, 0., 0.);
			}
			ENDCG
		}
	}
}
