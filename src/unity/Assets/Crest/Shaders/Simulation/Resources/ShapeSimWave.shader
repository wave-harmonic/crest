// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// solve 2D wave equation
Shader "Ocean/Shape/Sim/2D Wave Equation"
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
				#include "../../../../Crest/Shaders/Shapes/MultiscaleShape.cginc"
				#include "../../../../Crest/Shaders/OceanLODData.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 uv_lastframe : TEXCOORD0;
					float2 uv : TEXCOORD1;
				};

				#include "SimHelpers.cginc"

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					ComputeUVs(o.vertex.xy, o.uv_lastframe.xy, o.uv, o.uv_lastframe.z);

					return o;
				}

				uniform half _Damping;
				uniform float2 _LaplacianAxisX;

				float4 frag(v2f i) : SV_Target
				{
					// hack for issue #34 - guard against _SimDeltaTimePrev being 0 (for as yet unknown reasons)
					_SimDeltaTimePrev = max(_SimDeltaTimePrev, 0.001f);

					float4 uv_lastframe = float4(i.uv_lastframe.xy, 0., 0.);

					float4 ft_ftm_faccum_foam = tex2Dlod(_SimDataLastFrame, uv_lastframe);
					if (_SimDeltaTime == 0.0) return ft_ftm_faccum_foam;

					float ft = ft_ftm_faccum_foam.x; // t - current value before update
					float ftm = ft_ftm_faccum_foam.y; // t minus - previous value

					// compute axes of laplacian kernel - rotated every frame
					float e = i.uv_lastframe.z; // assumes square RT
					float4 X = float4(_LaplacianAxisX, 0., 0.);
					float4 Y = float4(-X.y, X.x, 0., 0.);
					float fxm = tex2Dlod(_SimDataLastFrame, uv_lastframe - e*X).x; // x minus
					float fym = tex2Dlod(_SimDataLastFrame, uv_lastframe - e*Y).x; // y minus
					float fxp = tex2Dlod(_SimDataLastFrame, uv_lastframe + e*X).x; // x plus
					float fyp = tex2Dlod(_SimDataLastFrame, uv_lastframe + e*Y).x; // y plus

					const float texelSize = 2. * unity_OrthoParams.x * i.uv_lastframe.z; // assumes square RT

					// average wavelength for this scale
					float wavelength = 1.5 * _TexelsPerWave * texelSize;;
					// could make velocity depend on waves
					//float h = max(waterSignedDepth + ft, 0.);
					float c = ComputeWaveSpeed(wavelength /*, h*/);

					const float dt = _SimDeltaTime;

					// wave propagation
					// velocity is implicit
					float v = (ft - ftm) / _SimDeltaTimePrev;
					float ftp = ft + dt*v + dt*dt*c*c*(fxm + fxp + fym + fyp - 4.*ft) / (texelSize*texelSize);

					// open boundary condition, from: http://hplgit.github.io/wavebc/doc/pub/._wavebc_cyborg002.html .
					// this actually doesn't work perfectly well - there is some minor reflections of high frequencies.
					// dudt + c*dudx = 0
					// (ftp - ft)   +   c*(ft-fxm) = 0.
					if (i.uv_lastframe.x + e >= 1.) ftp = -dt*c*(ft - fxm) + ft;
					if (i.uv_lastframe.y + e >= 1.) ftp = -dt*c*(ft - fym) + ft;
					if (i.uv_lastframe.x - e <= 0.) ftp = dt*c*(fxp - ft) + ft;
					if (i.uv_lastframe.y - e <= 0.) ftp = dt*c*(fyp - ft) + ft;

					// Damping
					ftp *= max(0.0, 1.0 - _Damping * dt);
					//if (-ft < waterSignedDepth)
					//{
					//	ftp = lerp( ft, ftp, min(waterSignedDepth + ft, 1.));
					//}
					//ftp *= lerp( 0.996, 1., clamp(waterSignedDepth, 0., 1.) );

					// Foam
					float accel = ((ftp - ft)/dt - v);
					float foam = -accel * dt;
					foam = max(foam, 0.);

					// attenuate waves based on ocean depth. if depth is greater than 0.5*wavelength, water is considered Deep and wave is
					// unaffected. if depth is less than this, wave velocity decreases. waves will then bunch up and grow in amplitude and
					// eventually break. i model "Deep" water, but then simply ramp down waves in non-deep water with a linear multiplier.
					// http://hyperphysics.phy-astr.gsu.edu/hbase/Waves/watwav2.html
					// http://hyperphysics.phy-astr.gsu.edu/hbase/watwav.html#c1
					float waterSignedDepth = tex2D(_WD_OceanDepth_Sampler_0, float4(i.uv, 0., 0.)).x + DEPTH_BIAS;
					float depthMul = 1. - (1. - saturate(2.0 * waterSignedDepth / wavelength)) * dt * 2.;
					ftp *= depthMul;
					ft *= depthMul;

					float4 result = float4(ftp, ft, 0., foam);

					return result;
				}

				ENDCG
			}
		}
	}
}
