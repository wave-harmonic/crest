// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Examples/UnderwaterTransparentSurfaceShader"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_FogMultiplier ("Fog Multiplier", Range(0, 1)) = 1.0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
		LOD 200

		CGPROGRAM
		// Fog is added to the final color so add "finalcolor:FinalColor"
		#pragma surface Surface Standard vertex:Vertex finalcolor:FinalColor alpha:blend
		#pragma target 3.0

		// Since we are setting finalcolor:FinalColor, Unity fog will no longer be applied. Only add this if
		// you use Unity's fog.
		#pragma multi_compile_fog

		// Keywords for optional features. Can skip if not needed.
		#pragma multi_compile _ CREST_WATER_VOLUME_2D CREST_WATER_VOLUME_HAS_BACKFACE
		#pragma multi_compile _ CREST_SUBSURFACESCATTERING_ON
		#pragma multi_compile _ CREST_SHADOWS_ON

		#include "UnityCG.cginc"

		// Include Crest shader functions etc.
		#include "../../../Crest/Shaders/Underwater/UnderwaterEffectIncludes.hlsl"

		struct Input
		{
			float3 worldPos;
			float4 screenPos;
			float1 fogCoord;
		};

		fixed4 _Color;
		half _Glossiness;
		half _Metallic;
		half _FogMultiplier;

		void Vertex(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			UNITY_TRANSFER_FOG(o, UnityObjectToClipPos(v.vertex));
		}

		// Fog should be applied to the final color.
		void FinalColor(Input IN, SurfaceOutputStandard o, inout fixed4 color)
		{
			float2 positionNDC = IN.screenPos.xy / IN.screenPos.w;
			float deviceDepth = IN.screenPos.z / IN.screenPos.w;

			// "true" is returned if pixel is underwater and fog was applied. If "false" then we should apply Unity fog.
			if (!CrestApplyUnderwaterFog(positionNDC, IN.worldPos, deviceDepth, _FogMultiplier, color.rgb))
			{
				UNITY_APPLY_FOG(IN.fogCoord, color);
			}
		}

		void Surface(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = _Color.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = _Color.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
