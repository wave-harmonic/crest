// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_OCEAN_EMISSION_INCLUDED
#define CREST_OCEAN_EMISSION_INCLUDED

half3 ScatterColour
(
	in const half i_surfaceOceanDepth,
	in const float i_shadow,
	in const half sss,
	in const half3 i_view,
	in const half3 i_ambientLighting,
	in const half3 i_lightDir,
	in const half3 i_lightCol,
	in const bool i_underwater
)
{
	// base colour
	float v = abs(i_view.y);
	half3 col = lerp(_Diffuse, _DiffuseGrazing, 1. - pow(v, 1.0));

#if _SHADOWS_ON
	col = lerp(_DiffuseShadow, col, i_shadow);
#endif

#if _SUBSURFACESCATTERING_ON
	{
#if _SUBSURFACESHALLOWCOLOUR_ON
		float shallowness = pow(1. - saturate(i_surfaceOceanDepth / _SubSurfaceDepthMax), _SubSurfaceDepthPower);
		half3 shallowCol = _SubSurfaceShallowCol;
#if _SHADOWS_ON
		shallowCol = lerp(_SubSurfaceShallowColShadow, shallowCol, i_shadow);
#endif
		col = lerp(col, shallowCol, shallowness);
#endif

		col *= i_ambientLighting;

		// Approximate subsurface scattering - add light when surface faces viewer. Use geometry normal - don't need high freqs.
		half towardsSun = pow(max(0., dot(i_lightDir, -i_view)), _SubSurfaceSunFallOff);
		half3 subsurface = (_SubSurfaceBase + _SubSurfaceSun * towardsSun) * _SubSurfaceColour.rgb * i_lightCol * i_shadow;
		if (!i_underwater)
		{
			subsurface *= (1.0 - v * v) * sss;
		}
		col += subsurface;
	}
#endif // _SUBSURFACESCATTERING_ON

	return col;
}


#if _CAUSTICS_ON
void ApplyCaustics
(
	in const WaveHarmonic::Crest::TiledTexture i_causticsTexture,
	in const WaveHarmonic::Crest::TiledTexture i_distortionTexture,
	in const int2 i_positionSS,
	in const float3 i_scenePos,
	in const half3 i_lightDir,
	in const float i_sceneZ,
	in const bool i_underwater,
	inout half3 io_sceneColour,
	in const CascadeParams cascadeData0,
	in const CascadeParams cascadeData1
)
{
	// could sample from the screen space shadow texture to attenuate this..
	// underwater caustics - dedicated to P
	const float3 scenePosUV = WorldToUV(i_scenePos.xz, cascadeData1, _LD_SliceIndex + 1);

	float3 disp = 0.0;
	// this gives height at displaced position, not exactly at query position.. but it helps. i cant pass this from vert shader
	// because i dont know it at scene pos.
	SampleDisplacements(_LD_TexArray_AnimatedWaves, scenePosUV, 1.0, disp);
	half seaLevelOffset = _LD_TexArray_SeaFloorDepth.SampleLevel(LODData_linear_clamp_sampler, scenePosUV, 0.0).y;
	half waterHeight = _OceanCenterPosWorld.y + disp.y + seaLevelOffset;
	half sceneDepth = waterHeight - i_scenePos.y;
	// Compute mip index manually, with bias based on sea floor depth. We compute it manually because if it is computed automatically it produces ugly patches
	// where samples are stretched/dilated. The bias is to give a focusing effect to caustics - they are sharpest at a particular depth. This doesn't work amazingly
	// well and could be replaced.
	float mipLod = log2(max(i_sceneZ, 1.0)) + abs(sceneDepth - _CausticsFocalDepth) / _CausticsDepthOfField;
	// project along light dir, but multiply by a fudge factor reduce the angle bit - compensates for fact that in real life
	// caustics come from many directions and don't exhibit such a strong directonality
	// Removing the fudge factor (4.0) will cause the caustics to move around more with the waves. But this will also
	// result in stretched/dilated caustics in certain areas. This is especially noticeable on angled surfaces.
	float2 surfacePosXZ = i_scenePos.xz + i_lightDir.xz * sceneDepth / (4.*i_lightDir.y);
	half2 causticN = _CausticsDistortionStrength * UnpackNormal(i_distortionTexture.Sample(surfacePosXZ / i_distortionTexture._scale)).xy;
	float3 cuv1 = float3((surfacePosXZ / i_causticsTexture._scale + 1.3 * causticN + float2(0.044 * _CrestTime + 17.16, -0.169 * _CrestTime)), mipLod);
	float3 cuv2 = float3((1.37 * surfacePosXZ / i_causticsTexture._scale + 1.77 * causticN + float2(0.248 * _CrestTime, 0.117 * _CrestTime)), mipLod);

	half causticsStrength = _CausticsStrength;

#if _SHADOWS_ON
	{
		// Apply shadow maps to caustics.
		{
			// Normally, we would use SHADOW_ATTENUATION(), but SHADOWS_SCREEN and UNITY_NO_SCREENSPACE_SHADOWS are not
			// handled for the transparent pass.
			causticsStrength *= LOAD_TEXTURE2D_X(_CrestScreenSpaceShadowTexture, i_positionSS).r;
		}
	}
#endif // _SHADOWS_ON

	io_sceneColour.xyz *= 1.0 + causticsStrength *
	(
		0.5 * i_causticsTexture.SampleLevel(cuv1.xy, cuv1.z).xyz +
		0.5 * i_causticsTexture.SampleLevel(cuv2.xy, cuv2.z).xyz -
		_CausticsTextureAverage
	);
}
#endif // _CAUSTICS_ON


