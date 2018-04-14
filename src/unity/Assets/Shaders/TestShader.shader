Shader "Tessellation Sample" {
    Properties {
        //_MainTex ("Base (RGB)", 2D) = "white" {}
        _NormalMap ("Normalmap", 2D) = "bump" {}
        _Emission ("Emission", color) = (1,1,1,0)
        _EmissionFacing ("Emission Facing", color) = (1,1,1,0)
		_Smoothness("Smoothness", Range(0, 1.0)) = 0.3
	}

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 300
            
        CGPROGRAM
        #pragma surface surf Standard /*addshadow*/ fullforwardshadows vertex:vert nolightmap
        #pragma target 4.6
		//#pragma enable_d3d11_debug_symbols // uncomment to allow debugging in renderdoc etc

        struct appdata {
            float4 vertex : POSITION;
            float4 tangent : TANGENT;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
        };

		struct Input {
		//	half4 invDeterminant_lodAlpha_worldXZUndisplaced;
			half shorelineFoam;
			float3 viewDir;
			float2 myuv;
		};

		#include "OceanLODData.cginc"

		#define DEPTH_BIAS 100.

		uniform float3 _OceanCenterPosWorld;
		uniform float _EnableSmoothLODs = 1.0; // debug

		// INSTANCE PARAMS

		// Geometry data
		// x: A square is formed by 2 triangles in the mesh. Here x is square size
		// yz: normalScrollSpeed0, normalScrollSpeed1
		// w: Geometry density - side length of patch measured in squares
		uniform float4 _GeomData;

		// MeshScaleLerp, FarNormalsWeight, LODIndex (debug), unused
		uniform float4 _InstanceData;

		void SampleDisplacements(in sampler2D i_dispSampler, in sampler2D i_oceanDepthSampler, in float2 i_centerPos, in float i_res, in float i_texelSize, in float i_geomSquareSize, in float2 i_samplePos, in float wt, inout float3 io_worldPos, inout float3 io_n, inout half io_determinant, inout half io_shorelineFoam)
		{
			if (wt < 0.001)
				return;

			float4 uv = float4(WD_worldToUV(i_samplePos, i_centerPos, i_res, i_texelSize), 0., 0.);

			// do computations for hi-res
			float3 dd = float3(i_geomSquareSize / (i_texelSize*i_res), 0.0, i_geomSquareSize);
			float4 s = tex2Dlod(i_dispSampler, uv);
			float4 sx = tex2Dlod(i_dispSampler, uv + dd.xyyy);
			float4 sz = tex2Dlod(i_dispSampler, uv + dd.yxyy);
			float3 disp = s.xyz;
			float3 disp_x = dd.zyy + sx.xyz;
			float3 disp_z = dd.yyz + sz.xyz;
			io_worldPos += wt * disp;

			float3 n = normalize(cross(disp_z - disp, disp_x - disp));
			io_n.xz += wt * n.xz;

			// The determinant of the displacement Jacobian is a good measure for turbulence:
			// > 1: Stretch
			// < 1: Squash
			// < 0: Overlap
			float4 du = float4(disp_x.xz, disp_z.xz) - disp.xzxz;
			float det = (du.x * du.w - du.y * du.z) / (dd.z * dd.z);
			// actually store 1-determinant. This means that when far lod is faded out to 0, this tends to make foam and scatter color etc fade out, instead of getting stronger.
			det = 1. - det;
			io_determinant += wt * det;

			// foam from shallow water - signed depth is depth compared to sea level, plus wave height. depth bias is an optimisation
			// which allows the depth data to be initialised once to 0 without generating foam everywhere.
			half signedDepth = (tex2Dlod(i_oceanDepthSampler, uv).x + DEPTH_BIAS) + disp.y;
			io_shorelineFoam += wt * clamp(1. - signedDepth / 1.5, 0., 1.);
		}

        void vert(inout appdata v, out Input o)
        {
			// see comments above on _GeomData
			const float SQUARE_SIZE = _GeomData.x, SQUARE_SIZE_2 = 2.0*_GeomData.x, SQUARE_SIZE_4 = 4.0*_GeomData.x;
			const float BASE_DENSITY = _GeomData.w;

			// move to world
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

			// snap the verts to the grid
			// The snap size should be twice the original size to keep the shape of the eight triangles (otherwise the edge layout changes).
			worldPos.xz -= frac(_OceanCenterPosWorld.xz / SQUARE_SIZE_2) * SQUARE_SIZE_2; // caution - sign of frac might change in non-hlsl shaders

			// how far are we into the current LOD? compute by comparing the desired square size with the actual square size
			float2 offsetFromCenter = float2(abs(worldPos.x - _OceanCenterPosWorld.x), abs(worldPos.z - _OceanCenterPosWorld.z));
			float taxicab_norm = max(offsetFromCenter.x, offsetFromCenter.y);
			float idealSquareSize = taxicab_norm / BASE_DENSITY;
			// this is to address numerical issues with the normal (errors are very visible at close ups of specular highlights).
			// i original had this max( .., SQUARE_SIZE ) but there were still numerical issues and a pop when changing camera height.
			idealSquareSize = max(idealSquareSize, 0.03125);

			// interpolation factor to next lod (lower density / higher sampling period)
			float lodAlpha = idealSquareSize / SQUARE_SIZE - 1.0;
			// lod alpha is remapped to ensure patches weld together properly. patches can vary significantly in shape (with
			// strips added and removed), and this variance depends on the base density of the mesh, as this defines the strip width.
			// using .15 as black and .85 as white should work for base mesh density as low as 16. TODO - make this automatic?
			const float BLACK_POINT = 0.15, WHITE_POINT = 0.85;
			lodAlpha = max((lodAlpha - BLACK_POINT) / (WHITE_POINT - BLACK_POINT), 0.);
			// blend out lod0 when viewpoint gains altitude
			const float meshScaleLerp = _InstanceData.x;
			lodAlpha = min(lodAlpha + meshScaleLerp, 1.);
			lodAlpha *= _EnableSmoothLODs;
			// pass it to fragment shader - used to blend normals scales
			//o.invDeterminant_lodAlpha_worldXZUndisplaced.y = lodAlpha;

			// now smoothly transition vert layouts between lod levels - move interior verts inwards towards center
			float2 m = frac(worldPos.xz / SQUARE_SIZE_4); // this always returns positive
			float2 offset = m - 0.5;
			// check if vert is within one square from the center point which the verts move towards
			const float minRadius = 0.26; //0.26 is 0.25 plus a small "epsilon" - should solve numerical issues
			if (abs(offset.x) < minRadius) worldPos.x += offset.x * lodAlpha * SQUARE_SIZE_4;
			if (abs(offset.y) < minRadius) worldPos.z += offset.y * lodAlpha * SQUARE_SIZE_4;
			//o.invDeterminant_lodAlpha_worldXZUndisplaced.zw = worldPos.xz;

			// sample shape textures - always lerp between 2 scales, so sample two textures
			float3 normal = half3(0., 1., 0.);
			//o.invDeterminant_lodAlpha_worldXZUndisplaced.x = 0.;
			o.shorelineFoam = 0.;
			// sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
			float wt_0 = (1. - lodAlpha) * _WD_Params_0.z;
			float wt_1 = (1. - wt_0) * _WD_Params_1.z;
			// sample displacement textures, add results to current world pos / normal / foam
			const float2 wxz = worldPos.xz;
			half x = 0.;
			SampleDisplacements(_WD_Sampler_0, _WD_OceanDepth_Sampler_0, _WD_Pos_0, _WD_Params_0.y, _WD_Params_0.x, idealSquareSize, wxz, wt_0, worldPos, normal, x, o.shorelineFoam);
			SampleDisplacements(_WD_Sampler_1, _WD_OceanDepth_Sampler_1, _WD_Pos_1, _WD_Params_1.y, _WD_Params_1.x, idealSquareSize, wxz, wt_1, worldPos, normal, x, o.shorelineFoam);

			// at this point worldPos contains the adjusted world pos

			float3 tangent = float3(1., 0., 0.);
			tangent -= dot(tangent, normal) * normal / dot(normal, normal);
			v.tangent = float4(mul(unity_WorldToObject, float4(tangent, .0)).xyz, -1.);

			float3 objPos = mul(unity_WorldToObject, float4(worldPos,1.)).xyz;
			v.vertex.xyz = objPos;
			//// view-projection	
			//o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.));
			v.normal = /*normalize*/(mul(unity_WorldToObject, float4(normal, 0.)).xyz);
			//v.normal = normalize(normal.xyz);// float3(-normal.x, -normal.z, -normal.y);// 
			//v.normal = float3(0., 1., 0.);
			o.myuv = wxz * .4;// +_Time.w;
			//v.normal = float3(0., 1., 0.);

			o.viewDir = _WorldSpaceCameraPos - worldPos;



			//UNITY_INITIALIZE_OUTPUT(appdata, v);
			//v.vertex.xyz += float3(_Displacement, 0., 0.);// v.normal * d;
			//v.vertex.x += .1 * sin(v.vertex.y*10. + _Time.w);
			//v.vertex.xyz += .1*mul(unity_ObjectToWorld, v.vertex);
			//v.normal.x += .5 * sin(v.vertex.y*10. + _Time.w);
        }

        //sampler2D _MainTex;
        sampler2D _NormalMap;
		half _Smoothness;
		half _Specular;
		half3 _Emission, _EmissionFacing;

        void surf (Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = float4(0.,0.,0.,1.);
			o.Smoothness =  _Smoothness;
			o.Normal = UnpackNormal(tex2D(_NormalMap, IN.myuv));
			half3 view = normalize(IN.viewDir);
			o.Emission = _Emission + _EmissionFacing * dot(view, o.Normal);
        }
        ENDCG
    }

    FallBack "Diffuse"
}
