// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Persistent foam sim
Shader "Hidden/Crest/Simulation/Update Foam"
{
	SubShader
	{
		Pass
		{
			Name "UpdateFoam"
			Blend Off
			ZWrite Off
			ZTest Always

			CGPROGRAM
			// For SV_VertexID
			#pragma target 3.5
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"
			#include "../FullScreenTriangle.hlsl"

			float _FoamFadeRate;
			float _WaveFoamStrength;
			float _WaveFoamCoverage;
			float _ShorelineFoamMaxDepth;
			float _ShorelineFoamStrength;
			float _SimDeltaTime;
			float _SimDeltaTimePrev;

			struct Attributes
			{
				uint vertexID : SV_VertexID;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 uv_slice : TEXCOORD0;
				float3 uv_slice_prevFrame : TEXCOORD1;
				float2 positionWS_XZ : TEXCOORD2;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;

				output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
				float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);

				// TODO(MRT): when porting this to geometry shader, set the slice there instead
				output.uv_slice = ADD_SLICE_THIS_LOD_TO_UV(uv);

				// lod data 1 is current frame, compute world pos from quad uv
				output.positionWS_XZ = UVToWorld_ThisFrame(uv);
				output.uv_slice_prevFrame = WorldToUV_PrevFrame(output.positionWS_XZ);

				return output;
			}

			half Frag(Varyings input) : SV_Target
			{
				float3 uv_slice = input.uv_slice;
				float3 uv_slice_prevFrame = input.uv_slice_prevFrame;
				// #if _FLOW_ON
				half3 velocity = half3(_LD_TexArray_Flow_ThisFrame.Sample(LODData_linear_clamp_sampler, uv_slice).xy, 0.0);
				half foam = _LD_TexArray_Foam_PrevFrame.Sample(LODData_linear_clamp_sampler, uv_slice_prevFrame
					- ((_SimDeltaTime * _LD_Params_PrevFrame[_LD_SLICE_Index_ThisLod_PrevFrame].w) * velocity)
					).x;
				// #else
				// // sampler will clamp the uv_slice currently
				// half foam = tex2Dlod(_LD_TexArray_Foam_PrevFrame, uv_slice_prevFrame).x;
				// #endif

				half2 r = abs(uv_slice_prevFrame.xy - 0.5);
				if (max(r.x, r.y) > 0.5 - _LD_Params_PrevFrame[_LD_SLICE_Index_ThisLod_PrevFrame].w)
				{
					// no border wrap mode for RTs in unity it seems, so make any off-texture reads 0 manually
					foam = 0.0;
				}

				// fade
				foam *= max(0.0, 1.0 - _FoamFadeRate * _SimDeltaTime);

				// sample displacement texture and generate foam from it
				const float3 dd = float3(_LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod].w, 0.0, _LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod].x);
				half3 s = SampleLod(_LD_TexArray_AnimatedWaves_ThisFrame, uv_slice).xyz;
				half3 sx = SampleLodLevel(_LD_TexArray_AnimatedWaves_ThisFrame, uv_slice + float3(dd.xy, 0), dd.yy).xyz;
				half3 sz = SampleLodLevel(_LD_TexArray_AnimatedWaves_ThisFrame, uv_slice + float3(dd.yx, 0), dd.yy).xyz;
				float3 disp = s.xyz;
				float3 disp_x = dd.zyy + sx.xyz;
				float3 disp_z = dd.yyz + sz.xyz;
				// The determinant of the displacement Jacobian is a good measure for turbulence:
				// > 1: Stretch
				// < 1: Squash
				// < 0: Overlap
				float4 du = float4(disp_x.xz, disp_z.xz) - disp.xzxz;
				float det = (du.x * du.w - du.y * du.z) / (_LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod].x * _LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod].x);
				foam += 5.0 * _SimDeltaTime * _WaveFoamStrength * saturate(_WaveFoamCoverage - det);

				// add foam in shallow water. use the displaced position to ensure we add foam where world objects are.
				float3 uv_slice_thisFrame_displaced = WorldToUV_ThisFrame(input.positionWS_XZ + disp.xz);
				float signedOceanDepth = SampleLodLevel(_LD_TexArray_SeaFloorDepth_ThisFrame, uv_slice_thisFrame_displaced, float2(0, 1)).x + disp.y;
				foam += _ShorelineFoamStrength * _SimDeltaTime * saturate(1.0 - signedOceanDepth / _ShorelineFoamMaxDepth);


				//TODO(MRT): Remove the need for this by makinng a sampler that returns 0 outisde of the slice bounds.
				if(_LD_SLICE_Index_ThisLod_PrevFrame > 6 || _LD_SLICE_Index_ThisLod_PrevFrame < 0)
				{
					foam = 0;
				}

				return foam;
			}
			ENDCG
		}
	}
}
