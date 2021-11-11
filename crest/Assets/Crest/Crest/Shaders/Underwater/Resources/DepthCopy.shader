Shader "Hidden/Crest/Helpers/DepthCopy"
{
	SubShader
	{
		Cull Off ZWrite On ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment

			#include "UnityCG.cginc"

			#include "../../Helpers/BIRP/Core.hlsl"
			#include "../../Helpers/BIRP/InputsDriven.hlsl"

			#include "../../OceanShaderHelpers.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f Vertex(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			TEXTURE2D_X(_CameraDepthTexture);
			// sampler2D _CrestOceanMaskDepthTexture;

			// important part: outputs depth from _MyDepthTex to depth buffer
			half4 Fragment(v2f i, out float outDepth : SV_Depth) : SV_Target
			{
				float depth = LOAD_DEPTH_TEXTURE_X(_CameraDepthTexture, i.vertex.xy);
				// Unity bug is binding wrong texture.
				// float oceanDepth = tex2D(_CrestOceanMaskDepthTexture, i.uv).r;
				outDepth = depth;
					// depth < oceanDepth ? oceanDepth :
					// depth;
				return 1;
			}

			ENDCG
		}
	}
}
