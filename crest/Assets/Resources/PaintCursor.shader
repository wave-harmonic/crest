// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Draws a 3D cursor in the world

// TODO can this be editor only?

Shader "Crest/PaintCursor"
{
	Properties
	{
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		LOD 100

		Blend One One
		ZWrite Off

		// In front of scene surfaces / visible
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "../Crest/Crest/Shaders/OceanGlobals.hlsl"
			#include "../Crest/Crest/Shaders/OceanInputsDriven.hlsl"
			#include "../Crest/Crest/Shaders/OceanHelpers.hlsl"
			#include "PaintCursorHelper.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 positionWS : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.positionWS = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0)).xyz;

				float3 centerPosWorld = mul(UNITY_MATRIX_M, float4(0.0, 0.0, 0.0, 1.0)).xyz;
				float3 centerPosProjected = ProjectToSurface(centerPosWorld.xz);
				float3 projectionOffset = centerPosProjected - centerPosWorld;

				// Offset vertex position
				o.positionWS += projectionOffset;
				o.vertex = mul(UNITY_MATRIX_VP, float4(o.positionWS, 1.0));

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = 0.1;
				return col;
			}
			ENDCG
		}

		// Behind scene surfaces / occluded
		Pass
		{
			ZTest Greater

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "../Crest/Crest/Shaders/OceanGlobals.hlsl"
			#include "../Crest/Crest/Shaders/OceanInputsDriven.hlsl"
			#include "../Crest/Crest/Shaders/OceanHelpers.hlsl"
			#include "PaintCursorHelper.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 positionWS : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.positionWS = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0)).xyz;

				float3 centerPosWorld = mul(UNITY_MATRIX_M, float4(0.0, 0.0, 0.0, 1.0)).xyz;
				float3 centerPosProjected = ProjectToSurface(centerPosWorld.xz);
				float3 projectionOffset = centerPosProjected - centerPosWorld;

				// Offset vertex position
				o.positionWS += projectionOffset;
				o.vertex = mul(UNITY_MATRIX_VP, float4(o.positionWS, 1.0));

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// Checkerboard because it looks fancy
				float alpha = frac(i.positionWS.x) < 0.5;
				if (frac(i.positionWS.z) < 0.5) alpha = 1.0 - alpha;

				fixed4 col = lerp(0.04, 0.06, alpha);
				return col;
			}
			ENDCG
		}
	}
}
