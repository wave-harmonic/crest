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
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"

			float _FoamFadeRate;
			float _WaveFoamStrength;
			float _WaveFoamCoverage;
			float _ShorelineFoamMaxDepth;
			float _ShorelineFoamStrength;
			float _SimDeltaTime;
			float _SimDeltaTimePrev;

			struct Attributes
			{
				// the input geom has clip space positions
				float4 positionCS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 uv_slice : TEXCOORD0;
				float3 uv_slice_lastframe : TEXCOORD1;
				float2 positionWS_XZ : TEXCOORD2;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				o.positionCS = input.positionCS;

#if !UNITY_UV_STARTS_AT_TOP // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
				o.positionCS.y = -o.positionCS.y;
#endif

				// TODO(MRT): when porting this to geometry shader, set the slice there instead
				o.uv_slice = ADD_SLICE_0_TO_UV(input.uv);

				// lod data 1 is current frame, compute world pos from quad uv
				o.positionWS_XZ = LD_1_UVToWorld(input.uv);
				o.uv_slice_lastframe = ADD_SLICE_0_TO_UV(LD_0_WorldToUV(o.positionWS_XZ));

				return o;
			}

			half Frag(Varyings input) : SV_Target
			{
				float3 uv_slice = input.uv_slice;
				float3 uv_slice_lastframe = input.uv_slice_lastframe;
				// #if _FLOW_ON
				half3 velocity = half3(_LD_TexArray_Flow_1.Sample(LODData_linear_clamp_sampler, uv_slice).xy, 0.0);
				half foam = _LD_TexArray_Foam_0.Sample(LODData_linear_clamp_sampler, uv_slice_lastframe
					- ((_SimDeltaTime * _LD_Params_0.w) * velocity)
					).x;
				// #else
				// // sampler will clamp the uv_slice currently
				// half foam = tex2Dlod(_LD_TexArray_Foam_0, uv_slice_lastframe).x;
				// #endif

				half2 r = abs(uv_slice_lastframe.xy - 0.5);
				if (max(r.x, r.y) > 0.5 - _LD_Params_0.w)
				{
					// no border wrap mode for RTs in unity it seems, so make any off-texture reads 0 manually
					foam = 0.0;
				}

				// fade
				foam *= max(0.0, 1.0 - _FoamFadeRate * _SimDeltaTime);

				// sample displacement texture and generate foam from it
				const float3 dd = float3(_LD_Params_1.w, 0.0, _LD_Params_1.x);
				half3 s = _LD_TexArray_AnimatedWaves_1.Sample(LODData_linear_clamp_sampler, uv_slice).xyz;
				half3 sx = _LD_TexArray_AnimatedWaves_1.SampleLevel(LODData_linear_clamp_sampler, uv_slice + float3(dd.xy, 0), dd.yy).xyz;
				half3 sz = _LD_TexArray_AnimatedWaves_1.SampleLevel(LODData_linear_clamp_sampler, uv_slice + float3(dd.yx, 0), dd.yy).xyz;
				float3 disp = s.xyz;
				float3 disp_x = dd.zyy + sx.xyz;
				float3 disp_z = dd.yyz + sz.xyz;
				// The determinant of the displacement Jacobian is a good measure for turbulence:
				// > 1: Stretch
				// < 1: Squash
				// < 0: Overlap
				float4 du = float4(disp_x.xz, disp_z.xz) - disp.xzxz;
				float det = (du.x * du.w - du.y * du.z) / (_LD_Params_1.x * _LD_Params_1.x);
				foam += 5.0 * _SimDeltaTime * _WaveFoamStrength * saturate(_WaveFoamCoverage - det);

				// add foam in shallow water. use the displaced position to ensure we add foam where world objects are.
				float3 uv_slice_1_displaced = float3(LD_1_WorldToUV(input.positionWS_XZ + disp.xz), uv_slice.z);
				float signedOceanDepth = _LD_TexArray_SeaFloorDepth_1.SampleLevel(LODData_linear_clamp_sampler, uv_slice_1_displaced, float2(0, 1)).x + disp.y;
				foam += _ShorelineFoamStrength * _SimDeltaTime * saturate(1.0 - signedOceanDepth / _ShorelineFoamMaxDepth);

				return foam;
			}
			ENDCG
		}
	}
}
