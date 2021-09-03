// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_OCEAN_LIGHTING_HELPERS_H
#define CREST_OCEAN_LIGHTING_HELPERS_H

#if defined(LIGHTING_INCLUDED)
float3 WorldSpaceLightDir(float3 worldPos)
{
	float3 lightDir = _WorldSpaceLightPos0.xyz;
	if (_WorldSpaceLightPos0.w > 0.)
	{
		// non-directional light - this is a position, not a direction
		lightDir = normalize(lightDir - worldPos.xyz);
	}
	return lightDir;
}
#endif

half3 AmbientLight()
{
	return half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
}

#endif // CREST_OCEAN_LIGHTING_HELPERS_H
