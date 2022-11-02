// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Takes waves from the wave buffer and places them into the world determined by an input texture.

Shader "Crest/Inputs/Shape Waves/Sample Spectrum"
{
	Properties
	{
		_MainTex("Texture", 2D) = "black" {}
		// Whether shallow water attenuation is applied to the blend weight. A minor optimization but can change the
		// outcome. For example if your waves are attenuated fully it could flatten existing waves. Requires alpha
		// blending.
		[Toggle] _AttenuationAffectsBlendWeight("Attenuation Affects Blend Weight", Float) = 0
		// Whether not to normalize the texture value when applying to blend. This can allow existing waves to blend in
		// with the new waves. Requires alpha blending.
		[Toggle] _SmoothBlend("Smooth Blend Weight", Float) = 0
	}

	SubShader
	{
		ZWrite Off
		ZTest Always
		Cull Off

		CGINCLUDE
		#include "UnityCG.cginc"

		#include "../OceanGlobals.hlsl"
		#include "../OceanInputsDriven.hlsl"
		#include "../OceanHelpersNew.hlsl"

		CBUFFER_START(CrestPerOceanInput)
		int _WaveBufferSliceIndex;
		float _Weight;
		float _AverageWavelength;
		float _AttenuationInShallows;
		float2 _AxisX;
		float _RespectShallowWaterAttenuation;
		half _MaximumAttenuationDepth;
		CBUFFER_END

		half GetAttenuationInShallowsWeight(const Texture2DArray i_texture, const float3 i_uv)
		{
			const half2 terrainHeight_seaLevelOffset = i_texture.SampleLevel(LODData_linear_clamp_sampler, i_uv, 0.0).xy;
			const half depth = _OceanCenterPosWorld.y - terrainHeight_seaLevelOffset.x + terrainHeight_seaLevelOffset.y;
			// Attenuate if depth is less than half of the average wavelength.
			half weight = saturate(2.0 * depth / _AverageWavelength);
			if (_MaximumAttenuationDepth < CREST_OCEAN_DEPTH_BASELINE)
			{
				weight = lerp(weight, 1.0, saturate(depth / _MaximumAttenuationDepth));
			}
			const float attenuationAmount = _AttenuationInShallows * _RespectShallowWaterAttenuation;
			return attenuationAmount * weight + (1.0 - attenuationAmount);
		}
		ENDCG

		Pass
		{
			Blend One One

			CGPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment

			// #pragma enable_d3d11_debug_symbols

			#pragma shader_feature_local _ATTENUATIONAFFECTSBLENDWEIGHT_ON

			Texture2DArray _WaveBuffer;
			Texture2D _MainTex;

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 uv_uvLodData : TEXCOORD0;
				float4 worldPosScaled_axis : TEXCOORD1;
			};

			Varyings Vertex(Attributes input)
			{
				Varyings output;
				output.positionCS = UnityObjectToClipPos(input.positionOS);

				output.uv_uvLodData.xy = input.uv;

				float2 worldPosXZ = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xz;
				output.uv_uvLodData.zw = WorldToUV(worldPosXZ, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);

				// World position prescaled by wave buffer size. Suitable for using as UVs in fragment shader.
				const float waveBufferSize = 0.5f * (1 << _WaveBufferSliceIndex);
				output.worldPosScaled_axis.xy = worldPosXZ / waveBufferSize;

				// Object and wind axis.
				float2 rotation = normalize(unity_ObjectToWorld._m00_m20.xy);
				output.worldPosScaled_axis.zw = rotation.x * _AxisX + rotation.y * float2(-_AxisX.y, _AxisX.x);

				return output;
			}

			half4 Fragment(Varyings input) : SV_Target
			{
				float4 displacement = 0.0;
				float weight = _Weight;

#if _ATTENUATIONAFFECTSBLENDWEIGHT_ON
				weight *= GetAttenuationInShallowsWeight(_LD_TexArray_SeaFloorDepth, float3(input.uv_uvLodData.zw, _LD_SliceIndex));
#endif

				float featherBoundaries = max(abs(input.uv_uvLodData.x - 0.5), abs(input.uv_uvLodData.y - 0.5));
				weight *= smoothstep(0.5, 0.4, featherBoundaries);

				if (weight > 0.0)
				{
					// -1.0 to 1.0
					float2 axis = _MainTex.Sample(LODData_linear_clamp_sampler, input.uv_uvLodData.xy).xy * 2.0 - 1.0;

					// Axis length squared. Saves a sqrt calculation if fails test.
					float axisLen2 = dot(axis, axis);

					if (axisLen2 > 0.0001)
					{
						// Add object and wind rotation.
						axis = axis.x * input.worldPosScaled_axis.zw + axis.y * float2(-input.worldPosScaled_axis.w, input.worldPosScaled_axis.z);

						// Quantize wave direction and interpolate waves.
						float axisHeading = atan2(axis.y, axis.x) + 2.0 * 3.141592654;
						const float dTheta = 0.5 * 0.314159265;
						float angle0 = axisHeading;
						const float rem = fmod(angle0, dTheta);
						angle0 -= rem;
						const float angle1 = angle0 + dTheta;

						float2 axisX0; sincos(angle0, axisX0.y, axisX0.x);
						float2 axisX1; sincos(angle1, axisX1.y, axisX1.x);
						float2 axisZ0; axisZ0.x = -axisX0.y; axisZ0.y = axisX0.x;
						float2 axisZ1; axisZ1.x = -axisX1.y; axisZ1.y = axisX1.x;

						const float2 uv0 = float2(dot(input.worldPosScaled_axis.xy, axisX0), dot(input.worldPosScaled_axis.xy, axisZ0));
						const float2 uv1 = float2(dot(input.worldPosScaled_axis.xy, axisX1), dot(input.worldPosScaled_axis.xy, axisZ1));

						// Sample displacement, rotate into frame.
						float4 displacement0 = _WaveBuffer.SampleLevel(sampler_Crest_linear_repeat, float3(uv0, _WaveBufferSliceIndex), 0);
						float4 displacement1 = _WaveBuffer.SampleLevel(sampler_Crest_linear_repeat, float3(uv1, _WaveBufferSliceIndex), 0);

						displacement = lerp(displacement0, displacement1, rem / dTheta);
						displacement.xz = displacement.x * axis + displacement.z * float2(-axis.y, axis.x);
						displacement.y *= sqrt(axisLen2);

						// The large waves are added to the last two lods. Don't write cumulative variances for these -
						// cumulative variance for the last fitting wave cascade captures everything needed.
						const float minWavelength = _AverageWavelength / 1.5;
						if (minWavelength > _CrestCascadeData[_LD_SliceIndex]._maxWavelength)
						{
							displacement.w = 0.0;
						}
					}

#if !_ATTENUATIONAFFECTSBLENDWEIGHT_ON
					// Attenuate the displacement.
					displacement *= GetAttenuationInShallowsWeight(_LD_TexArray_SeaFloorDepth, float3(input.uv_uvLodData.zw, _LD_SliceIndex));
#endif
				}

				return displacement * weight;
			}
			ENDCG
		}

		Pass
		{
			// Multiply
			Blend Zero SrcColor

			CGPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment

			// #pragma enable_d3d11_debug_symbols

			#pragma shader_feature_local _ATTENUATIONAFFECTSBLENDWEIGHT_ON
			#pragma shader_feature_local _SMOOTHBLEND_ON

			Texture2D _MainTex;

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
#if _ATTENUATIONAFFECTSBLENDWEIGHT_ON
				float2 uvLodData : TEXCOORD1;
#endif
			};

			Varyings Vertex(Attributes input)
			{
				Varyings output;
				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.uv = input.uv;
				float2 worldPosXZ = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xz;

#if _ATTENUATIONAFFECTSBLENDWEIGHT_ON
				output.uvLodData = WorldToUV(worldPosXZ, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);
#endif
				return output;
			}

			half4 Fragment(Varyings input) : SV_Target
			{
				float weight = _Weight;

				float featherBoundaries = max(abs(input.uv.x - 0.5), abs(input.uv.y - 0.5));
				weight *= smoothstep(0.5, 0.4, featherBoundaries);

				if (weight > 0.0)
				{
					float2 axis = _MainTex.Sample(LODData_linear_clamp_sampler, input.uv).xy * 2.0 - 1.0;

#if !_SMOOTHBLEND_ON
					if (dot(axis, axis) > 0.0001)
					{
						axis = normalize(axis);
					}
#endif

					weight *= length(axis);

#if _ATTENUATIONAFFECTSBLENDWEIGHT_ON
					weight *= GetAttenuationInShallowsWeight(_LD_TexArray_SeaFloorDepth, float3(input.uvLodData, _LD_SliceIndex));
#endif
				}

				return 1.0 - weight;
			}
			ENDCG
		}
	}
}
