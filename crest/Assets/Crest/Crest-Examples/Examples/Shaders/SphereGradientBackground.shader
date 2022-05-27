// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/SphereGradientBackground"
{
	Properties
	{
		_ColorTowardsSun("_ColorTowardsSun", Color) = (1, 1, 1)
		_ColorAwayFromSun("_ColorAwayFromSun", Color) = (1, 1, 1)
		_Exponent("_Exponent", Float) = 1.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }
		LOD 100

		Pass
		{
			Cull Front
			Blend Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			#include "../../../Crest/Shaders/Helpers/BIRP/Core.hlsl"

			struct Attributes
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 vertex : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes v)
			{
				Varyings o;
				ZERO_INITIALIZE(Varyings, o);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.positionWS = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));
				return o;
			}

			float3 _ColorTowardsSun;
			float3 _ColorAwayFromSun;
			float _Exponent;

			float4 frag (Varyings i) : SV_Target
			{
				float3 worldPosition = i.positionWS;
				float3 viewDirection = normalize(i.positionWS - _WorldSpaceCameraPos);

				float alpha = saturate(0.5 * dot(viewDirection, _WorldSpaceLightPos0.xyz) + 0.5);
				alpha = pow(alpha, _Exponent);

				float3 col = lerp(_ColorAwayFromSun, _ColorTowardsSun, alpha);
				return float4(col * saturate(max(_LightColor0.r, max(_LightColor0.g, _LightColor0.b))), 1.0);
			}
			ENDCG
		}
	}
}
