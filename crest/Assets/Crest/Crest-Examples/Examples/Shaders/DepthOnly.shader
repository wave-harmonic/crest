// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Examples/DepthOnly"
{
	SubShader
	{
		Tags { "Queue"="Geometry-1" }
		ColorMask 0
		ZWrite On

		Pass {}
	}
}
