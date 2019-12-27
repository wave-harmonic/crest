// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Improved attenuation for vertex lights. Call this routine in the vertex shader to save on performance.
// https://forum.unity.com/threads/point-light-in-v-f-shader.499717/#post-3250481
void CalculateVertexLightsAttenuation(out half4 uv, in const float3 positionWS)
{
	for (int index = 0; index < 4; index++)
	{
		float3 lightPosition = float3(unity_4LightPosX0[index], unity_4LightPosY0[index], unity_4LightPosZ0[index]);
		float range = (0.005 * sqrt(1000000 - unity_4LightAtten0[index])) / sqrt(unity_4LightAtten0[index]);
		uv[index] = distance(lightPosition, positionWS.xyz) / range;
	}
}

// Returns vertex light colour with custom attenuation. Call in fragment shader.
half3 CalculateVertexLightsColor(in const half4 uv, in const float3 positionWS, in const sampler2D lightsTexture)
{
	half3 lightsColor = 0;
	for (int index = 0; index < 4; index++)
	{
		float3 lightPosition = float3(unity_4LightPosX0[index], unity_4LightPosY0[index], unity_4LightPosZ0[index]);
		float attenuation = tex2D(lightsTexture, (uv[index] * uv[index]).xx).UNITY_ATTEN_CHANNEL;
		lightsColor.rgb += unity_LightColor[index].rgb * (1 / distance(lightPosition, positionWS.xyz)) * attenuation;
	}
	return lightsColor;
}
