// unify shape combine traditional and compute shaders

void Flow(out float2 offsets, out float2 weights)
{
	const float period = 3.0 * _LD_Params_0.x;
	const float half_period = period / 2.0;
	offsets = fmod(float2(_CrestTime, _CrestTime + half_period), period);
	weights.x = offsets.x / half_period;
	if (weights.x > 1.0) weights.x = 2.0 - weights.x;
	weights.y = 1.0 - weights.x;
}

half4 ShapeCombineFunction(
	const float2 uv_0, const float2 uv_1, const float2 worldPosXZ
) {

	float2 flow = 0.0;
	SampleFlow(_LD_Sampler_Flow_0, uv_0, 1.0, flow);

	float3 result = 0.0;

	// this lods waves
#if _FLOW_ON
	float2 offsets, weights;
	Flow(offsets, weights);

	float2 uv_0_flow_0 = LD_0_WorldToUV(worldPosXZ - offsets[0] * flow);
	float2 uv_0_flow_1 = LD_0_WorldToUV(worldPosXZ - offsets[1] * flow);
	SampleDisplacements(_LD_Sampler_AnimatedWaves_0, uv_0_flow_0, weights[0], result);
	SampleDisplacements(_LD_Sampler_AnimatedWaves_0, uv_0_flow_1, weights[1], result);
#else
	SampleDisplacements(_LD_Sampler_AnimatedWaves_0, uv_0, 1.0, result);
#endif

	// waves to combine down from the next lod up the chain
	SampleDisplacements(_LD_Sampler_AnimatedWaves_1, uv_1, 1.0, result);

	// TODO - uncomment this define once it works in standalone builds
#if _DYNAMIC_WAVE_SIM_ON
	{
		// convert dynamic wave sim to displacements

		half waveSimY = tex2Dlod(_LD_Sampler_DynamicWaves_0, float4(uv_0, 0.0, 0.0)).x;
		result.y += waveSimY;

		const float2 invRes = float2(_LD_Params_0.w, 0.0);
		const half waveSimY_px = tex2Dlod(_LD_Sampler_DynamicWaves_0, float4(uv_0 + invRes.xy, 0.0, 0.0)).x;
		const half waveSimY_nx = tex2Dlod(_LD_Sampler_DynamicWaves_0, float4(uv_0 - invRes.xy, 0.0, 0.0)).x;
		const half waveSimY_pz = tex2Dlod(_LD_Sampler_DynamicWaves_0, float4(uv_0 + invRes.yx, 0.0, 0.0)).x;
		const half waveSimY_nz = tex2Dlod(_LD_Sampler_DynamicWaves_0, float4(uv_0 - invRes.yx, 0.0, 0.0)).x;
		// compute displacement from gradient of water surface - discussed in issue #18 and then in issue #47

		// For gerstner waves, horiz displacement is proportional to derivative of vertical displacement multiplied by the wavelength
		const float wavelength_mid = 2.0 * _LD_Params_0.x * 1.5;
		const float wavevector = 2.0 * 3.14159 / wavelength_mid;
		const float2 dydx = (float2(waveSimY_px, waveSimY_pz) - float2(waveSimY_nx, waveSimY_nz)) / (2.0 * _LD_Params_0.x);
		float2 dispXZ = _HorizDisplace * dydx / wavevector;

		const float maxDisp = _LD_Params_0.x * _DisplaceClamp;
		dispXZ = clamp(dispXZ, -maxDisp, maxDisp);

		result.xz += dispXZ;
	}
#endif // _DYNAMIC_WAVE_SIM_

	return half4(result, 1.0);
}
