Shader "Ocean/AlphaTest"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "../../Crest/Shaders/OceanLODData.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			// MeshScaleLerp, FarNormalsWeight, LODIndex (debug), unused
			uniform float4 _InstanceData;

			v2f vert (appdata v)
			{
				v2f o;

				// move to world
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);

				// vertex snapping and lod transition
				float lodAlpha;
				SnapAndTransitionVertLayout(_InstanceData.x, worldPos, lodAlpha);

				// sample shape textures - always lerp between 2 scales, so sample two textures
				half3 n = half3(0., 1., 0.);
				half invDeterminant_lodAlpha_worldXZUndisplaced_x = 0.;
				half shorelineFoam_screenPos_x = 0.;
				// sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
				float wt_0 = (1. - lodAlpha) * _WD_Params_0.z;
				float wt_1 = (1. - wt_0) * _WD_Params_1.z;
				// sample displacement textures, add results to current world pos / normal / foam
				const float2 wxz = worldPos.xz;
				SampleDisplacements(_WD_Sampler_0, _WD_OceanDepth_Sampler_0, _WD_Pos_0, _WD_Params_0.y, _WD_Params_0.w, _WD_Params_0.x, wxz, wt_0, worldPos, n, invDeterminant_lodAlpha_worldXZUndisplaced_x, shorelineFoam_screenPos_x);
				SampleDisplacements(_WD_Sampler_1, _WD_OceanDepth_Sampler_1, _WD_Pos_1, _WD_Params_1.y, _WD_Params_1.w, _WD_Params_1.x, wxz, wt_1, worldPos, n, invDeterminant_lodAlpha_worldXZUndisplaced_x, shorelineFoam_screenPos_x);

				// lift alpha above water surface slightly
				worldPos.y += 0.02;

				// view-projection	
				o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.));

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);

				col.a = 0.5;

				return col;
			}
			ENDCG
		}
	}
}
