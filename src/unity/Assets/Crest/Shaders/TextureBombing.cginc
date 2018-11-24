// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Texture bombing to get unique texture variation / breakup repeating patterns
// From https://www.shadertoy.com/view/4tyGWK

#define BLEND_WIDTH 0.4
//#define DEBUG_COLORS
//#define JIGGLE

// utilities for randomizing uvs
vec4 hash4( vec2 p ) { return fract(sin(vec4( 1.0+dot(p,vec2(37.0,17.0)), 2.0+dot(p,vec2(11.0,47.0)), 3.0+dot(p,vec2(41.0,29.0)), 4.0+dot(p,vec2(23.0,31.0))))*103.); }
vec2 transformUVs( in vec2 iuvCorner, in vec2 uv )
{
	// random in [0,1]^4
	vec4 tx = hash4( iuvCorner );
	// scale component is +/-1 to mirror
	tx.zw = sign( tx.zw - 0.5 );

	// debug vis
#ifdef JIGGLE
	tx.xy *= .05*sin(5.*iTime + iuvCorner.x + iuvCorner.y);
#endif

	// random scale and offset
	return tx.zw * uv + tx.xy;
}


// new 3 samples version.
// makes heavy use of branching to factor out computation and texture sampling but
// these are optional, and whether they help or not will depend on target platform/hardware
vec4 textureNoTile_3weights( sampler2D samp, in vec2 uv )
{
	vec4 res = (vec4)(0.);
	int sampleCnt = 0; // debug vis
	
	// compute per-tile integral and fractional uvs.
	// flip uvs for 'odd' tiles to make sure tex samples are coherent
	vec2 fuv = mod(uv, 2.);
#ifdef UNITY_VERSION
	// convert from glsl fmod to hlsl :(
	if (fuv.x < 0.) fuv.x += 2.;
	if (fuv.y < 0.) fuv.y += 2.;
#endif
	vec2 iuv = uv - fuv;
	vec3 BL_one = vec3(0.,0.,1.); // xy = bot left coords, z = 1
	if( fuv.x >= 1. ) fuv.x = 2.-fuv.x, BL_one.x = 2.;
	if( fuv.y >= 1. ) fuv.y = 2.-fuv.y, BL_one.y = 2.;
	
	
	// weight orthogonal to diagonal edge = 3rd texture sample
	vec2 iuv3;
	float w3 = (fuv.x+fuv.y) - 1.;
	if( w3 < 0. ) iuv3 = iuv + BL_one.xy, w3 = -w3; // bottom left corner, offset negative, weight needs to be negated
	else iuv3 = iuv + BL_one.zz; // use transform from top right corner
	w3 = smoothstep(BLEND_WIDTH, 1.-BLEND_WIDTH, w3);
	
	// if third sample doesnt dominate, take first two
	if( w3 < 0.999 )
	{
		// use weight along long diagonal edge
		float w12 = dot(fuv,vec2(.5,-.5)) + .5;
		w12 = smoothstep(1.125*BLEND_WIDTH, 1.-1.125*BLEND_WIDTH, w12);

		// take samples from texture for each side of diagonal edge
		if( w12 > 0.001 ) res +=     w12  * texture( samp, transformUVs( iuv + BL_one.zy, uv ) ), sampleCnt++;
		if( w12 < 0.999 ) res += (1.-w12) * texture( samp, transformUVs( iuv + BL_one.xz, uv ) ), sampleCnt++;
	}
	
	// first two samples aren't dominating, take third
	if( w3 > 0.001 ) res = mix( res, texture( samp, transformUVs( iuv3, uv ) ), w3 ), sampleCnt++;

	// debug vis: colour based on num samples taken for vis purposes
#ifdef DEBUG_COLORS
	if( sampleCnt == 1 ) res.rb *= .25;
	if( sampleCnt == 2 ) res.b *= .25;
	if( sampleCnt == 3 ) res.gb *= .25;
#endif
	
	return res;
}
