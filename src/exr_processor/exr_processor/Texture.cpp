#include "Texture.h"

int FindXZDisplacementSourceUV( const Texture& i_dispTex, float i_dispTexHorizScale, float i_u, float i_v, float& o_u, float& o_v )
{
	float values[4];

	o_u = i_u;
	o_v = i_v;

	i_dispTex.GetPixel_Bilin( o_u, o_v, values );

	// FPI to find solution
	const int MAX_ITERS = 100;
	const float MULTIPLIER = 0.6f;
	const float EPSILON_SQ = 0.000001f;
	int i = 0;
	for( ; i < MAX_ITERS; i++ )
	{
		float dispU = values[0] / i_dispTexHorizScale, dispV = values[2] / i_dispTexHorizScale;
		float errorU = (o_u + dispU) - i_u;
		float errorV = (o_v + dispV) - i_v;
		o_u -= MULTIPLIER * errorU;
		o_v -= MULTIPLIER * errorV;

		i_dispTex.GetPixel_Bilin( o_u, o_v, values );

		if( errorU*errorU + errorV*errorV < EPSILON_SQ )
			break;
	}

	return i;
}
