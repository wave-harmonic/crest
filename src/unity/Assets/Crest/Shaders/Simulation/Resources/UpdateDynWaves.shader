// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// solve 2D wave equation
Shader "Hidden/Ocean/Simulation/Update Dynamic Waves"
{
	Properties {
	}

	Category
	{
		// Base simulation runs first on geometry queue, no blending.
		// Any interactions will additively render later in the transparent queue.
		Tags { "Queue" = "Geometry" }

		SubShader {
			Pass {

				Name "BASE"
				Tags{ "LightMode" = "Always" }

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"
				#include "../../../../Crest/Shaders/MultiscaleShape.hlsl"
				#include "../../../../Crest/Shaders/OceanLODData.hlsl"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 worldPosLastFrame : TEXCOORD0;
					float2 uv : TEXCOORD1;
				};

				#include "SimHelpers.hlsl"

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					// lod data 1 is current frame, compute world pos from quad uv
					float2 worldXZ = LD_1_UVToWorld(v.uv);

					ComputeUVs(worldXZ, o.vertex.xy, o.worldPosLastFrame, o.uv);
					o.worldPosLastFrame = worldXZ;

					return o;
				}

				uniform half _Damping;
				uniform float2 _LaplacianAxisX;
				uniform half _Gravity;

				#define MIN_DT 0.00001

				half2 frag(v2f i) : SV_Target
				{
					half2 velocity = tex2Dlod(_LD_Sampler_Flow_1, float4(i.uv, 0, 0));
					float2 uv_lastframe = LD_0_WorldToUV(i.worldPosLastFrame - (_SimDeltaTime * velocity));
					float4 uv_lastframe4 = float4(uv_lastframe, 0., 0.);

					half2 ft_ftm = tex2Dlod(_LD_Sampler_DynamicWaves_0, uv_lastframe4);

					float ft = ft_ftm.x; // t - current value before update
					float ftm = ft_ftm.y; // t minus - previous value

					// compute axes of laplacian kernel - rotated every frame
					float e = _LD_Params_0.w; // assumes square RT
					float4 X = float4(_LaplacianAxisX, 0., 0.);
					float4 Y = float4(-X.y, X.x, 0., 0.);
					float fxm = tex2Dlod(_LD_Sampler_DynamicWaves_0, uv_lastframe4 - e*X).x; // x minus
					float fym = tex2Dlod(_LD_Sampler_DynamicWaves_0, uv_lastframe4 - e*Y).x; // y minus
					float fxp = tex2Dlod(_LD_Sampler_DynamicWaves_0, uv_lastframe4 + e*X).x; // x plus
					float fyp = tex2Dlod(_LD_Sampler_DynamicWaves_0, uv_lastframe4 + e*Y).x; // y plus

					// average wavelength for this scale
					float wavelength = 1.5 * _TexelsPerWave * _GridSize;
					// could make velocity depend on waves
					//float h = max(waterSignedDepth + ft, 0.);
					float c = ComputeWaveSpeed(wavelength, _Gravity);

					const float dt = _SimDeltaTime;
					const float dtp = _SimDeltaTimePrev;

					// wave propagation
					// velocity is implicit
					float v = dtp > MIN_DT ? (ft - ftm) / dtp : 0.;

					// damping
					v *= 1. - min(1., _Damping * dt);

					// wave equation
					float ftp = ft + dt*v + dt*dt*c*c*(fxm + fxp + fym + fyp - 4.*ft) / (_GridSize*_GridSize);

					// open boundary condition, from: http://hplgit.github.io/wavebc/doc/pub/._wavebc_cyborg002.html .
					// this actually doesn't work perfectly well - there is some minor reflections of high frequencies.
					// dudt + c*dudx = 0
					// (ftp - ft)   +   c*(ft-fxm) = 0.
					if (uv_lastframe.x + e >= 1.) ftp = -dt*c*(ft - fxm) + ft;
					if (uv_lastframe.y + e >= 1.) ftp = -dt*c*(ft - fym) + ft;
					if (uv_lastframe.x - e <= 0.) ftp = dt*c*(fxp - ft) + ft;
					if (uv_lastframe.y - e <= 0.) ftp = dt*c*(fyp - ft) + ft;

					// attenuate waves based on ocean depth. if depth is greater than 0.5*wavelength, water is considered Deep and wave is
					// unaffected. if depth is less than this, wave velocity decreases. waves will then bunch up and grow in amplitude and
					// eventually break. i model "Deep" water, but then simply ramp down waves in non-deep water with a linear multiplier.
					// http://hyperphysics.phy-astr.gsu.edu/hbase/Waves/watwav2.html
					// http://hyperphysics.phy-astr.gsu.edu/hbase/watwav.html#c1
					float waterSignedDepth = DEPTH_BASELINE - tex2D(_LD_Sampler_SeaFloorDepth_1, float4(i.uv, 0., 0.)).x;
					float depthMul = 1. - (1. - saturate(2.0 * waterSignedDepth / wavelength)) * dt * 2.;
					ftp *= depthMul;
					ft *= depthMul;

					return half2(ftp, ft);
				}

				ENDCG
			}
		}
	}
}
