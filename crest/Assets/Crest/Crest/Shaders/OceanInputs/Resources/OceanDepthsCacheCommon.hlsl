// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

sampler2D _MainTex;
float4 _MainTex_ST;
int _CurrentLodCount;

struct Attributes
{
	float3 positionOS : POSITION;
	float2 uv : TEXCOORD0;
};

struct Varyings
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	uint sliceIndex : SV_RenderTargetArrayIndex;
};

Varyings Vert(Attributes input)
{
	Varyings output;

#ifdef CREST_OCEAN_DEPTHS_GEOM_SHADER_ON
	// Geometry shader version - go to world space
	output.position = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
#else
	// Normal (non-GS) - transform to clip space.
	output.position = UnityObjectToClipPos(input.positionOS);
#endif

	output.uv = TRANSFORM_TEX(input.uv, _MainTex);
	// May be overwritten by geometry shader
	output.sliceIndex = 0;
	return output;
}

half4 Frag(Varyings input) : SV_Target
{
	return half4(tex2D(_MainTex, input.uv).x, 0.0, 0.0, 0.0);
}
