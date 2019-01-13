// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

uniform half3 _Diffuse;

// this is copied from the render target by unity
uniform sampler2D _BackgroundTexture;

#define DEPTH_OUTSCATTER_CONSTANT 0.25

#if _TRANSPARENCY_ON
uniform half _RefractionStrength;
#endif // _TRANSPARENCY_ON
uniform half4 _DepthFogDensity;

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

half3 ScatterColour(
	in const float3 i_surfaceWorldPos, in const half i_surfaceOceanDepth, in const float3 i_cameraPos,
	in const half3 i_lightDir, in const half3 i_view, in const fixed i_shadow,
	in const bool i_underwater, in const bool i_outscatterLight)
{
	half depth;
	half waveHeight;
	half shadow = 0.;
	if (i_underwater)
	{
		// compute scatter colour from cam pos. two scenarios this can be called:
		// 1. rendering ocean surface from bottom, in which case the surface may be some distance away. use the scatter
		//    colour at the camera, not at the surface, to make sure its consistent.
		// 2. for the underwater skirt geometry, we don't have the lod data sampled from the verts with lod transitions etc,
		//    so just approximate by sampling at the camera position.
		// this used to sample LOD1 but that doesnt work in last LOD, the data will be missing.
		const float2 uv_0 = LD_0_WorldToUV(i_cameraPos.xz);
		float seaFloorHeightAboveBaseline = 0.;
		SampleSeaFloorHeightAboveBaseline(_LD_Sampler_SeaFloorDepth_0, uv_0, 1.0, seaFloorHeightAboveBaseline);
		depth = DEPTH_BASELINE - seaFloorHeightAboveBaseline;
		waveHeight = 0.;

		fixed2 shadowSoftHard = 0.;
		SampleShadow(_LD_Sampler_Shadow_0, uv_0, 1.0, shadowSoftHard);
		shadow = 1. - shadowSoftHard.x;
	}
	else
	{
		// above water - take data from geometry
		depth = i_surfaceOceanDepth;
		waveHeight = i_surfaceWorldPos.y - _OceanCenterPosWorld.y;
		shadow = i_shadow;
	}

	// base colour
	half3 col = _Diffuse;

#if _SHADOWS_ON
	col = lerp(_DiffuseShadow, col, shadow);
#endif

#if _SUBSURFACESCATTERING_ON
	{
#if _SUBSURFACESHALLOWCOLOUR_ON
		float shallowness = pow(1. - saturate(depth / _SubSurfaceDepthMax), _SubSurfaceDepthPower);
		half3 shallowCol = _SubSurfaceShallowCol;
#if _SHADOWS_ON
		shallowCol = lerp(_SubSurfaceShallowColShadow, shallowCol, shadow);
#endif
		col = lerp(col, shallowCol, shallowness);
#endif

#if _SUBSURFACEHEIGHTLERP_ON
		col += pow(saturate(0.5 + 2.0 * waveHeight / _SubSurfaceHeightMax), _SubSurfaceHeightPower) * _SubSurfaceCrestColour.rgb;
#endif

		// light
		// use the constant term (0th order) of SH stuff - this is the average. it seems to give the right kind of colour
		col *= half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

		// Approximate subsurface scattering - add light when surface faces viewer. Use geometry normal - don't need high freqs.
		half towardsSun = pow(max(0., dot(i_lightDir, -i_view)), _SubSurfaceSunFallOff);
		col += (_SubSurfaceBase + _SubSurfaceSun * towardsSun) * _SubSurfaceColour.rgb * _LightColor0 * shadow;
	}
#endif // _SUBSURFACESCATTERING_ON

	// outscatter light - attenuate the final colour by the camera depth under the water, to approximate less
	// throughput due to light scatter as the camera gets further under water.
	if (i_outscatterLight)
	{
		half camDepth = i_surfaceWorldPos.y - _WorldSpaceCameraPos.y;
		if (i_underwater)
		{
			col *= exp(-_DepthFogDensity.xyz * camDepth * DEPTH_OUTSCATTER_CONSTANT);
		}
	}

	return col;
}


