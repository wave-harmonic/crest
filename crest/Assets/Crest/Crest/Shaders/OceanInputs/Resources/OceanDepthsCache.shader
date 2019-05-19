// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Draw cached depths into current frame ocean depth data
Shader "Crest/Inputs/Depth/Cached Depths"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			// Min blending to take the min of all depths. Similar in spirit to zbuffer'd visibility when viewing from top down.
			// To confuse matters further, ocean depth is now more like 'sea floor altitude' - a height above a deep water value,
			// so values are increasing in Y and we need to take the MAX of all depths.
			BlendOp Max

			CGPROGRAM
			#pragma vertex Vert
			#pragma geometry Geometry
			#pragma fragment Frag

			#include "UnityCG.cginc"

			#define SLICE_COUNT 4

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4x4 _SliceViewProjMatrices[SLICE_COUNT];

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			struct SlicedVaryings
			{
				float4 positionCS : SV_POSITION;
				float uv : TEXCOORD0;
				uint sliceIndex : SV_RenderTargetArrayIndex;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = mul(unity_ObjectToWorld, float4(input.positionOS, 1));
				o.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return o;
			}

			float4 ObjectToClipPos(float4 positionCS, uint sliceIndex)
			{
				return mul(_SliceViewProjMatrices[sliceIndex], positionCS);
			}

			[maxvertexcount(SLICE_COUNT * 3)]
			void Geometry(
				triangle SlicedVaryings input[3],
				inout TriangleStream<SlicedVaryings> outStream
			)
			{
				SlicedVaryings output;
				for(int sliceIndex = 0; sliceIndex  < SLICE_COUNT; sliceIndex++)
				{
					output.sliceIndex = sliceIndex;
					for(int vertex = 0; vertex < 3; vertex++)
					{
						output.positionCS = ObjectToClipPos(input[vertex].positionCS, sliceIndex);
						output.uv = input[vertex].uv;
						outStream.Append(output);
					}
					outStream.RestartStrip();
				}
			}

			half4 Frag(SlicedVaryings input) : SV_Target
			{
				return half4(tex2D(_MainTex, input.uv).x, 0.0, 0.0, 0.0);
			}
			ENDCG
		}
	}
}
