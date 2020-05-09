Shader "Crest/Examples/SubmarineWindow"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags { "Queue" = "Geometry+511" "RenderType"="Transparent" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:blend

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input
		{
			float2 uv_MainTex;
			float4 screenPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		float4 _CrestHorizonPosNormal;
		sampler2D _CrestOceanMaskTexture;
		sampler2D _CrestOceanMaskDepthTexture;

		// TODO(TRC):Now use ocean constants values directly
		// @volatie:UnderwaterMaskValues These MUST match the values in UnderwaterPostProcessUtils.cs
		// Background
		#define UNDERWATER_MASK_NO_MASK 1.0
		// Water rendered from above
		#define UNDERWATER_MASK_WATER_SURFACE_ABOVE 0.0
		// Water rendered from below
		#define UNDERWATER_MASK_WATER_SURFACE_BELOW 2.0


		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			// Albedo comes from a texture tinted by color
			//fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			fixed4 c = fixed4(.3, .25, .2, .3);
			float2 uvScreenSpace = IN.screenPos.xy / IN.screenPos.w;

			// TODO(TRC):Now, break this all out into a helpfer function that will
			// also compute fog
			float oceanMask = tex2D(_CrestOceanMaskTexture, uvScreenSpace).x;
			float z = IN.screenPos.z / IN.screenPos.w;
			const float oceanDepth01 = tex2D(_CrestOceanMaskDepthTexture, uvScreenSpace);
			const bool isBelowHorizon = dot(uvScreenSpace - _CrestHorizonPosNormal.xy, _CrestHorizonPosNormal.zw) > 0.0;
			bool isUnderwater = oceanMask == UNDERWATER_MASK_WATER_SURFACE_BELOW || (isBelowHorizon && oceanMask != UNDERWATER_MASK_WATER_SURFACE_ABOVE);
			if(isUnderwater && oceanDepth01 < z)
			{
				c.a = 0.8;
				c.r = 1.0;
			}

			// TODO(TRC):Now compute fog
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
