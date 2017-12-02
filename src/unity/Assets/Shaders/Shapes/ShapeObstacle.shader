// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// This shader blats out any wave amplitude, and adds some foam for fun.
// TODO this could take alpha values from geometry, so an artist could author a mesh which prescribes wave damping.
Shader "Ocean/Shape/Obstacle"
{
	Properties
	{
		_Foam("Foam", Range(0,1)) = 0.5
	}

	Category
	{
		// base simulation runs on the Geometry queue, before this shader.
		Tags { "Queue"="Transparent" }

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode" = "Always" }
				// multiply blend - can mask out particular channels
				//Blend DstColor Zero

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float altitude : TEXCOORD0;
				};

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );

					float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.altitude = worldPos.y;

					return o;
				}

				uniform float _Foam;

				float4 frag (v2f i) : SV_Target
				{
					clip(i.altitude - -.5);

					// stamp down 0 wave height, and some foam
					return float4(0., 0., _Foam, 0.);
				}

				ENDCG
			}
		}
	}
}
