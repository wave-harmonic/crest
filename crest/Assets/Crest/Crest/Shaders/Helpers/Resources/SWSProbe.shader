Shader "Crest/Debugging/SWSProbe"
{
    Properties
    {
		[Toggle] _FinalHeight("Final Height", Float) = 1
		[Toggle] _GroundHeight("Ground Height", Float) = 0
		[Toggle] _Animate("Animate", Float) = 1
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

			#pragma shader_feature_local _FINALHEIGHT_ON
			#pragma shader_feature_local _GROUNDHEIGHT_ON
			#pragma shader_feature_local _ANIMATE_ON

            #include "UnityCG.cginc"

			#include "../../OceanGlobals.hlsl"

			half _DomainWidth;
			float3 _SimOrigin;

			Texture2D<float> _swsHRender;
			Texture2D<float> _swsGroundHeight;

            struct appdata
            {
				float3 positionOS : POSITION;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
			};

			float CalculatePositionY(float2 worldXZ)
			{
				float2 uv = (worldXZ - _SimOrigin.xz) / _DomainWidth + 0.5;

				float h = _swsHRender.SampleLevel(LODData_linear_clamp_sampler, uv, 0.0).x;
				float g = _swsGroundHeight.SampleLevel(LODData_linear_clamp_sampler, uv, 0.0).x;

				float y;
#if _FINALHEIGHT_ON
				y = h + g + _SimOrigin.y;
#elif _GROUNDHEIGHT_ON
				y = g + _SimOrigin.y;
#endif

#if _ANIMATE_ON
				y += sin(worldXZ.x - 5.0 * _Time.w) * 0.02;
#endif
				return y;
			}

            v2f vert (appdata v)
            {
                v2f o;

				o.positionCS = UnityObjectToClipPos(float4(v.positionOS, 1.0));
				float3 positionWS = mul(UNITY_MATRIX_M, float4(v.positionOS, 1.0)).xyz;

				float y = CalculatePositionY(positionWS.xz);
				positionWS.y = y;

				o.positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return float4(1.0, 0.5, 0.5, 1.0);
            }
            ENDCG
        }
    }
}
