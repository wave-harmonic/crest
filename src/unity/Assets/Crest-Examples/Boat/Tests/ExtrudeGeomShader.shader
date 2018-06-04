// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Experimenting with using a geometry shader to extrude triangles and use this to write displacements into sim. It turns out that a simple vert shader
// seems to do a pretty good job, but leaving this experiment here for now.

// The starting point for this shader came from Shaders Laboratory: http://www.shaderslab.com/shaders.html

Shader "Ocean/Shape/Extrude Test"
{
	Properties
	{
		_Factor("Factor", Range(0., 2.)) = 0.2
		_Velocity("Velocity", Vector) = (0,0,0,0)
	}

	SubShader
	{
		//Tags{ "Queue" = "Transparent" }
		//Blend SrcAlpha One
		//Cull Off

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
			};

			struct g2f
			{
				float4 pos : SV_POSITION;
				fixed4 col : COLOR;
				float height : TEXCOORD0;
			};

			v2g vert(appdata_base v)
			{
				v2g o;
				o.vertex = v.vertex;
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

				float3 vel = _Velocity;
				vel /= 30.;
				float velMag = max(length(vel), 0.001);
				float3 velN = vel / velMag;
				float angleFactor = dot(velN, normalFace);

				//if (angleFactor <= -0.001)
				//	return;
				//angleFactor = max(0., angleFactor);

				float4 colBefore = (fixed4)0.5 * abs(angleFactor);
				float4 colAfter = (fixed4)1. * abs(angleFactor);
				colBefore.a = colAfter.a = 0.5;

				//if (angleFactor < -0.0001)
				//{
				//	vel = -vel;
				//	colAfter.xyz *= -1.;
				//	angleFactor *= -1.;
				//}

				float4 posBefore[3];
				float heightBefore[3];
				for (int i = 0; i < 3; i++)
				{
					posBefore[i] = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, IN[i].vertex));
					heightBefore[i] = mul(unity_ObjectToWorld, IN[i].vertex).y;
				}
				
				if (min(min(heightBefore[0], heightBefore[1]), heightBefore[2]) > 0.)
				{
					return;
				}

				float3 offset = _Velocity + _Factor * normalFace;
				float4 posAfter[3];
				float heightAfter[3];
				for (int i = 0; i < 3; i++)
				{
					posAfter[i] = mul(UNITY_MATRIX_VP, float4(mul(unity_ObjectToWorld, IN[i].vertex).xyz + offset, 1.));
					heightAfter[i] = mul(unity_ObjectToWorld, IN[i].vertex).y + offset.y;
				}

				// create strip to bridge original edge positions to extruded edges of face
				for (int i = 0; i < 3; i++)
				{
					int inext = (i + 1) % 3;

					o.pos = posBefore[i];
					o.height = heightBefore[i];
					o.col = colBefore;
					tristream.Append(o);

					o.pos = posBefore[inext];
					o.height = heightBefore[inext];
					o.col = colBefore;
					tristream.Append(o);

					o.pos = posAfter[i];
					o.height = heightAfter[i];
					o.col = colAfter;
					tristream.Append(o);

					tristream.RestartStrip();

					o.pos = posAfter[i];
					o.height = heightAfter[i];
					o.col = colAfter;
					tristream.Append(o);

					o.pos = posBefore[inext];
					o.height = heightBefore[inext];
					o.col = colBefore;
					tristream.Append(o);

					o.pos = posAfter[inext];
					o.height = heightAfter[inext];
					o.col = colAfter;
					tristream.Append(o);

					tristream.RestartStrip();
				}

				// the extruded faces at their new positions
				for (int i = 0; i < 3; i++)
				{
					o.pos = posAfter[i];
					o.height = heightAfter[i];
					o.col = colAfter;
					tristream.Append(o);
				}

				tristream.RestartStrip();

				//// original faces
				//for (int i = 0; i < 3; i++)
				//{
				//	o.pos = UnityObjectToClipPos(IN[i].vertex);
				//	o.col = colBefore;
				//	tristream.Append(o);
				//}

				//tristream.RestartStrip();

				//// original faces flipped to cap end - each extruded tri becomes a closed surface
				//{
				//	o.col = colBefore;

				//	o.pos = UnityObjectToClipPos(IN[0].vertex);
				//	tristream.Append(o);
				//	o.pos = UnityObjectToClipPos(IN[2].vertex);
				//	tristream.Append(o);
				//	o.pos = UnityObjectToClipPos(IN[1].vertex);
				//	tristream.Append(o);
				//}

				//tristream.RestartStrip();
			}

			fixed4 frag(g2f i) : SV_Target
			{
				fixed4 col = i.col + .5;
				col *= i.height;
				col = (fixed4)exp(-2.*-i.height);
				return col;
			}
			ENDCG
		}
	}
}
