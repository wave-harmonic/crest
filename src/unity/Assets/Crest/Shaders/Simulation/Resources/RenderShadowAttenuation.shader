﻿
Shader "Ocean/Shape/Sim/Render Shadow Attenuation"
{
	Properties
	{
	}

	SubShader
	{
		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			// compile shader into multiple variants, with and without shadows
			// (we don't care about any lightmaps yet, so skip these variants)
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			// shadow helper functions and macros
			#include "AutoLight.cginc"
	
			struct v2f
			{
				float4 pos : SV_POSITION;
				SHADOW_COORDS(0) // put shadows data into TEXCOORD0
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				TRANSFER_SHADOW(o)
				return o;
			}

			fixed frag(v2f i) : SV_Target
			{
				return SHADOW_ATTENUATION(i);
			}
			ENDCG
		}

		// shadow casting support
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
