Shader "Masked/Mask Using Stencil"
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
