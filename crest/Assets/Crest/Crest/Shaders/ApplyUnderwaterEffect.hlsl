// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)


half3 ApplyUnderwaterEffect(
	in Texture2DArray i_dispSampler,
	in sampler2D i_normalsSampler,
	in const float3 i_cameraPos,
	in const half3 i_ambientLighting,
	half3 i_sceneColour,
	in const float i_sceneZ,
	in const half3 i_view,
	in const half4 i_depthFogDensity,
	in const bool i_isOceanSurface
) {
	const float3 lightDir = _WorldSpaceLightPos0.xyz;

	half3 scatterCol = 0.0;
	{
		float3 dummy;
		half sss = 0.0;
		const float3 uv_slice = WorldToUV(i_cameraPos.xz);
		SampleDisplacements(i_dispSampler, uv_slice, 1.0, dummy, sss);

		// depth and shadow are computed in ScatterColour when underwater==true, using the LOD1 texture.
		const float depth = 0.0;
		const half shadow = 1.0;

		scatterCol = ScatterColour(i_ambientLighting, depth, i_cameraPos, lightDir, i_view, shadow, true, true, sss);
	}

#if _CAUSTICS_ON
	if (i_sceneZ != 0.0 && !i_isOceanSurface)
	{
		ApplyCaustics(i_view, lightDir, i_sceneZ, i_normalsSampler, true, i_sceneColour);
	}
#endif // _CAUSTICS_ON

	return lerp(i_sceneColour, scatterCol, saturate(1.0 - exp(-i_depthFogDensity.xyz * i_sceneZ)));
}
