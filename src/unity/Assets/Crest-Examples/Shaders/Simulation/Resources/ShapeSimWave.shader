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

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 uv_lastframe : TEXCOORD0;
				};

				#include "SimHelpers.cginc"

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					float2 uv;
					ComputeUVs(o.vertex.xy, o.uv_lastframe.xy, uv, o.uv_lastframe.z);

					return o;
				}

				uniform half _Damping;
				uniform float2 _LaplacianAxisX;

				// respects the gui option to freeze time
				uniform float _MyTime;

				uniform sampler2D _SimDataLastFrame;

				float4 frag(v2f i) : SV_Target
				{
					// if i.uv is out of bounds, it will be clamped. this seems to work ok-ish, it doesnt generate shock
					// waves, but it does produce some stretchy artifacts at edges.

					float4 uv_lastframe = float4(i.uv_lastframe.xy, 0., 0.);

					float4 ft_ftm_faccum_foam = tex2Dlod(_SimDataLastFrame, uv_lastframe);
					if (_SimDeltaTime < 0.01) return ft_ftm_faccum_foam;

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

					//float waterSignedDepth = tex2D(_WD_OceanDepth_Sampler_0, float4(i.uv_uncompensated, 0., 0.)).x;
					//float h = max(waterSignedDepth + ft, 0.);
					float wavelength = 1.5 * _TexelsPerWave * texelSize;;
					float c = ComputeWaveSpeed(wavelength /*, h*/);

					const float dt = _SimDeltaTime;

					// wave propagation
					// velocity is implicit - current and previous values stored, time step assumed to be constant.
					// this only works at a fixed framerate 60hz!
					float ftp = ft + (ft - ftm) + dt*dt*c*c*(fxm + fxp + fym + fyp - 4.*ft) / (texelSize*texelSize);

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
					float accel = ((ftp - ft) - (ft - ftm));
					float foam = -accel;
					//foam = smoothstep(.0, .05, foam);
					foam = max(foam, 0.);
					// foam could be faded slowly across frames, but right now the combine pass uses the foam channel for
					// accumulation, so the last frames foam value cannot be used.

					// z channel will be used to accumulate simulation results down the lod chain. the obstacle shader
					// uses two different blend modes - it multiplies xyz and adds some foam for shallow water.
					float4 result = float4(ftp, ft, 0., foam);

					return result;
				}

				ENDCG
			}
		}
	}
}
