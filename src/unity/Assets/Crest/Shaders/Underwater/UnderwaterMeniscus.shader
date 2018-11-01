// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Underwater Meniscus"
{
	Properties
	{
		_MeniscusWidth("Meniscus Width", Range(0.0, 100.0)) = 1.0
		[Toggle] _CompileShaderWithDebugInfo("Compile Shader With Debug Info (D3D11)", Float) = 0
	}

	SubShader
	{
		Tags{ "LightMode" = "ForwardBase" "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			// Could turn this off, and it would allow the ocean surface to render through it
			ZWrite Off
			//Blend SrcAlpha OneMinusSrcAlpha
			Blend DstColor Zero

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#pragma shader_feature _COMPILESHADERWITHDEBUGINFO_ON

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "../OceanLODData.hlsl"
			#include "UnderwaterShared.hlsl"

			uniform float _CrestTime;
			uniform float _MeniscusWidth;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				half4 foam_screenPos : TEXCOORD1;
				half4 grabPos : TEXCOORD2;
				float3 worldPos : TEXCOORD3;
			};

			#define MAX_OFFSET 5.0

			v2f vert (appdata v)
			{
				v2f o;

				// view coordinate frame for camera
				const float3 right = unity_CameraToWorld._11_21_31;
				const float3 up = unity_CameraToWorld._12_22_32;
				const float3 forward = unity_CameraToWorld._13_23_33;

				const float3 nearPlaneCenter = _WorldSpaceCameraPos + forward * _ProjectionParams.y * 1.001;
				// Spread verts across the near plane.
				const float aspect = _ScreenParams.x / _ScreenParams.y;
				o.worldPos = nearPlaneCenter
					+ 2.01 * unity_CameraInvProjection._m11 * aspect * right * v.vertex.x * _ProjectionParams.y
					+ up * v.vertex.z * _ProjectionParams.y;


				if (abs(forward.y) < MAX_UPDOWN_AMOUNT)
				{
					o.worldPos += min(IntersectRayWithWaterSurface(o.worldPos, up), MAX_OFFSET) * up;

					const float offset = 0.001 * _ProjectionParams.y * _MeniscusWidth;
					if (v.vertex.z > 0.49)
					{
						o.worldPos += offset * up;
					}
					else
					{
						o.worldPos -= offset * up;
					}
				}
				else
				{
					// kill completely if looking up/down
					o.worldPos *= 0.0;
				}

				o.vertex = mul(UNITY_MATRIX_VP, float4(o.worldPos, 1.));
				o.vertex.z = o.vertex.w;

				o.foam_screenPos.yzw = ComputeScreenPos(o.vertex).xyw;
				o.foam_screenPos.x = 0.;
				o.grabPos = ComputeGrabScreenPos(o.vertex);

				o.uv = v.uv;

				return o;
			}
			
			half4 frag(v2f i) : SV_Target
			{
				const half3 col = 1.3*half3(0.37, .4, .5);
				float alpha = abs(i.uv.y - 0.5);
				alpha = pow(smoothstep(0.5, .0, alpha), .5);
				return half4(lerp(1., col, alpha), alpha);
			}
			ENDCG
		}
	}
}
