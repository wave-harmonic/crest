// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Adds clipping from the Painting feature

Shader "Hidden/Crest/Inputs/Clip Surface/Painted"
{
	SubShader
	{
		// Painted clip defines a minimum value of the clip
		BlendOp Max
		ZWrite Off
		ZTest Always
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			//#pragma enable_d3d11_debug_symbols

			#include "UnityCG.cginc"

			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanHelpersNew.hlsl"
			#include "../../FullScreenTriangle.hlsl"

			Texture2DArray _WaveBuffer;
			Texture2D _PaintedWavesData;

			CBUFFER_START(CrestPerOceanInput)
			int _WaveBufferSliceIndex;
			float _Weight;
			float _AverageWavelength;
			CBUFFER_END

			CBUFFER_START(CrestPerMaterial)
			float2 _PaintedWavesSize;
			float2 _PaintedWavesPosition;
			CBUFFER_END

			struct Attributes
			{
				uint VertexID : SV_VertexID;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 worldPosXZ : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = GetFullScreenTriangleVertexPosition(input.VertexID);

				const float2 worldPosXZ = UVToWorld(GetFullScreenTriangleTexCoord(input.VertexID), _LD_SliceIndex, _CrestCascadeData[_LD_SliceIndex] );

				o.worldPosXZ = worldPosXZ;

				return o;
			}

			half Frag( Varyings input ) : SV_Target
			{
				half result = 0.0;

				if (all(_PaintedWavesSize > 0.0))
				{
					float2 paintUV = (input.worldPosXZ - _PaintedWavesPosition) / _PaintedWavesSize + 0.5;
					// Check if in bounds
					if (all(saturate(paintUV) == paintUV))
					{
						result = _PaintedWavesData.Sample(LODData_linear_clamp_sampler, paintUV).x;
					}
				}

				return _Weight * result;
			}
			ENDCG
		}
	}
}
