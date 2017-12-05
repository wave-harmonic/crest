#ifndef _TEXTURE_H_
#define _TEXTURE_H_

//#include "Util.h"
//#include "Vector3.h"
#include "FreeImage/Dist/FreeImage.h"
#include <math.h>

#include <stdio.h>

class Texture
{
public:
	Texture()
	{
	}

	Texture( const Texture& sizeAndFormtGiver )
	{
		Allocate( sizeAndFormtGiver.GetWidth(), sizeAndFormtGiver.GetHeight(), sizeAndFormtGiver.m_type, FreeImage_GetBPP( sizeAndFormtGiver.m_img ) );
	}

	~Texture()
	{
		Deallocate();
	}

	bool Allocate(int w, int h, FREE_IMAGE_TYPE imgType = FIT_BITMAP, int bpp = 32)
	{
		Deallocate();

		m_img = FreeImage_AllocateT(imgType, w, h, bpp);

		if( m_img )
		{
			m_type = imgType;
		}

		return m_img != NULL;
	}

	void Deallocate()
	{
		if (m_img)
		{
			FreeImage_Unload(m_img);
			m_img = NULL;
			m_type = FIT_UNKNOWN;
		}
	}

	bool Load(FREE_IMAGE_FORMAT format, const char* inFile)
	{
		Deallocate();

		m_img = FreeImage_Load(format, inFile);

		if (m_img == NULL)
		{
			fprintf(stderr, "Couldnt load image %s\n", inFile);
			return false;
		}

		m_type = FreeImage_GetImageType( m_img );

		return true;
	}

	bool Save(FREE_IMAGE_FORMAT format, const char* fileName) const
	{
		if (!m_img)
			return false;

		return FreeImage_Save(format, m_img, fileName) != 0;
	}

	bool GetPixel(unsigned int x, unsigned int y, void* value) const
	{
		if (!m_img)
			return false;

		if (m_type == FIT_BITMAP)
		{
			BOOL result;
			{
//#pragma omp critical(GetPixel)
				result = FreeImage_GetPixelColor(m_img, x, y, (RGBQUAD*)value);
			}
			return result != 0;
		}
		else if (m_type == FIT_FLOAT)
		{
			float* const row = (float*)FreeImage_GetScanLine(m_img, y);

			if (!row)
				return false;

			*(float*)value = row[x];

			return true;
		}
		else if( m_type == FIT_RGBAF )
		{
			float* const row = (float*)FreeImage_GetScanLine( m_img, y );
			
			if( !row )
				return false;

			unsigned bpp = FreeImage_GetBPP( m_img );

			const int CHANNELS = bpp / 32;

			float* writePtr = static_cast<float*>(value);
			for( int xi = 0; xi < CHANNELS; xi++ )
			{
				writePtr[xi] = row[x*CHANNELS + xi];
			}

			return true;
		}

		return false;
	}

	bool GetPixel_Bilin(float x, float y, void* value) const
	{
		if (!m_img)
			return false;

		int w = GetWidth(), h = GetHeight();

		x *= float(w);
		x -= 0.5f; // opengl has pixel centers at 0.5 positions
		x = fmodf(x, float(w));
		if (x < 0.0f) x += float(w);
		int x1, x0 = int(x);
		x -= float(x0);
		x1 = (x0 + 1) % w;

		y *= float(h);
		y -= 0.5f; // opengl has pixel centers at 0.5 positions
		y = fmodf(y, float(h));
		if (y < 0.0f) y += float(h);
		int y1, y0 = int(y);
		y -= float(y0);
		y1 = (y0 + 1) % h;

		if (m_type == FIT_BITMAP)
		{
			RGBQUAD val00, val01, val10, val11;

			if (
				!GetPixel(x0, y0, &val00) ||
				!GetPixel(x1, y0, &val01) ||
				!GetPixel(x0, y1, &val10) ||
				!GetPixel(x1, y1, &val11)
				)
			{
				return false;
			}

			RGBQUAD& result = *(RGBQUAD*)value;

			result.rgbBlue = (BYTE)(((1.0f - x) * val00.rgbBlue + x * val01.rgbBlue) * (1.0f - y)
				+ ((1.0f - x) * val10.rgbBlue + x * val11.rgbBlue) * y);
			result.rgbGreen = (BYTE)(((1.0f - x) * val00.rgbGreen + x * val01.rgbGreen) * (1.0f - y)
				+ ((1.0f - x) * val10.rgbGreen + x * val11.rgbGreen) * y);
			result.rgbRed = (BYTE)(((1.0f - x) * val00.rgbRed + x * val01.rgbRed) * (1.0f - y)
				+ ((1.0f - x) * val10.rgbRed + x * val11.rgbRed) * y);
			result.rgbReserved = (BYTE)(((1.0f - x) * val00.rgbReserved + x * val01.rgbReserved) * (1.0f - y)
				+ ((1.0f - x) * val10.rgbReserved + x * val11.rgbReserved) * y);

			return true;
		}
		else if (m_type == FIT_FLOAT)
		{
			float val00, val01, val10, val11;

			if (
				!GetPixel(x0, y0, &val00) ||
				!GetPixel(x1, y0, &val01) ||
				!GetPixel(x0, y1, &val10) ||
				!GetPixel(x1, y1, &val11)
				)
			{
				return false;
			}

			float& result = *(float*)value;

			result = (((1.0f - x) * val00 + x * val01) * (1.0f - y)
				+ ((1.0f - x) * val10 + x * val11) * y);

			return true;
		}
		else if( m_type == FIT_RGBAF )
		{
			const int CHANNELS = 4;
			float val00[CHANNELS], val01[CHANNELS], val10[CHANNELS], val11[CHANNELS];

			if(
				!GetPixel( x0, y0, val00 ) ||
				!GetPixel( x1, y0, val01 ) ||
				!GetPixel( x0, y1, val10 ) ||
				!GetPixel( x1, y1, val11 )
				)
			{
				return false;
			}

			float* result = (float*)value;
			for( int i = 0; i < CHANNELS; i++ )
			{
				result[i] = (((1.0f - x) * val00[i] + x * val01[i]) * (1.0f - y)
					+ ((1.0f - x) * val10[i] + x * val11[i]) * y);
			}

			return true;
		}

		return false;
	}

