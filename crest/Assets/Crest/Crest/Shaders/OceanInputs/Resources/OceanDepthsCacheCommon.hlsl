// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

struct Attributes
{
	float3 positionOS : POSITION;
	float2 uv : TEXCOORD0;
};

struct VaryingsVS
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

struct VaryingsGS
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	uint sliceIndex : SV_RenderTargetArrayIndex;
};

VaryingsVS Vert(Attributes input)
{
	VaryingsVS output;

#ifdef CREST_OCEAN_DEPTHS_GEOM_SHADER_ON
	// Geometry shader version - go to world space
	output.position = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
#else
	// Normal (non-GS) - transform to clip space.
	output.position = UnityObjectToClipPos(input.positionOS);
#endif

	output.uv = TRANSFORM_TEX(input.uv, _MainTex);

	return output;
}

[maxvertexcount(MAX_LOD_COUNT * 3)]
void Geometry(
	triangle VaryingsVS input[3],
	inout TriangleStream<VaryingsGS> outStream
)
{
	VaryingsGS output;
	for (int sliceIndex = 0; sliceIndex < _CurrentLodCount; sliceIndex++)
	{
		output.sliceIndex = sliceIndex;
		for (int vertex = 0; vertex < 3; vertex++)
		{
			// Project to each slice
			output.position = mul(_SliceViewProjMatrices[sliceIndex], input[vertex].position);
			output.uv = input[vertex].uv;
			outStream.Append(output);
		}
		outStream.RestartStrip();
	}
}

#ifdef CREST_OCEAN_DEPTHS_GEOM_SHADER_ON
half4 Frag(VaryingsGS input) : SV_Target
#else
half4 Frag(VaryingsVS input) : SV_Target
#endif
{
	return half4(tex2D(_MainTex, input.uv).x, 0.0, 0.0, 0.0);
}
