// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

uniform half3 _Diffuse;

#if _TRANSPARENCY_ON
// this is copied from the render target by unity
uniform sampler2D _BackgroundTexture;

uniform half4 _DepthFogDensity;
#endif // _TRANSPARENCY_ON

#if _SUBSURFACESCATTERING_ON
uniform half3 _SubSurfaceColour;
uniform half _SubSurfaceBase;
uniform half _SubSurfaceSun;
uniform half _SubSurfaceSunFallOff;
uniform half _SubSurfaceHeightMax;
uniform half _SubSurfaceHeightPower;
uniform half3 _SubSurfaceCrestColour;
#endif // _SUBSURFACESCATTERING_ON

#if _SUBSURFACESHALLOWCOLOUR_ON
uniform half _SubSurfaceDepthMax;
uniform half _SubSurfaceDepthPower;
uniform half3 _SubSurfaceShallowCol;
#if _SHADOWS_ON
uniform half3 _SubSurfaceShallowColShadow;
#endif // _SHADOWS_ON
#endif // _SUBSURFACESHALLOWCOLOUR_ON

#if _CAUSTICS_ON
uniform sampler2D _CausticsTexture;
uniform half _CausticsTextureScale;
uniform half _CausticsTextureAverage;
uniform half _CausticsStrength;
uniform half _CausticsFocalDepth;
uniform half _CausticsDepthOfField;
uniform half _CausticsDistortionScale;
uniform half _CausticsDistortionStrength;
#endif // _CAUSTICS_ON

#if _SHADOWS_ON
uniform half3 _DiffuseShadow;
#endif

half3 OceanEmission(float3 worldPos, half oceanDepth, half3 view, half3 n, half3 n_geom, float3 lightDir, fixed i_shadow, half4 grabPos, half3 screenPos, float pixelZ, half2 uvDepth, float sceneZ, float sceneZ01, half3 bubbleCol, in sampler2D i_normals, in sampler2D i_cameraDepths)
{
	// base colour
	half3 col = _Diffuse;

#if _SHADOWS_ON
	col = lerp(_DiffuseShadow, col, i_shadow);
#endif

#if _SUBSURFACESCATTERING_ON
	{
#if _SUBSURFACESHALLOWCOLOUR_ON
		float shallowness = pow(1. - saturate(oceanDepth / _SubSurfaceDepthMax), _SubSurfaceDepthPower);
		half3 shallowCol = _SubSurfaceShallowCol;
#if _SHADOWS_ON
		shallowCol = lerp(_SubSurfaceShallowColShadow, shallowCol, i_shadow);
#endif
		col = lerp(col, shallowCol, shallowness);
#endif

#if _SUBSURFACEHEIGHTLERP_ON
		half h = worldPos.y - _OceanCenterPosWorld.y;
		col += pow(saturate(0.5 + 2.0 * h / _SubSurfaceHeightMax), _SubSurfaceHeightPower) * _SubSurfaceCrestColour.rgb;
#endif

		// light
		// use the constant term (0th order) of SH stuff - this is the average. it seems to give the right kind of colour
		col *= half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

		// Approximate subsurface scattering - add light when surface faces viewer. Use geometry normal - don't need high freqs.
		half towardsSun = pow(max(0., dot(lightDir, -view)), _SubSurfaceSunFallOff);
		col += (_SubSurfaceBase + _SubSurfaceSun * towardsSun) * max(dot(n_geom, view), 0.) * _SubSurfaceColour.rgb * _LightColor0 * i_shadow;
	}
#endif // _SUBSURFACESCATTERING_ON

	// underwater bubbles reflect in light
	col += bubbleCol;

#if _TRANSPARENCY_ON

	// zfar? then don't read from the backbuffer at all, as i get occasionally nans spread across the screen when reading
	// from uninit'd backbuffer
	if (sceneZ01 != 0.0)
	{
		half2 uvBackgroundRefract = grabPos.xy / grabPos.w + .02 * n.xz;
		half2 uvDepthRefract = uvDepth + .02 * n.xz;
		half3 alpha = (half3)1.;

		// if we haven't refracted onto a surface in front of the water surface, compute an alpha based on Z delta
		if (sceneZ > pixelZ)
		{
			float sceneZRefract = LinearEyeDepth(tex2D(i_cameraDepths, uvDepthRefract).x);
			float maxZ = max(sceneZ, sceneZRefract);
			float deltaZ = maxZ - pixelZ;
			alpha = 1. - exp(-_DepthFogDensity.xyz * deltaZ);
		}

		half3 sceneColour = tex2D(_BackgroundTexture, uvBackgroundRefract).rgb;

#if _CAUSTICS_ON
		// could sample from the screen space shadow texture to attenuate this..
		// underwater caustics - dedicated to P
		float3 camForward = mul((float3x3)unity_CameraToWorld, float3(0., 0., 1.));
		float3 scenePos = _WorldSpaceCameraPos - view * sceneZ / dot(camForward, -view);
		const float2 scenePosUV = LD_1_WorldToUV(scenePos.xz);
		half3 disp = 0.; half2 n_dummy = 0.;
		// this gives height at displaced position, not exactly at query position.. but it helps. i cant pass this from vert shader
		// because i dont know it at scene pos.
		SampleDisplacements(_LD_Sampler_AnimatedWaves_1, scenePosUV, 1.0, _LD_Params_1.w, _LD_Params_1.x, disp, n_dummy);
		half waterHeight = _OceanCenterPosWorld.y + disp.y;
		half sceneDepth = waterHeight - scenePos.y;
		half bias = abs(sceneDepth - _CausticsFocalDepth) / _CausticsDepthOfField;
		// project along light dir, but multiply by a fudge factor reduce the angle bit - compensates for fact that in real life
		// caustics come from many directions and don't exhibit such a strong directonality
		float2 surfacePosXZ = scenePos.xz + lightDir.xz * sceneDepth / (4.*lightDir.y);
		half2 causticN = _CausticsDistortionStrength * UnpackNormal(tex2D(i_normals, surfacePosXZ / _CausticsDistortionScale)).xy;
		half4 cuv1 = half4((surfacePosXZ / _CausticsTextureScale + 1.3 *causticN + half2(0.88*_Time.x + 17.16, -3.38*_Time.x)), 0., bias);
		half4 cuv2 = half4((1.37*surfacePosXZ / _CausticsTextureScale + 1.77*causticN + half2(4.96*_Time.x, 2.34*_Time.x)), 0., bias);

		half causticsStrength = _CausticsStrength;
#if _SHADOWS_ON
		{
			// only sample the bigger lod. if pops are noticeable this could lerp the 2 lods smoothly, but i didnt notice issues.
			fixed2 causticShadow = 0.;
			float2 uv_1 = LD_1_WorldToUV(surfacePosXZ);
			SampleShadow(_LD_Sampler_Shadow_1, uv_1, 1.0, causticShadow);
			causticsStrength *= 1. - causticShadow.y;
		}
#endif

		sceneColour *= 1. + causticsStrength *
			(0.5*tex2Dbias(_CausticsTexture, cuv1).x + 0.5*tex2Dbias(_CausticsTexture, cuv2).x - _CausticsTextureAverage);
#endif

		col = lerp(sceneColour, col, alpha);
	}
#endif // _TRANSPARENCY_ON

	return col;
}
