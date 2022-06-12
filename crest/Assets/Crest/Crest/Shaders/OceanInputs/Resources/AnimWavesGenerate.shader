// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Adds waves to world - takes wave buffer as input, makes final waves as output.

Shader "Hidden/Crest/Inputs/Animated Waves/Generate Waves"
{
	SubShader
	{
		Blend [_BlendSrcMode] [_BlendDstMode]
		ZWrite Off
		ZTest Always
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			//#pragma enable_d3d11_debug_symbols

			// Use multi_compile because these keywords are copied over from the ocean material. With shader_feature,
			// the keywords would be stripped from builds. Unused shader variants are stripped using a build processor.
			#pragma multi_compile_local _PAINTED_ON

			#include "UnityCG.cginc"

			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanHelpers.hlsl"
			#include "../../FullScreenTriangle.hlsl"

			Texture2DArray _WaveBuffer;
			Texture2D _PaintedData;

			CBUFFER_START(CrestPerOceanInput)
			int _WaveBufferSliceIndex;
			float _Weight;
			float _AverageWavelength;
			float _AttenuationInShallows;
			float2 _AxisX;
			float _RespectShallowWaterAttenuation;
			half _MaximumAttenuationDepth;
			CBUFFER_END

#if _PAINTED_ON
			CBUFFER_START(CrestPerMaterial)
			float2 _PaintedDataSize;
			float2 _PaintedDataPosition;
			CBUFFER_END
#endif

			struct Attributes
			{
				uint VertexID : SV_VertexID;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 uv_uvWaves : TEXCOORD0;
#if _PAINTED_ON
				float2 worldPosXZ : TEXCOORD1;
				float2 worldPosScaled : TEXCOORD2;
#endif
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = GetFullScreenTriangleVertexPosition(input.VertexID);

				o.uv_uvWaves.xy = GetFullScreenTriangleTexCoord(input.VertexID);

				const float2 worldPosXZ = UVToWorld( o.uv_uvWaves.xy, _LD_SliceIndex, _CrestCascadeData[_LD_SliceIndex] );

				const float waveBufferSize = 0.5f * (1 << _WaveBufferSliceIndex);

#if _PAINTED_ON
				o.worldPosXZ = worldPosXZ;
				o.worldPosScaled = worldPosXZ / waveBufferSize;
#endif

				// UV coordinate into wave buffer
				float2 wavePos = float2( dot(worldPosXZ, _AxisX), dot(worldPosXZ, float2(-_AxisX.y, _AxisX.x)) );
				o.uv_uvWaves.zw = wavePos / waveBufferSize;

				return o;
			}

			half4 Frag( Varyings input ) : SV_Target
			{
				float wt = _Weight;

				// Attenuate if depth is less than half of the average wavelength
				const half2 terrainHeight_seaLevelOffset =
					_LD_TexArray_SeaFloorDepth.SampleLevel(LODData_linear_clamp_sampler, float3(input.uv_uvWaves.xy, _LD_SliceIndex), 0.0).xy;
				const half depth = _OceanCenterPosWorld.y - terrainHeight_seaLevelOffset.x + terrainHeight_seaLevelOffset.y;
				half depth_wt = saturate(2.0 * depth / _AverageWavelength);
				if (_MaximumAttenuationDepth < CREST_OCEAN_DEPTH_BASELINE)
				{
					depth_wt = lerp(depth_wt, 1.0, saturate(depth / _MaximumAttenuationDepth));
				}
				const float attenuationAmount = _AttenuationInShallows * _RespectShallowWaterAttenuation;
				wt *= attenuationAmount * depth_wt + (1.0 - attenuationAmount);

				float3 disp = 0.0;

#if _PAINTED_ON
				if (all(_PaintedDataSize > 0.0))
				{
					float2 paintUV = (input.worldPosXZ - _PaintedDataPosition) / _PaintedDataSize + 0.5;
					// Check if in bounds
					if (all(saturate(paintUV) == paintUV))
					{
						float2 axis = _PaintedData.Sample(LODData_linear_clamp_sampler, paintUV).xy;
						float axisLen2 = dot(axis, axis);
						if (axisLen2 > 0.00001)
						{
							// Quantize wave direction and interpolate waves
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

							const float2 uv0 = float2(dot(input.worldPosScaled.xy, axisX0), dot(input.worldPosScaled.xy, axisZ0));
							const float2 uv1 = float2(dot(input.worldPosScaled.xy, axisX1), dot(input.worldPosScaled.xy, axisZ1));

							// Sample displacement, rotate into frame
							float3 disp0 = _WaveBuffer.SampleLevel(sampler_Crest_linear_repeat, float3(uv0, _WaveBufferSliceIndex), 0).xyz;
							float3 disp1 = _WaveBuffer.SampleLevel(sampler_Crest_linear_repeat, float3(uv1, _WaveBufferSliceIndex), 0).xyz;

							disp = lerp(disp0, disp1, rem / dTheta);
							disp.xz = disp.x * axis + disp.z * float2(-axis.y, axis.x);
							disp.y *= sqrt(axisLen2);
						}
					}
				}
				else
#endif
				{
					// Sample displacement, rotate into frame defined by global wind direction
					disp = _WaveBuffer.SampleLevel(sampler_Crest_linear_repeat, float3(input.uv_uvWaves.zw, _WaveBufferSliceIndex), 0).xyz;
					disp.xz = disp.x * _AxisX + disp.z * float2(-_AxisX.y, _AxisX.x);
				}

				return float4(disp, wt);
			}
			ENDCG
		}
	}
}
