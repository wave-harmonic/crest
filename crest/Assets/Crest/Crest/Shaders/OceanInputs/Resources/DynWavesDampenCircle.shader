// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Dynamic Waves/Dampen Circle"
{
	Properties
	{
		_Radius("Radius", float) = 3
		_DampenStrength("Dampen Strength", Range(0, 1)) = 0.01
	}

	SubShader
	{
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			CBUFFER_START(CrestPerOceanInput)
			float _Radius;
			float _DampenStrength;
			float3 _DisplacementAtInputPosition;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 worldOffsetScaled : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);

				float3 worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				float3 centerPos = unity_ObjectToWorld._m03_m13_m23;
				o.worldOffsetScaled.xy = worldPos.xz - centerPos.xz;

				// shape is symmetric around center with known radius - fix the vert positions to perfectly wrap the shape.
				o.worldOffsetScaled.xy = sign(o.worldOffsetScaled.xy);
				float4 newWorldPos = float4(centerPos, 1.0);
				newWorldPos.xz += o.worldOffsetScaled.xy * _Radius;

				// Correct for displacement
				newWorldPos.xz -= _DisplacementAtInputPosition.xz;

				o.positionCS = mul(UNITY_MATRIX_VP, newWorldPos);

				return o;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				// power 4 smoothstep - no normalize needed
				// credit goes to stubbe's shadertoy: https://www.shadertoy.com/view/4ldSD2
				float r2 = dot(input.worldOffsetScaled.xy, input.worldOffsetScaled.xy);
				if (r2 > 1.0) return (float4)0.0;
				r2 = 1.0 - r2;
				float val = r2 * r2;

				float weight = val * _DampenStrength;
				return float4(0.0, 0.0, 0.0, weight);
			}
			ENDCG
		}
	}
}
