// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Experiment - project texture onto water surface using raymarching. This saves needing a grabpass
// to get the depths with water included. However a grabpass would probably be simpler, and maybe
// cheaper, than this approach. Grabpass is possible on BIRP, and projected decals on water already
// works on HDRP, so this is mainly concerning URP which doesn't seem to have built-in grabpass.

// TODO - needs to sample opaque depth buffer so it doesnt draw in front of scene
// TODO - needs to stay within box
// TODO - could probaby use bisection search or similar

Shader "Crest/Project Onto Water"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent+100" }
        LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

			#include "OceanGlobals.hlsl"
			#include "OceanInputsDriven.hlsl"
			#include "OceanHelpersNew.hlsl"
			#include "OceanVertHelpers.hlsl"
			#include "OceanShaderHelpers.hlsl"

			UNITY_DECLARE_SCREENSPACE_TEXTURE( _CameraDepthTexture );

			struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
				float3 worldPos : TEXCOORD2;
				float4 vertex : SV_POSITION;
				float3 screenPosXYW : TEXCOORD9;
			};

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
				o.worldPos = mul( UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0) );
				o.screenPosXYW = ComputeGrabScreenPos( o.vertex ).xyw;
				return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float3 V = normalize( i.worldPos - _WorldSpaceCameraPos.xyz );

				const CascadeParams cascadeData0 = _CrestCascadeData[_LD_SliceIndex];

				float3 x = i.worldPos;
				float2 dispxz = 0.;
				float diff = 0.0;
				for( int i = 0; i < 5; i++ )
				{
					const float3 uv_slice = WorldToUV( x.xz, cascadeData0, _LD_SliceIndex );
					half variance = 0.0;
					float3 disp = 0.0;
					SampleDisplacements( _LD_TexArray_AnimatedWaves, uv_slice, 1.0, disp, variance );
					dispxz = disp.xz;
					float y = disp.y + _OceanCenterPosWorld.y;
					float3 step = V * (x.y - y); // / max( -V.y, 0.2 );
					diff = dot( step, step );
					x += step;
				}

				float3 screenPos = mul( UNITY_MATRIX_VP, float4(x, 1.0) ).xyw;
				float2 uvDepth = screenPos.xy / screenPos.z;
				uvDepth = uvDepth * 0.5 + 0.5;
				uvDepth.y = 1.0 - uvDepth.y;
				
				// Raw depth is logarithmic for perspective, and linear (0-1) for orthographic.
				float rawDepth = SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, uvDepth ).x;
				float sceneZ = CrestLinearEyeDepth( rawDepth );
				float3 camForward = mul( (float3x3)unity_CameraToWorld, float3(0., 0., 1.) );
				clip( sceneZ - dot(x - _WorldSpaceCameraPos.xyz, camForward ) );

				x.xz -= dispxz;
				float alpha = tex2D( _MainTex, x.xz / 10.0 ).x;
				alpha = smoothstep( 0.1, 0.2, alpha );
				return float2(1.0, alpha).xxxy;
            }
            ENDCG
        }
    }
}
