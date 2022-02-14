// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_TEXTURE_INCLUDED
#define CREST_TEXTURE_INCLUDED

#include "../OceanGlobals.hlsl"
#include "../OceanInputsDriven.hlsl"

namespace WaveHarmonic
{
	namespace Crest
	{
		struct TiledTexture
		{
			Texture2D _texture;
			SamplerState _sampler;
			half _size;
			half _scale;
			float _texel;

			static TiledTexture Make
			(
				in const Texture2D i_texture,
				in const SamplerState i_sampler,
				in const float4 i_size,
				in const half i_scale
			)
			{
				TiledTexture tiledTexture;
				tiledTexture._texture = i_texture;
				tiledTexture._sampler = i_sampler;
				tiledTexture._scale = i_scale;
				// Safely assume a square texture.
				tiledTexture._size = i_size.z;
				tiledTexture._texel = i_size.x;
				return tiledTexture;
			}

			half4 Sample(float2 uv)
			{
				return _texture.Sample(_sampler, uv);
			}

			half4 SampleLevel(float2 uv, float lod)
			{
				return _texture.SampleLevel(_sampler, uv, lod);
			}

#if CREST_FLOATING_ORIGIN
			float2 FloatingOriginOffset()
			{
				// Safely assumes a square texture.
				return _CrestFloatingOriginOffset.xz % (_scale * _size * _texel);
			}

			float2 FloatingOriginOffset(const CascadeParams i_cascadeData)
			{
				// Safely assumes a square texture.
				return _CrestFloatingOriginOffset.xz % (_scale * _size * i_cascadeData._texelWidth);
			}
#endif // CREST_FLOATING_ORIGIN
		};
	}
}

#endif // CREST_TEXTURE_INCLUDED
