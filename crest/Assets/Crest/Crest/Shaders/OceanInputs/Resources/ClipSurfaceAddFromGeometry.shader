// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders the geometry to the clip surface texture.

Shader "Crest/Inputs/Clip Surface/Add From Geometry"
{
	SubShader
	{
		Pass
		{
			Blend Off
			ZWrite Off
			ColorMask R
		}
	}
}
