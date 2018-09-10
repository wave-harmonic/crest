Shader "Unlit/ShadowUpdate"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			
			// from https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/42ce8fe3bab5aa12d8fcbad4d90f8bf3d7f5fad3/com.unity.render-pipelines.lightweight/LWRP/ShaderLibrary/Shadows.hlsl
			#define MAX_SHADOW_CASCADES 4

			UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);


			// copy pasted like a pro from https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/42ce8fe3bab5aa12d8fcbad4d90f8bf3d7f5fad3/com.unity.render-pipelines.lightweight/LWRP/ShaderLibrary/Shadows.hlsl

			CBUFFER_START(_DirectionalShadowBuffer)
			// Last cascade is initialized with a no-op matrix. It always transforms
			// shadow coord to half(0, 0, NEAR_PLANE). We use this trick to avoid
			// branching since ComputeCascadeIndex can return cascade index = MAX_SHADOW_CASCADES
			float4x4    _WorldToShadow[MAX_SHADOW_CASCADES + 1];
			float4      _DirShadowSplitSpheres0;
			float4      _DirShadowSplitSpheres1;
			float4      _DirShadowSplitSpheres2;
			float4      _DirShadowSplitSpheres3;
			float4      _DirShadowSplitSphereRadii;
			half4       _ShadowOffset0;
			half4       _ShadowOffset1;
			half4       _ShadowOffset2;
			half4       _ShadowOffset3;
			half4       _ShadowData;    // (x: shadowStrength)
			float4      _ShadowmapSize; // (xy: 1/width and 1/height, zw: width and height)
			CBUFFER_END

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				//TRANSFER_SHADOW(o);
				//o._ShadowCoord = mul(unity_WorldToShadow[3], float4(o.worldPos, 1.));// mul(unity_ObjectToWorld, v.vertex));
				return o;
			}

			inline fixed unitySampleShadow(unityShadowCoord4 shadowCoord)
			{
#if defined(SHADOWS_NATIVE)
				fixed shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, shadowCoord.xyz);
				shadow = _LightShadowData.r + shadow * (1 - _LightShadowData.r);
				return shadow;
#else
				unityShadowCoord dist = SAMPLE_DEPTH_TEXTURE(_ShadowMapTexture, shadowCoord.xy);
				// tegra is confused if we use _LightShadowData.x directly
				// with "ambiguous overloaded function reference max(mediump float, float)"
				unityShadowCoord lightShadowDataX = _LightShadowData.x;
				unityShadowCoord threshold = shadowCoord.z;
				return max(dist > threshold, lightShadowDataX);
#endif
			}

			fixed frag (v2f i) : SV_Target
			{
				fixed shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, float3(i.uv, 0.5));

				return shadow;
			}
			ENDCG
		}
	}
}
