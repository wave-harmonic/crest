// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Mask/Mask Using Stencil"
{
	Properties
	{
	}

	SubShader
	{
		Tags { "Queue"="Geometry+501" }

		Stencil {
			Ref 1
			Comp always
			Pass replace
		}

		ColorMask 0
		ZWrite Off

		Pass
		{
		}
	}
}
