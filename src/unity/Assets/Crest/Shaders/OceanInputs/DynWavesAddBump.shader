// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Inputs/Dynamic Waves/Add Bump"
{
	Properties
	{
		_Amplitude( "Amplitude", float ) = 1
		_Radius( "Radius", float) = 3
	}

	SubShader
	{
		Pass
		{
			Blend One One
			
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			struct Attributes
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct Varyings
			{
				float4 vertex : SV_POSITION;
				float2 worldOffsetScaled : TEXCOORD0;
			};

			float _Radius;
			float _SimCount;
			float _SimDeltaTime;

			Varyings Vert(Attributes v)
			{
				Varyings o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				float3 centerPos = unity_ObjectToWorld._m03_m13_m23;
				o.worldOffsetScaled.xy = worldPos.xz - centerPos.xz;

				// shape is symmetric around center with known radius - fix the vert positions to perfectly wrap the shape.
				o.worldOffsetScaled.xy = sign(o.worldOffsetScaled.xy);
				float4 newWorldPos = float4(centerPos, 1.0);
				newWorldPos.xz += o.worldOffsetScaled.xy * _Radius;
				o.vertex = mul(UNITY_MATRIX_VP, newWorldPos);

				return o;
			}

			float _Amplitude;

			struct SimOutput
			{
				half2 h_hprev : SV_Target0;
				half4 uv_uvprev : SV_Target1;
			};

			SimOutput Frag(Varyings input)
			{
				SimOutput o = (SimOutput)0;

				// power 4 smoothstep - no normalize needed
				// credit goes to stubbe's shadertoy: https://www.shadertoy.com/view/4ldSD2
				float r2 = dot(input.worldOffsetScaled.xy, input.worldOffsetScaled.xy);
				if (r2 > 1.0)
					return o;

				r2 = 1.0 - r2;

				float y = r2 * r2;
				y = pow(y, 0.05);
				y *= _Amplitude;

				if (_SimCount > 0.0) // user friendly - avoid nans
					y /= _SimCount;

				// treat as an acceleration - dt^2
				o.h_hprev = float2(_SimDeltaTime * _SimDeltaTime * y, 0.0);
				return o;
			}

			ENDCG
		}
	}
}