half3 OceanEmission
(
	in const half3 i_view,
	in const half3 i_n_pixel,
	in const float3 i_lightDir,
	in const half4 i_grabPos,
	in const float i_pixelZ,
	const float i_rawPixelZ,
	in const half2 i_uvDepth,
	in const int2 i_positionSS,
	in const float i_sceneZ,
	const float i_rawDepth,
	in const half3 i_bubbleCol,
	in const bool i_underwater,
	in const half3 i_scatterCol,
	in const CascadeParams cascadeData0,
	in const CascadeParams cascadeData1
)
{
	half3 col = i_scatterCol;

	// underwater bubbles reflect in light
	col += i_bubbleCol;

#if _TRANSPARENCY_ON

	// View ray intersects geometry surface either above or below ocean surface

	const half2 uvBackground = i_grabPos.xy / i_grabPos.w;
	half3 sceneColour;
	half3 alpha = 0.;
	float depthFogDistance;

	// Depth fog & caustics - only if view ray starts from above water
	if (!i_underwater)
	{
		const half2 refractOffset = _RefractionStrength * i_n_pixel.xz * min(1.0, 0.5*(i_sceneZ - i_pixelZ)) / i_sceneZ;
		const float rawDepth = CREST_SAMPLE_SCENE_DEPTH_X(i_uvDepth + refractOffset);
		half2 uvBackgroundRefract;

		// Compute depth fog alpha based on refracted position if it landed on an underwater surface, or on unrefracted depth otherwise
#if UNITY_REVERSED_Z
		if (rawDepth < i_rawPixelZ)
#else
		if (rawDepth > i_rawPixelZ)
#endif
		{
			uvBackgroundRefract = uvBackground + refractOffset;
			depthFogDistance = CrestLinearEyeDepth(CREST_MULTISAMPLE_SCENE_DEPTH(uvBackgroundRefract, rawDepth)) - i_pixelZ;
		}
		else
		{
			// It seems that when MSAA is enabled this can sometimes be negative
			depthFogDistance = max(CrestLinearEyeDepth(CREST_MULTISAMPLE_SCENE_DEPTH(uvBackground, i_rawDepth)) - i_pixelZ, 0.0);

			// We have refracted onto a surface in front of the water. Cancel the refraction offset.
			uvBackgroundRefract = uvBackground;
		}

		sceneColour = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_BackgroundTexture, uvBackgroundRefract).rgb;
#if _CAUSTICS_ON
		float3 scenePos = _WorldSpaceCameraPos - i_view * i_sceneZ / dot(unity_CameraToWorld._m02_m12_m22, -i_view);
		ApplyCaustics(_CausticsTiledTexture, _CausticsDistortionTiledTexture, i_positionSS, scenePos, i_lightDir, i_sceneZ, i_underwater, sceneColour, cascadeData0, cascadeData1);
#endif
		alpha = 1.0 - exp(-_DepthFogDensity.xyz * depthFogDistance);
	}
	else
	{
		half2 uvBackgroundRefractSky = uvBackground + _RefractionStrength * i_n_pixel.xz;
		sceneColour = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_BackgroundTexture, uvBackgroundRefractSky).rgb;
		depthFogDistance = i_pixelZ;
		// keep alpha at 0 as UnderwaterReflection shader handles the blend
		// appropriately when looking at water from below
	}

	// blend from water colour to the scene colour
	col = lerp(sceneColour, col, alpha);

#endif // _TRANSPARENCY_ON

	return col;
}

#endif // CREST_OCEAN_EMISSION_INCLUDED