#if _CAUSTICS_ON
void ApplyCaustics(in const half3 i_view, in const half3 i_lightDir, in const float i_sceneZ, in sampler2D i_normals, in const bool i_underwater, inout half3 io_sceneColour)
{
	// could sample from the screen space shadow texture to attenuate this..
	// underwater caustics - dedicated to P
	float3 camForward = mul((float3x3)unity_CameraToWorld, float3(0., 0., 1.));
	float3 scenePos = _WorldSpaceCameraPos - i_view * i_sceneZ / dot(camForward, -i_view);
	const float2 scenePosUV = LD_1_WorldToUV(scenePos.xz);
	half3 disp = 0.;
	// this gives height at displaced position, not exactly at query position.. but it helps. i cant pass this from vert shader
	// because i dont know it at scene pos.
	SampleDisplacements(_LD_Sampler_AnimatedWaves_1, scenePosUV, 1.0, disp);
	half waterHeight = _OceanCenterPosWorld.y + disp.y;
	half sceneDepth = waterHeight - scenePos.y;
	// Compute mip index manually, with bias based on sea floor depth. We compute it manually because if it is computed automatically it produces ugly patches
	// where samples are stretched/dilated. The bias is to give a focusing effect to caustics - they are sharpest at a particular depth. This doesn't work amazingly
	// well and could be replaced.
	float mipLod = log2(i_sceneZ) + abs(sceneDepth - _CausticsFocalDepth) / _CausticsDepthOfField;
	// project along light dir, but multiply by a fudge factor reduce the angle bit - compensates for fact that in real life
	// caustics come from many directions and don't exhibit such a strong directonality
	float2 surfacePosXZ = scenePos.xz + i_lightDir.xz * sceneDepth / (4.*i_lightDir.y);
	half2 causticN = _CausticsDistortionStrength * UnpackNormal(tex2D(i_normals, surfacePosXZ / _CausticsDistortionScale)).xy;
	half4 cuv1 = half4((surfacePosXZ / _CausticsTextureScale + 1.3 *causticN + half2(0.044*_CrestTime + 17.16, -0.169*_CrestTime)), 0., mipLod);
	half4 cuv2 = half4((1.37*surfacePosXZ / _CausticsTextureScale + 1.77*causticN + half2(0.248*_CrestTime, 0.117*_CrestTime)), 0., mipLod);

	half causticsStrength = _CausticsStrength;
#if _SHADOWS_ON
	{
		fixed2 causticShadow = 0.;
		// As per the comment for the underwater code in ScatterColour,
		// LOD_1 data can be missing when underwater
		if(i_underwater)
		{
			const float2 uv_0 = LD_0_WorldToUV(surfacePosXZ);
			SampleShadow(_LD_Sampler_Shadow_0, uv_0, 1.0, causticShadow);
		}
		else
		{
			// only sample the bigger lod. if pops are noticeable this could lerp the 2 lods smoothly, but i didnt notice issues.
			float2 uv_1 = LD_1_WorldToUV(surfacePosXZ);
			SampleShadow(_LD_Sampler_Shadow_1, uv_1, 1.0, causticShadow);
		}
		causticsStrength *= 1. - causticShadow.y;
	}
#endif // _SHADOWS_ON

	io_sceneColour *= 1. + causticsStrength *
		(0.5*tex2Dlod(_CausticsTexture, cuv1).x + 0.5*tex2Dlod(_CausticsTexture, cuv2).x - _CausticsTextureAverage);
}
#endif // _CAUSTICS_ON


half3 OceanEmission(in const half3 i_view, in const half3 i_n_pixel, in const float3 i_lightDir,
	in const half4 i_grabPos, in const float i_pixelZ, in const half2 i_uvDepth, in const float i_sceneZ, in const float i_sceneZ01,
	in const half3 i_bubbleCol, in sampler2D i_normals, in sampler2D i_cameraDepths, in const bool i_underwater, in const half3 i_scatterCol)
{
	half3 col = i_scatterCol;

	// underwater bubbles reflect in light
	col += i_bubbleCol;

#if _TRANSPARENCY_ON

	// View ray intersects geometry surface either above or below ocean surface

	const half2 uvBackground = i_grabPos.xy / i_grabPos.w;
	half2 uvBackgroundRefract = uvBackground + _RefractionStrength * i_n_pixel.xz;
	half3 sceneColour;
	half3 alpha = 0.;
	float depthFogDistance;

	// Depth fog & caustics - only if view ray starts from above water
	if (!i_underwater)
	{
		const half2 uvDepthRefract = i_uvDepth + _RefractionStrength * i_n_pixel.xz;
		const float sceneZRefract = LinearEyeDepth(tex2D(i_cameraDepths, uvDepthRefract).x);

		// Compute depth fog alpha based on refracted position if it landed on an underwater surface, or on unrefracted depth otherwise
		if (sceneZRefract > i_pixelZ)
		{
			depthFogDistance = sceneZRefract - i_pixelZ;
		}
		else
		{
			// It seems that when MSAA is enabled this can sometimes be negative
			depthFogDistance = max(i_sceneZ - i_pixelZ, 0.0);

			// We have refracted onto a surface in front of the water. Cancel the refraction offset.
			uvBackgroundRefract = uvBackground;
		}

		sceneColour = tex2D(_BackgroundTexture, uvBackgroundRefract).rgb;
#if _CAUSTICS_ON
		ApplyCaustics(i_view, i_lightDir, i_sceneZ, i_normals, i_underwater, sceneColour);
#endif
		alpha = 1.0 - exp(-_DepthFogDensity.xyz * depthFogDistance);
	}
	else
	{
		sceneColour = tex2D(_BackgroundTexture, uvBackgroundRefract).rgb;
		depthFogDistance = i_pixelZ;
		// keep alpha at 0 as UnderwaterReflection shader handles the blend
		// appropriately when looking at water from below
	}

	// blend from water colour to the scene colour
	col = lerp(sceneColour, col, alpha);

#endif // _TRANSPARENCY_ON

	return col;
}
