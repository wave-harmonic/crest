// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Animated Waves/Gerstner Geometry"
{
    Properties
    {
		_FeatherWidth("Feather width", Range(0.001, 0.5)) = 0.1
		_UseShallowWaterAttenuation("Use Shallow Water Attenuation", Range(0, 1)) = 1
	}

    SubShader
    {
		// Additive blend everywhere
		Blend One One
		ZWrite Off
		ZTest Always
		Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanHelpersNew.hlsl"

			struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
				float4 vertex : SV_POSITION;
				float4 uvGeo_uvWaves : TEXCOORD0;
				float3 uv_slice : TEXCOORD1;
            };

			Texture2DArray _WaveBuffer;

			CBUFFER_START(GerstnerPerMaterial)
			half _FeatherWidth;
			float _UseShallowWaterAttenuation;
			CBUFFER_END

			CBUFFER_START(CrestPerOceanInput)
			int _WaveBufferSliceIndex;
			float _AverageWavelength;
			float _AttenuationInShallows;
			float _Weight;
			CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;

				// We take direction of vert, not its position
				float3 positionOS = v.vertex.xyz;

				// Scale by inner/outer edge distance to create ring
				//positionOS *= lerp(_RadiusOuter, _RadiusInner, v.uv.y);

				o.vertex = UnityObjectToClipPos(positionOS);
				o.uvGeo_uvWaves.xy = v.uv;

				//float aveCircum = 3.1415927 * (_RadiusInner + _RadiusOuter);
				const float waveBufferSize = 0.5f * (1 << _WaveBufferSliceIndex);
				// Make wave buffer repeat an integral number of times around the circumference
				//aveCircum = max(1.0, round(aveCircum / waveBufferSize)) * waveBufferSize;

				// UV coordinate into wave buffer
				const float2 wavePosition = v.uv; // v.uv* float2(aveCircum, _RadiusOuter - _RadiusInner);
				o.uvGeo_uvWaves.zw = wavePosition.yx / waveBufferSize;

				// UV coordinate into the cascade we are rendering into
				const float3 worldPos = mul(unity_ObjectToWorld, float4(positionOS, 1.0)).xyz;
				o.uv_slice.xyz = WorldToUV(worldPos.xz, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);

                return o;
            }

            float4 frag(v2f input) : SV_Target
            {
				float wt = _Weight;

				// Attenuate if depth is less than half of the average wavelength
				const half depth = _LD_TexArray_SeaFloorDepth.SampleLevel(LODData_linear_clamp_sampler, input.uv_slice.xyz, 0.0).x;
				half depth_wt = saturate(2.0 * depth / _AverageWavelength);
				const float attenuationAmount = _AttenuationInShallows * _UseShallowWaterAttenuation;
				wt *= attenuationAmount * depth_wt + (1.0 - attenuationAmount);

				// Feature at front/back
				//float r_l1 = abs(input.uvGeo_uvWaves.y - 0.5);
				//wt *= saturate(1.0 - (r_l1 - (0.5 - _FeatherWidth)) / _FeatherWidth);

				// Sample displacement, rotate into frame
				float4 disp_variance = _WaveBuffer.SampleLevel(sampler_Crest_linear_repeat, float3(input.uvGeo_uvWaves.zw, _WaveBufferSliceIndex), 0);
				//disp_variance.xz = disp_variance.x * input.axisX + disp_variance.z * float2(-input.axisX.y, input.axisX.x);

				// The large waves are added to the last two lods. Don't write cumulative variances for these - cumulative variance
				// for the last fitting wave cascade captures everything needed.
				const float minWavelength = _AverageWavelength / 1.5;
				if( minWavelength > _CrestCascadeData[_LD_SliceIndex]._maxWavelength )
				{
					disp_variance.w = 0.0;
				}

				return wt * disp_variance;
            }
            ENDCG
        }
    }
}