	//bool GetPixel_Bilin(float x, float y, Vector3& result) const
	//{
	//	if (m_type == FIT_BITMAP)
	//	{
	//		RGBQUAD value;
	//		if (!GetPixel_Bilin(x, y, &value))
	//			return false;

	//		result.x = value.rgbRed / 255.0f;
	//		result.y = value.rgbGreen / 255.0f;
	//		result.z = value.rgbBlue / 255.0f;

	//		return true;
	//	}
	//	else if (m_type == FIT_FLOAT)
	//	{
	//		float value;
	//		if (!GetPixel_Bilin(x, y, &value))
	//			return false;

	//		result = value;

	//		return true;
	//	}

	//	return false;
	//}

	bool SetPixel(unsigned int x, unsigned int y, void* value)
	{
		if (!m_img)
			return false;

		if (m_type == FIT_BITMAP)
		{
			BOOL result;
			{
//#pragma omp critical(SetPixel)
				result = FreeImage_SetPixelColor(m_img, x, y, (RGBQUAD*)value);
			}
			return result != 0;
		}
		else if ( m_type == FIT_FLOAT )
		{
			float* const row = (float*)FreeImage_GetScanLine(m_img, y);

			if (!row)
				return false;

			row[x] = *(float*)value;

			return true;
		}
		else if( m_type == FIT_RGBAF )
		{
			float* const row = (float*)FreeImage_GetScanLine( m_img, y );

			if( !row )
				return false;

			const int CHANNELS = 4;

			for( int i = 0; i < CHANNELS; i++ )
				row[x * CHANNELS + i] = ((float*)value)[i];

			return true;
		}

		return false;
	}

//	bool SetPixel(unsigned int x, unsigned int y, Vector3& value)
//	{
//		if (m_img == NULL)
//			return false;
//
//		if (m_type == FIT_BITMAP)
//		{
//			value = saturate(value);
//
//			RGBQUAD pixColour;
//			pixColour.rgbRed = (BYTE)(value.x * 255.0f);
//			pixColour.rgbGreen = (BYTE)(value.y * 255.0f);
//			pixColour.rgbBlue = (BYTE)(value.z * 255.0f);
//			pixColour.rgbReserved = 255;
//
//			BOOL result;
//			{
////#pragma omp critical(SetPixel_Vec3)
//				result = FreeImage_SetPixelColor(m_img, x, y, &pixColour);
//			}
//			return result != 0;
//		}
//		else if (m_type == FIT_FLOAT)
//		{
//			float* const row = (float*)FreeImage_GetScanLine(m_img, y);
//
//			if (!row)
//				return false;
//
//			row[x] = value.x;
//
//			return true;
//		}
//
//		return false;
//	}

	int GetWidth() const { return m_img == NULL ? -1 : FreeImage_GetWidth(m_img); }
	int GetHeight() const { return m_img == NULL ? -1 : FreeImage_GetHeight(m_img); }

	bool IsValid() const { return m_img != NULL; }

private:

	FIBITMAP* m_img = NULL;

	FREE_IMAGE_TYPE m_type = FIT_UNKNOWN;
};

int FindXZDisplacementSourceUV( const Texture& i_dispTex, float i_dispTexHorizScale, float i_u, float i_v, float& o_u, float& o_v );

#endif //_TEXTURE_H_
