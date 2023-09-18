// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Dynamic Waves/Sphere-Water Interaction"
{
	SubShader
	{
		Pass
		{
			Blend One One
			ZTest Always
			ZWrite Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanHelpersNew.hlsl"

			CBUFFER_START(CrestPerOceanInput)
			float _SimDeltaTime;
			CBUFFER_END

			float _MinWavelength;

			struct Attributes
			{
				float3 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 offsetXZ : TEXCOORD0;
				float2 positionWS : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			UNITY_INSTANCING_BUFFER_START(CrestPerInstance)
				UNITY_DEFINE_INSTANCED_PROP(float3, _Velocity)
				UNITY_DEFINE_INSTANCED_PROP(float, _Weight)
				UNITY_DEFINE_INSTANCED_PROP(float, _Radius)
				UNITY_DEFINE_INSTANCED_PROP(float3, _DisplacementAtInputPosition)
				UNITY_DEFINE_INSTANCED_PROP(float, _InnerSphereOffset)
				UNITY_DEFINE_INSTANCED_PROP(float, _InnerSphereMultiplier)
				UNITY_DEFINE_INSTANCED_PROP(float, _LargeWaveMultiplier)
			UNITY_INSTANCING_BUFFER_END(CrestPerInstance)

			Varyings Vert(Attributes input)
			{
				Varyings o;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, o);

				const float quadExpand = 4.0;

				float3 vertexWorldPos = mul(unity_ObjectToWorld, float4(input.positionOS * quadExpand, 1.0));
				float3 centerPos = unity_ObjectToWorld._m03_m13_m23;

				o.offsetXZ = vertexWorldPos.xz - centerPos.xz;

				// Correct for displacement
				vertexWorldPos.xz -= UNITY_ACCESS_INSTANCED_PROP(CrestPerInstance, _DisplacementAtInputPosition).xz;

				o.positionCS = mul(UNITY_MATRIX_VP, float4(vertexWorldPos, 1.0));
				o.positionWS = vertexWorldPos.xz;

				float largeWaveMultiplier = UNITY_ACCESS_INSTANCED_PROP(CrestPerInstance, _LargeWaveMultiplier);
				float radius = UNITY_ACCESS_INSTANCED_PROP(CrestPerInstance, _Radius);

				if (largeWaveMultiplier * radius < _CrestCascadeData[_LD_SliceIndex]._texelWidth) o.positionCS *= 0.0;

				return o;
			}

			// Resolution-aware interaction falloff function, inspired by "bandfiltered step" from Ottosson.
			// Basically adding together this falloff function at different scales generates a consistent result
			// that doesn't grow into an ugly uintended shape. Shadertoy with more details: https://www.shadertoy.com/view/WltBWM
			float InteractionFalloff( float a, float x )
			{
				float ax = a * x;
				float ax2 = ax * ax;
				float ax4 = ax2 * ax2;

				return ax / (1.0 + ax2 * ax4);
			}

			// Signed distance field for sphere.
			void SphereSDF(float2 offsetXZ, float radius, out float signedDist, out float2 normal)
			{
				float dist = length(offsetXZ);
				signedDist = dist - radius;
				normal = dist > 0.0001 ? offsetXZ / dist : float2(1.0, 0.0);
			}

			half4 Frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				float radius = UNITY_ACCESS_INSTANCED_PROP(CrestPerInstance, _Radius);
				float3 velocity = UNITY_ACCESS_INSTANCED_PROP(CrestPerInstance, _Velocity);

				// Compute signed distance to sphere (sign gives inside/outside), and outwards normal to sphere surface
				float signedDist;
				float2 sdfNormal;
				SphereSDF(input.offsetXZ, radius, signedDist, sdfNormal);

				// Forces from up/down motion. Push in same direction as vel inside sphere, and opposite dir outside.
				float forceUpDown = 0.0;
				{
					forceUpDown = -velocity.y;

					// Range / radius of interaction force
					const float a = 1.67 / _MinWavelength;
					forceUpDown *= InteractionFalloff( a, signedDist );
				}

				// Forces from horizontal motion - push water up in direction of motion, pull down behind.
				float forceHoriz = 0.0;
				if (signedDist > 0.0 || signedDist < -radius * UNITY_ACCESS_INSTANCED_PROP(CrestPerInstance, _InnerSphereOffset))
				{
					// Range / radius of interaction force.
					const float a = 1.43 / _MinWavelength;

					// Invert within sphere, to balance / negate forces applied outside of sphere.
					float forceSign = sign(signedDist);

					forceHoriz = forceSign * dot( sdfNormal, velocity.xz ) * InteractionFalloff( a, abs(signedDist) );

					// If inside sphere, add an additional weight.
					if (signedDist < 0.0)
					{
						forceHoriz *= UNITY_ACCESS_INSTANCED_PROP(CrestPerInstance, _InnerSphereMultiplier);
					}
				}

				// Add to velocity (y-channel) to accelerate water. Magic number was the default value for _Strength
				// which has been removed.
				float accel = UNITY_ACCESS_INSTANCED_PROP(CrestPerInstance, _Weight) * (forceUpDown + forceHoriz) * 0.2;

				// Helps interaction to work at different scales
				accel /= _MinWavelength;

				// Feather edges to reduce streaking without introducing reflections.
				accel *= FeatherWeightFromUV(WorldToUV(input.positionWS, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex), 0.1);

				return half4(0.0, accel * _SimDeltaTime, 0.0, 0.0);
			}
			ENDCG
		}
	}
}
