// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// solve 2D wave equation
Shader "Ocean/Shape/Sim/2D Wave Equation"
{
	Properties
	{
	}

	Category
	{
		// Base simulation runs first on geometry queue, no blending.
		// Any interactions will additively render later in the transparent queue.
		Tags { "Queue"="Geometry" }

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode" = "Always" }

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"
				#define PI 3.141592653

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float4 uv : TEXCOORD0;
					float2 uv_uncompensated : TEXCOORD1;
				};

				uniform float3 _CameraPositionDelta;

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );

					// compute uncompensated uv
					o.uv_uncompensated.xy = o.vertex.xy;
					o.uv_uncompensated.y = -o.uv_uncompensated.y;
					o.uv_uncompensated.xy = 0.5*o.uv_uncompensated.xy + 0.5;

					// compensate for camera motion - adjust lookup uv to get texel from last frame sim
					o.uv.xy = o.uv_uncompensated;
					o.uv.zw = float2(1., 1.) / _ScreenParams.xy;
					const float texelSize = 2. * unity_OrthoParams.x * o.uv.z; // assumes square RT
					o.uv.xy += o.uv.zw * _CameraPositionDelta.xz / texelSize;

					return o;
				}

				// respects the gui option to freeze time
				uniform float _MyTime;
				uniform float _MyDeltaTime;

				// simulate wavelengths between _TexelsPerWave texels and 2*_TexelsPerWave texels
				uniform float _TexelsPerWave;

				uniform sampler2D _WavePPTSource;

				#include "../OceanLODData.cginc"

				float4 frag (v2f i) : SV_Target
				{
					// this is required because border color is not supported for unity render textures :(
					if (i.uv.x >= 1. || i.uv.y >= 1. || i.uv.x <= 0. || i.uv.y <= 0.) return (float4)0.;

					float3 e = float3(i.uv.zw, 0.);

					float4 ft_ftm_faccum_foam = tex2D(_WavePPTSource, i.uv);
					float ft = ft_ftm_faccum_foam.x; // t - current value before update
					float ftm = ft_ftm_faccum_foam.y; // t minus - previous value
					float fxm = tex2D(_WavePPTSource, i.uv - e.xz).x; // x minus
					float fym = tex2D(_WavePPTSource, i.uv - e.zy).x; // y minus
					float fxp = tex2D(_WavePPTSource, i.uv + e.xz).x; // x plus
					float fyp = tex2D(_WavePPTSource, i.uv + e.zy).x; // y plus

					const float texelSize = 2. * unity_OrthoParams.x * i.uv.z; // assumes square RT

					// wave speed of deep sea ocean waves: https://en.wikipedia.org/wiki/Wind_wave
					// C = sqrt( gL/2pi )
					float g = 9.81;
					float Lmin = _TexelsPerWave * texelSize;
					// to compute wave speed, take "middle" wavelength of this band (Lmin,2.*Lmin)
					float L = Lmin * 1.5;
					float c = sqrt(g*L / 6.28318);

					const float dt = _MyDeltaTime;
					// dont support variable framerates, so just abort if dt == 0
					if (dt < 0.01) return ft_ftm_faccum_foam;

					// wave propagation
					// velocity is implicit - current and previous values stored, time step assumed to be constant.
					// this only works at a fixed framerate 60hz!
					float ftp = ft + (ft - ftm) + dt*dt*c*c*(fxm + fxp + fym + fyp - 4.*ft) / (texelSize*texelSize);

					// open boundary condition, from: http://hplgit.github.io/wavebc/doc/pub/._wavebc_cyborg002.html .
					// this actually doesn't work perfectly well - there is some minor reflections of high frequencies.
					// dudt + c*dudx = 0
					// (ftp - ft)   +   c*(ft-fxm) = 0.
					if (i.uv.x + e.x >= 1.) ftp = -dt*c*(ft - fxm) + ft;
					if (i.uv.y + e.y >= 1.) ftp = -dt*c*(ft - fym) + ft;
					if (i.uv.x - e.x <= 0.) ftp = dt*c*(fxp - ft) + ft;
					if (i.uv.y - e.y <= 0.) ftp = dt*c*(fyp - ft) + ft;

					// Damping
					ftp *= max(0.0, 1.0 - 0.04 * dt);
					ftp *= clamp(tex2D(_WD_OceanDepth_Sampler_0, i.uv_uncompensated), 0.998, 1.);

					// Foam
					float accel = ((ftp - ft) - (ft - ftm));
					float foam = -accel * 600.;
					foam = max(foam, 0.);
					// foam could be faded slowly across frames, but right now the combine pass uses the foam channel for
					// accumulation, so the last frames foam value cannot be used.

					// z channel will be used to accumulate simulation results down the lod chain
					return float4( ftp, ft, 0., foam );
				}

				ENDCG
			}
		}
	}
}
