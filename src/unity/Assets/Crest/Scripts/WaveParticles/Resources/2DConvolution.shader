Shader "Unlit/2DConvolution"
{
	Properties
	{
		[HDR] _MainTex("Texture", 2D) = "white" {}
		[HDR] _KernelTex("Texture", 2D) = "white" {}
		_HoriRes("Horizontal Resolution", Int) = 80
		_VertRes("Vertical Resolution", Int) = 80
		_Width("Width", Float) = 4.0
		_Height("Height", Float) = 4.0
		_ParticleRadii("Particle Radii", Float) = 0.2
		_KernelWidth("Kernel Width", Float) = 10
		_KernelHeight("Kernel Height", Float) = 10
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

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
			sampler2D _KernelTex;
			float4 _MainTex_ST;
			float _HoriRes;
			float _VertRes;
			float _Width;
			float _Height;
			float _ParticleRadii;
			float _KernelWidth;
			float _KernelHeight;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				//// sample the texture
				const float unitX = _Width / _HoriRes;
				const float unitY = _Height / _VertRes;

				float4 color = float4(0, 0, 0, 1);

				for (float x = 0; x < _KernelWidth; x++) {
					for (float y = 0; y < _KernelHeight; y++) {
						float2 coords = i.uv + float2((x - (_KernelWidth * 0.5))/ _HoriRes, (y - (_KernelHeight * 0.5)) / _VertRes);
						float4 newVal = tex2D(_MainTex, coords);
						float4 kernelColor = tex2D(_KernelTex, float2(x / _KernelWidth, y / _KernelHeight));
						color += newVal.g * kernelColor;
					}
				}

				return color;
			}
			ENDCG
		}
	}
}
