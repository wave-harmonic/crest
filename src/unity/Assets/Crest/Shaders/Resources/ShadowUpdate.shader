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

			#define SHADOW_COLLECTOR_PASS
			#include "UnityCG.cginc"

#pragma enable_d3d11_debug_symbols

			//#include "UnityCG.cginc"
			//#include "AutoLight.cginc"
			
			//// from https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/42ce8fe3bab5aa12d8fcbad4d90f8bf3d7f5fad3/com.unity.render-pipelines.lightweight/LWRP/ShaderLibrary/Shadows.hlsl
			//#define MAX_SHADOW_CASCADES 4

			//UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);

			//#include "UnityShaderVariables.cginc"
	//C:\Program Files\Unity\Editor\Data\CGIncludes\UnityShaderVariables.cginc
	//CBUFFER_START(UnityShadows)
	//	float4 unity_ShadowSplitSpheres[4];
	//float4 unity_ShadowSplitSqRadii;
	//float4 unity_LightShadowBias;
	//float4 _LightSplitsNear;
	//float4 _LightSplitsFar;
	//float4x4 unity_WorldToShadow[4];
	//half4 _LightShadowData;
	//float4 unity_ShadowFadeCenterAndType;
	//CBUFFER_END


			//// copy pasted like a pro from https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/42ce8fe3bab5aa12d8fcbad4d90f8bf3d7f5fad3/com.unity.render-pipelines.lightweight/LWRP/ShaderLibrary/Shadows.hlsl
			//CBUFFER_START(_DirectionalShadowBuffer)
			//// Last cascade is initialized with a no-op matrix. It always transforms
			//// shadow coord to half(0, 0, NEAR_PLANE). We use this trick to avoid
			//// branching since ComputeCascadeIndex can return cascade index = MAX_SHADOW_CASCADES
			//float4x4    _WorldToShadow[MAX_SHADOW_CASCADES + 1];
			//float4      _DirShadowSplitSpheres0;
			//float4      _DirShadowSplitSpheres1;
			//float4      _DirShadowSplitSpheres2;
			//float4      _DirShadowSplitSpheres3;
			//float4      _DirShadowSplitSphereRadii;
			//half4       _ShadowOffset0;
			//half4       _ShadowOffset1;
			//half4       _ShadowOffset2;
			//half4       _ShadowOffset3;
			//half4       _ShadowData;    // (x: shadowStrength)
			//float4      _ShadowmapSize; // (xy: 1/width and 1/height, zw: width and height)
			//CBUFFER_END

			struct appdata
			{
				float4 vertex : POSITION;
				//float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				V2F_SHADOW_COLLECTOR;
				//float4 pos : SV_POSITION; float3 _ShadowCoord0 : TEXCOORD0; float3 _ShadowCoord1 : TEXCOORD1; float3 _ShadowCoord2 : TEXCOORD2; float3 _ShadowCoord3 : TEXCOORD3; float4 _WorldPosViewZ : TEXCOORD4

				//float2 uv : TEXCOORD0;
				//UNITY_FOG_COORDS(1)
				//float4 vertex : SV_POSITION;
			};

			//sampler2D _MainTex;
			//float4 _MainTex_ST;
			uniform float3 _CenterPos;
			uniform float3 _Scale;
			uniform float3 _CamPos;
			uniform float3 _CamForward;

			v2f vert (appdata v)
			{
				v2f o;
				//o.vertex = UnityObjectToClipPos(v.vertex);
				//o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				//TRANSFER_SHADOW(o);
				//o._ShadowCoord = mul(unity_WorldToShadow[3], float4(o.worldPos, 1.));// mul(unity_ObjectToWorld, v.vertex));

				//TRANSFER_SHADOW_COLLECTOR(o);
				o.pos = UnityObjectToClipPos(v.vertex);

				float4 wpos = float4(float3(v.vertex.x - 0.5, 0.0, v.vertex.y - 0.5) * _Scale.xzy * 4. + _CenterPos, 1.);
				wpos.y = 0.;

				//wpos = mul(unity_ObjectToWorld, v.vertex.xzyw).xyz * _Scale + _CenterPos;
				o._WorldPosViewZ.xyz = wpos.xyz;
				//o._WorldPosViewZ.w = -UnityObjectToViewPos(v.vertex).z;
				o._WorldPosViewZ.w = dot(wpos.xyz - _CamPos, _CamForward);

				o._ShadowCoord0 = mul(unity_WorldToShadow[0], wpos).xyz;
				o._ShadowCoord1 = mul(unity_WorldToShadow[1], wpos).xyz;
				o._ShadowCoord2 = mul(unity_WorldToShadow[2], wpos).xyz;
				o._ShadowCoord3 = mul(unity_WorldToShadow[3], wpos).xyz;

				return o;
			}

//			inline fixed unitySampleShadow(unityShadowCoord4 shadowCoord)
//			{
//#if defined(SHADOWS_NATIVE)
//				fixed shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, shadowCoord.xyz);
//				shadow = _LightShadowData.r + shadow * (1 - _LightShadowData.r);
//				return shadow;
//#else
//				unityShadowCoord dist = SAMPLE_DEPTH_TEXTURE(_ShadowMapTexture, shadowCoord.xy);
//				// tegra is confused if we use _LightShadowData.x directly
//				// with "ambiguous overloaded function reference max(mediump float, float)"
//				unityShadowCoord lightShadowDataX = _LightShadowData.x;
//				unityShadowCoord threshold = shadowCoord.z;
//				return max(dist > threshold, lightShadowDataX);
//#endif
//			}


			float4 ComputeShadow(v2f i)
			{
				//o._WorldPosViewZ.xyz = wpos;
				//o._WorldPosViewZ.w = -UnityObjectToViewPos(v.vertex).z;
				//o._WorldPosViewZ.w = dot(wpos - _CamPos, _CamForward);
				//float4 wpos = float4(i._WorldPosViewZ.xyz, 1.);
				//i._ShadowCoord0 = mul(unity_WorldToShadow[0], wpos).xyz;
				//i._ShadowCoord1 = mul(unity_WorldToShadow[1], wpos).xyz;
				//i._ShadowCoord2 = mul(unity_WorldToShadow[2], wpos).xyz;
				//i._ShadowCoord3 = mul(unity_WorldToShadow[3], wpos).xyz;


				SHADOW_COLLECTOR_FRAGMENT(i);
			}

			half4 frag (v2f i) : SV_Target
			{
				//fixed shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, float3(i.uv, 0.5));

				//return shadow;
				//return unity_ShadowSplitSqRadii;

				return (half4)ComputeShadow(i).xxxx;
			}
			ENDCG
		}
	}
}
