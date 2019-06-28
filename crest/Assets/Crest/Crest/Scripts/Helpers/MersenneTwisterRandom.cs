using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveShapeCalculator
{
    using System;

namespace Random
{
    /* C# Version Copyright (C) 2001 Akihilo Kramot (Takel).       */
    /* C# porting from a C-program for MT19937, originaly coded by */
    /* Takuji Nishimura, considering the suggestions by            */
    /* Topher Cooper and Marc Rieffel in July-Aug. 1997.           */
    /* This library is free software under the Artistic license:   */
    /*                                                             */
    /* You can find the original C-program at                      */
    /* http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/emt.html    */
    /*                                                             */

    /// <summary>
    /// Implements a Mersenne Twister Random Number Generator. This class provides the same interface
    /// as the standard System.Random number generator, plus some additional functions.
    /// </summary>

    public class MersenneTwister: System.Random
    {
        /* Period parameters */
        private const int N = 624;
        private const int M = 397;
        private const uint MATRIX_A = 0x9908b0df; /* constant vector a */
        private const uint UPPER_MASK = 0x80000000; /* most significant w-r bits */
        private const uint LOWER_MASK = 0x7fffffff; /* least significant r bits */

        /* Tempering parameters */
        private const uint TEMPERING_MASK_B = 0x9d2c5680;
        private const uint TEMPERING_MASK_C = 0xefc60000;

        private static uint TEMPERING_SHIFT_U( uint y ) { return ( y >> 11 ); }
        private static uint TEMPERING_SHIFT_S( uint y ) { return ( y << 7 ); }
        private static uint TEMPERING_SHIFT_T( uint y ) { return ( y << 15 ); }
        private static uint TEMPERING_SHIFT_L( uint y ) { return ( y >> 18 ); }

        private uint[] mt = new uint[N]; /* the array for the state vector  */

        private uint seed_;
        private short mti;

        private static uint[] mag01 = { 0x0, MATRIX_A };

        /// <summary>
        /// Create a twister with the specified seed. All sequences started with the same seed will contain
        /// the same random numbers in the same order.
        /// </summary>
        /// <param name="seed">The seed with which to start the twister.</param>

        public MersenneTwister( uint seed )
        {
            Seed = seed;
        }


        /// <summary>
        /// Create a twister seeded from the system clock to make it as random as possible.
        /// </summary>

        public MersenneTwister()
            : this( ( (uint) DateTime.Now.Ticks ) )  // A random initial seed is used.
        {
        }


        /// <summary>
        /// The seed that was used to start the random number generator.
        /// Setting the seed resets the random number generator with the new seed.
        /// All sequences started with the same seed will contain the same random numbers in the same order.
        /// </summary>

        public uint Seed
        {
            set
            {
                seed_ = value;

                /* setting initial seeds to mt[N] using         */
                /* the generator Line 25 of Table 1 in          */
                /* [KNUTH 1981, The Art of Computer Programming */
                /*    Vol. 2 (2nd Ed.), pp102]                  */

                mt[0] = seed_ & 0xffffffffU;
                for ( mti = 1; mti < N; mti++ )
                {
                    mt[mti] = ( 69069 * mt[mti - 1] ) & 0xffffffffU;
                }
            }

            get
            {
                return seed_;
            }
        }


        /// <summary>
        /// Generate a random uint.
        /// </summary>
        /// <returns>A random uint.</returns>

        protected uint GenerateUInt()
        {
            uint y;

            /* mag01[x] = x * MATRIX_A  for x=0,1 */

            if ( mti >= N ) /* generate N words at one time */
            {
                short kk;

                for ( kk = 0; kk < N - M; kk++ )
                {
                    y = ( mt[kk] & UPPER_MASK ) | ( mt[kk + 1] & LOWER_MASK );
                    mt[kk] = mt[kk + M] ^ ( y >> 1 ) ^ mag01[y & 0x1];
                }

                for ( ; kk < N - 1; kk++ )
                {
                    y = ( mt[kk] & UPPER_MASK ) | ( mt[kk + 1] & LOWER_MASK );
                    mt[kk] = mt[kk + ( M - N )] ^ ( y >> 1 ) ^ mag01[y & 0x1];
                }

                y = ( mt[N - 1] & UPPER_MASK ) | ( mt[0] & LOWER_MASK );
                mt[N - 1] = mt[M - 1] ^ ( y >> 1 ) ^ mag01[y & 0x1];

                mti = 0;
            }

            y = mt[mti++];
            y ^= TEMPERING_SHIFT_U( y );
            y ^= TEMPERING_SHIFT_S( y ) & TEMPERING_MASK_B;
            y ^= TEMPERING_SHIFT_T( y ) & TEMPERING_MASK_C;
            y ^= TEMPERING_SHIFT_L( y );

            return y;
        }


        /// <summary>
        /// Returns the next uint in the random sequence.
        /// </summary>
        /// <returns>The next uint in the random sequence.</returns>

        public virtual uint NextUInt()
        {
            return this.GenerateUInt();
        }


        /// <summary>
        /// Returns a random number between 0 and a specified maximum.
        /// </summary>
        /// <param name="maxValue">The upper bound of the random number to be generated. maxValue must be greater than or equal to zero.</param>
        /// <returns>A 32-bit unsigned integer greater than or equal to zero, and less than maxValue; that is, the range of return values includes zero but not MaxValue.</returns>

        public virtual uint NextUInt( uint maxValue )
        {
            return (uint) ( this.GenerateUInt() / ( (double) uint.MaxValue / maxValue ) );
        }


        /// <summary>
        /// Returns an unsigned random number from a specified range.
        /// </summary>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. maxValue must be greater than or equal to minValue.</param>
        /// <returns>A 32-bit signed integer greater than or equal to minValue and less than maxValue;
        /// that is, the range of return values includes minValue but not MaxValue.
        /// If minValue equals maxValue, minValue is returned.</returns>

        public virtual uint NextUInt( uint minValue, uint maxValue ) /* throws ArgumentOutOfRangeException */
        {
            if (minValue >= maxValue)
            {
                if (minValue == maxValue)
                {
                    return minValue;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("minValue", "NextUInt() called with minValue >= maxValue");
                }
            }

            return (uint) ( this.GenerateUInt() / ( (double) uint.MaxValue / ( maxValue - minValue ) ) + minValue );
        }


        /// <summary>
        /// Returns a nonnegative random number.
        /// </summary>
        /// <returns>A 32-bit signed integer greater than or equal to zero and less than int.MaxValue.</returns>

        public override int Next()
        {
            return (int) ( this.GenerateUInt() / 2 );
        }


        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The upper bound of the random number to be generated. maxValue must be greater than or equal to zero.</param>
        /// <returns>A 32-bit signed integer greater than or equal to zero, and less than maxValue;
        /// that is, the range of return values includes zero but not MaxValue.</returns>

        public override int Next( int maxValue ) /* throws ArgumentOutOfRangeException */
        {
            if ( maxValue <= 0 )
            {
                if ( maxValue == 0 )
                    return 0;
                else
                    throw new ArgumentOutOfRangeException( "maxValue", "Next() called with a negative parameter" );
            }

            return (int) ( this.GenerateUInt() / ( uint.MaxValue / maxValue ) );
        }


        /// <summary>
        /// Returns a signed random number from a specified range.
        /// </summary>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. maxValue must be greater than or equal to minValue.</param>
        /// <returns>A 32-bit signed integer greater than or equal to minValue and less than maxValue;
        /// that is, the range of return values includes minValue but not MaxValue.
        /// If minValue equals maxValue, minValue is returned.</returns>

        public override int Next( int minValue, int maxValue ) /* ArgumentOutOfRangeException */
        {
            if (minValue >= maxValue)
            {
                if (minValue == maxValue)
                {
                    return minValue;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("minValue", "Next() called with minValue > maxValue");
                }
            }

            return (int) ( this.GenerateUInt() / ( (double) uint.MaxValue / ( maxValue - minValue ) ) + minValue );
        }


        /// <summary>
        /// Fills an array of bytes with random numbers from 0..255
        /// </summary>
        /// <param name="buffer">The array to be filled with random numbers.</param>

        public override void NextBytes( byte[] buffer ) /* throws ArgumentNullException*/
        {
            int bufLen = buffer.Length;

            if ( buffer == null )
                throw new ArgumentNullException("buffer");

            for ( int idx = 0; idx < bufLen; idx++ )
                buffer[idx] = (byte) ( this.GenerateUInt() / ( uint.MaxValue / byte.MaxValue ) );
        }


        /// <summary>
        /// Returns a double-precision random number in the range [0..1[
        /// </summary>
        /// <returns>A random double-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>

        public override double NextDouble()
        {
            return (double) this.GenerateUInt() / uint.MaxValue;
        }
    }
}
}
