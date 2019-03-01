// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Inputs/Animated Waves/Baked"
{
	Properties
	{
		_Amplitude("_Amplitude", float) = 1
		_Scale("_Scale", float) = 50
	}

	SubShader
	{
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			struct Attributes
			{
				float4 vertex : POSITION;
			};

			struct Varyings
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			Varyings Vert(Attributes v)
			{
				Varyings o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
				return o;
			}

			sampler2D _WaveData0, _WaveData1;
			float _WaveDataLerp;

			float _Amplitude;
			float _Scale;

			float3 Frag(Varyings input) : SV_Target
			{
				float3 waveData = lerp(tex2D(_WaveData0, input.worldPos.xz / _Scale).xyz, tex2D(_WaveData1, input.worldPos.xz / _Scale).xyz, _WaveDataLerp);

				return _Amplitude * waveData;
			}

			ENDCG
		}
	}
}
