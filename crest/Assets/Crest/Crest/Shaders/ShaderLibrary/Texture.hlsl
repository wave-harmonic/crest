// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

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
				// "texture" is a reserved word.
				TiledTexture _texture;
				_texture._texture = i_texture;
				_texture._sampler = i_sampler;
				_texture._scale = i_scale;
				// Safely assume a square texture.
				_texture._size = i_size.z;
				_texture._texel = i_size.x;
				return _texture;
			}

			half4 Sample(float2 uv)
			{
				return _texture.Sample(_sampler, uv);
			}

			half4 SampleLevel(float2 uv, float lod)
			{
				return _texture.SampleLevel(_sampler, uv, lod);
			}
		};
	}
}
