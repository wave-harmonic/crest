
#include "Texture.h"

#include <stdio.h>
#include <stdlib.h>
#include <tchar.h>
#include <time.h>
#include <omp.h>

#include "FreeImage/Dist/FreeImage.h"

void PauseConsole()
{
	int dummy;
	printf( "Press a key\n" );
	scanf_s( "%d", &dummy );
}

bool DumpEXRToTable( const char* exrBaseName, int firstChannel, int lastChannel, bool convertDisplacementToHeightfield, float spatialSize )
{
	char EXRFileName[256];
	sprintf_s( EXRFileName, "%s.exr", exrBaseName );

	if( firstChannel > lastChannel || firstChannel < 0 || lastChannel > 3 )
	{
		printf( "Invalid channel range: [%d,%d]\n", firstChannel, lastChannel );
		PauseConsole();
		return false;
	}

	Texture exr;
	exr.Load( FIF_EXR, EXRFileName );
	if( !exr.IsValid() )
	{
		printf( "Invalid EXR filename: %s\n", EXRFileName );
		PauseConsole();
		return false;
	}

	Texture outExr( exr );
	if( !exr.IsValid() )
	{
		printf( "Could not allocate target EXR image.\n" );
		PauseConsole();
		return false;
	}

	char outTXTFileName[256];
	sprintf_s( outTXTFileName, "%s.txt", EXRFileName );
	FILE* outTXTFile = NULL;
	fopen_s( &outTXTFile, outTXTFileName, "w" );
	if( !outTXTFile )
	{
		printf( "Could not open output file for writing: %s\n", outTXTFileName );
		PauseConsole();
		return false;
	}

	float aveY = 0.f;
	float minY = 10000.f;
	float maxY = -10000.f;
	float aveIters = 0.f;
	float fw = float( exr.GetWidth() ), fh = float( exr.GetHeight() );
	for( unsigned y = 0; y < fh; y++ )
	{
		float v = (0.5f + float( y )) / fh;

		for( unsigned x = 0; x < fw; x++ )
		{
			float u = (0.5f + float( x )) / fw;

			float values[4];

			if( !convertDisplacementToHeightfield )
			{
				exr.GetPixel( x, y, values );

				aveY += values[1];
				minY = fminf( minY, values[1] );
				maxY = fmaxf( maxY, values[1] );
			}
			else
			{
				float u_, v_;
				int iters = FindXZDisplacementSourceUV( exr, spatialSize, u, v, u_, v_ );

				// sample at source uv, convert to heightmap
				exr.GetPixel_Bilin( u_, v_, values );
				values[0] = values[2] = 0.0f;

				float bkp = values[1];
				values[1] = 0.5f + bkp / 4.0f;

				minY = fminf( minY, values[1] );
				maxY = fmaxf( maxY, values[1] );
				aveY += values[1];

				outExr.SetPixel( x, y, values );

				values[1] = bkp;

				//if( x == exr.GetWidth()/2 && y == exr.GetHeight()/2 )
				//	printf( "%d iterations\n", i+1 );

				aveIters += iters;
			}

			for( int i = firstChannel; i <= lastChannel; i++ )
			{
				fprintf( outTXTFile, "%f\t", values[i] );
			}
		}
		fprintf( outTXTFile, "\n" );
	}

	printf( "Average Y:\t%f\n", aveY / (fw*fh) );
	printf( "Min Y:\t\t%f\n", minY );
	printf( "Max Y:\t\t%f\n", maxY );

	if( convertDisplacementToHeightfield )
	{
		printf( "Average iteration count: %f\n", aveIters / (fh*fw) );
	}

	fclose( outTXTFile );

	// save out the heightfield EXR
	char outEXRFileName[256];
	sprintf_s( outEXRFileName, "%s_OUT.exr", exrBaseName );
	outExr.Save( FIF_EXR, outEXRFileName );

	return true;
}

bool DumpEXRToTable( const char* exrBaseName, int firstChannel, int lastChannel )
{
	return DumpEXRToTable( exrBaseName, firstChannel, lastChannel, false, 0.0f );
}

bool LoadGroundTruth(Texture& tGroundTruth, int resX, int resY)
{
	const char* imgGroundTruthName = "GT.exr";
	tGroundTruth.Load(FIF_EXR, imgGroundTruthName);

	if (!tGroundTruth.IsValid() || tGroundTruth.GetWidth() != resX || tGroundTruth.GetHeight() != resY)
	{
		printf("Invalid GT: %s\n", imgGroundTruthName);
		if (tGroundTruth.IsValid())
			printf(" - Invalid dimension\n");

		PauseConsole();
		return false;
	}

	return true;
}

int main(int argc, char* argv[])
{
	srand(0);

	FreeImage_Initialise();

	const int MAX_EXRS = 100;
	char exrFileName[256];
	for( int i = 0; i < MAX_EXRS; i++ )
	{
		sprintf_s( exrFileName, "disp_000%d", i );

		// these correspond to the parameters on the Ocean modifier in blender. these are not stored in the EXR!
		const float SIZE = 1.0f;
		const float SPATIAL_SIZE = 50.0f;
		if( !DumpEXRToTable( exrFileName, 1, 1, true, SIZE*SPATIAL_SIZE ) )
			break;
		
		printf( "%s.. DONE\n", exrFileName );
	}
	FreeImage_DeInitialise();
	PauseConsole();

	return 0;
}
