// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// This writes straight into the displacement texture and sets the water height to the y value of the geometry.

Shader "Ocean/Shape/Set Water Height"
{
	Properties
	{
		// See comment on the variable declaration below.
		_ShapeSize("Shape Size", Range(0.0, 20.0)) = 5.0
	}

 	SubShader
	{
		Tags{ "Queue" = "Transparent" }
		LOD 100
 		Pass
		{
			Blend Off

 			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

 			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"
			#include "MultiscaleShape.hlsl"

 			struct appdata
			{
				float4 vertex : POSITION;
			};

 			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			// The approximate size of the object - used as a heuristic to pick an LOD to render into. Small shapes
			// will render into detailed LODs, which have limited view range.
			uniform float _ShapeSize;

 			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				// Wt should usually be used as an alpha value to support smoothly transitioning between LODs,
				// but i wont bother with this for now.
				float wt;
				if (!SamplingIsAppropriate(_ShapeSize, wt))
				{
					o.vertex *= 0.;
				}

				return o;
			}

 			half4 frag (v2f i) : SV_Target
			{
				// Write displacement to get from sea level of ocean to the y value of this geometry
				return half4(0., i.worldPos.y - _OceanCenterPosWorld.y, 0., 1.);
			}
			ENDCG
		}
	}
}
