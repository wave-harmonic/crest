// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Experimenting with using a geometry shader to extrude triangles and use this to write displacements into sim. It turns out that a simple vert shader
// seems to do a pretty good job, but leaving this experiment here for now.

// The starting point for this shader came from Shaders Laboratory: http://www.shaderslab.com/shaders.html

Shader "Ocean/Shape/Extrude Test"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Factor("Factor", Range(0., 2.)) = 0.2
		_Velocity("Velocity", Vector) = (0,0,0,0)
	}

	SubShader
	{
		//Tags{ "Queue" = "Transparent" }
		//Blend SrcAlpha One

		Pass
		{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma geometry geom

		#include "UnityCG.cginc"

		struct v2g
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 uv : TEXCOORD0;
		};

		struct g2f
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			fixed4 col : COLOR;
		};

		sampler2D _MainTex;
		float4 _MainTex_ST;

		v2g vert(appdata_base v)
		{
			v2g o;
			o.vertex = v.vertex;
			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			o.normal = v.normal;
			return o;
		}

		float _Factor;
		float4 _Velocity;

		[maxvertexcount(24)]
		void geom(triangle v2g IN[3], inout TriangleStream<g2f> tristream)
		{
			g2f o;

			float3 edgeA = mul(unity_ObjectToWorld, IN[1].vertex - IN[0].vertex).xyz;
			float3 edgeB = mul(unity_ObjectToWorld, IN[2].vertex - IN[0].vertex).xyz;
			
			float3 normalFace = normalize(cross(edgeA, edgeB));

			_Velocity /= 30.;
			float velMag = max(length(_Velocity), 0.001);
			float3 velN = _Velocity / velMag;
			float angleFactor = dot(velN, normalFace);

			float4 colBefore = (fixed4)0.5;
			float4 colAfter = (fixed4)1.;
			colBefore.a = colAfter.a = 0.5;

			if (angleFactor < -0.0001)
			{
				_Velocity = -_Velocity;
				colAfter.xyz *= -1.;
				angleFactor *= -1.;
			}

			float4 offset = float4(normalFace, 0) * _Factor * velMag * (1. - angleFactor) + _Velocity;

			// create strip to bridge original edge positions to extruded edges of face
			for (int i = 0; i < 3; i++)
			{
				int inext = (i + 1) % 3;

				o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, IN[i].vertex) + offset);
				o.uv = IN[i].uv;
				o.col = colAfter;
				tristream.Append(o);

				o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, IN[i].vertex) + offset);
				o.uv = IN[i].uv;
				o.col = colBefore;
				tristream.Append(o);

				o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, IN[inext].vertex) + offset);
				o.uv = IN[inext].uv;
				o.col = colBefore;
				tristream.Append(o);

				tristream.RestartStrip();

				o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, IN[i].vertex) + offset);
				o.uv = IN[i].uv;
				o.col = colAfter;
				tristream.Append(o);

				o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, IN[inext].vertex) + offset);
				o.uv = IN[inext].uv;
				o.col = colBefore;
				tristream.Append(o);

				o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, IN[inext].vertex) + offset);
				o.uv = IN[inext].uv;
				o.col = colAfter;
				tristream.Append(o);

				tristream.RestartStrip();
			}

			// the extruded faces at their new positions
			for (int i = 0; i < 3; i++)
			{
				o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, IN[i].vertex) + offset);
				o.uv = IN[i].uv;
				o.col = colAfter;
				tristream.Append(o);
			}

			tristream.RestartStrip();

			//// original faces
			//for (int i = 0; i < 3; i++)
			//{
			//	o.pos = UnityObjectToClipPos(IN[i].vertex);
			//	o.uv = IN[i].uv;
			//	o.col = colBefore;
			//	tristream.Append(o);
			//}

			//tristream.RestartStrip();

			//// original faces flipped to cap end - each extruded tri becomes a closed surface
			//{
			//	o.col = colBefore;

			//	o.pos = UnityObjectToClipPos(IN[0].vertex);
			//	o.uv = IN[0].uv;
			//	tristream.Append(o);
			//	o.pos = UnityObjectToClipPos(IN[2].vertex);
			//	o.uv = IN[2].uv;
			//	tristream.Append(o);
			//	o.pos = UnityObjectToClipPos(IN[1].vertex);
			//	o.uv = IN[1].uv;
			//	tristream.Append(o);
			//}

			//tristream.RestartStrip();
		}

		fixed4 frag(g2f i) : SV_Target
		{
			fixed4 col = tex2D(_MainTex, i.uv) * i.col;
			return col;
		}
			ENDCG
		}
	}
}