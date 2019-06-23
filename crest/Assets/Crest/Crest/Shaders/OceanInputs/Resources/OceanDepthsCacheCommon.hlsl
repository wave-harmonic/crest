#include "UnityCG.cginc"
#include "../../OceanLodData.hlsl"

sampler2D _MainTex;
float4 _MainTex_ST;
int _CurrentLodCount;
float4 ObjectToPosition(float3 objectPosition);

struct Attributes
{
	float3 positionOS : POSITION;
	float2 uv : TEXCOORD0;
};

struct Varyings
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};


Varyings Vert(Attributes input)
{
	Varyings output;
	output.position = ObjectToPosition(input.positionOS);
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);
	return output;
}

half4 Frag(SlicedVaryings input) : SV_Target
{
	return half4(tex2D(_MainTex, input.uv).x, 0.0, 0.0, 0.0);
}
