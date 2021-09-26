// Crest Ocean System

// Adds functions from SRP.

// Taken and modified from:
// com.unity.render-pipelines.core@10.5.0/ShaderLibrary/Common.hlsl
float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
{
	float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
	// positionCS.y was flipped here but that is SRP specific to solve flip baked into matrix.
	return positionCS;
}

// Taken from:
// com.unity.render-pipelines.core@10.5.0/ShaderLibrary/Common.hlsl
float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)
{
	float4 positionCS  = ComputeClipSpacePosition(positionNDC, deviceDepth);
	float4 hpositionWS = mul(invViewProjMatrix, positionCS);
	return hpositionWS.xyz / hpositionWS.w;
}
