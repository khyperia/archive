using System;
using System.Numerics;

namespace StellarStack
{
    /// <summary>
    /// Fourier transformation.
    /// </summary>
    /// 
    /// <remarks>The class implements one dimensional and two dimensional
    /// Discrete and Fast Fourier Transformation.</remarks>
    /// 
    public static class FourierTransform
    {
        /// <summary>
        /// Fourier transformation direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// Forward direction of Fourier transformation.
            /// </summary>
            Forward = 1,

            /// <summary>
            /// Backward direction of Fourier transformation.
            /// </summary>
            Backward = -1
        };

        /// <summary>
        /// One dimensional Discrete Fourier Transform.
        /// </summary>
        /// 
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        /// 
        public static void DFT(Complex[] data, Direction direction)
        {
            var n = data.Length;
            var dst = new Complex[n];

            // for each destination element
            for (var i = 0; i < n; i++)
            {
                dst[i] = Complex.Zero;

                var arg = -(int)direction * 2.0 * Math.PI * i / n;

                // sum source elements
                for (var j = 0; j < n; j++)
                {
                    var cos = Math.Cos(j * arg);
                    var sin = Math.Sin(j * arg);

                    dst[i] += new Complex((data[j].Real * cos - data[j].Imaginary * sin), (data[j].Real * sin + data[j].Imaginary * cos));
                }
            }

            // copy elements
            if (direction == Direction.Forward)
            {
                // devide also for forward transform
                for (var i = 0; i < n; i++)
                {
                    data[i] = new Complex(dst[i].Real / n, dst[i].Imaginary / n);
                }
            }
            else
            {
                for (var i = 0; i < n; i++)
                {
                    data[i] = dst[i];
                }
            }
        }

        /// <summary>
        /// Two dimensional Discrete Fourier Transform.
        /// </summary>
        /// 
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        /// 
        public static void DFT2(Complex[,] data, Direction direction)
        {
            var n = data.GetLength(0);        // rows
            var m = data.GetLength(1);        // columns
            double arg, cos, sin;
            var dst = new Complex[Math.Max(n, m)];

            // process rows
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < m; j++)
                {
                    dst[j] = Complex.Zero;

                    arg = -(int)direction * 2.0 * Math.PI * j / m;

                    // sum source elements
                    for (var k = 0; k < m; k++)
                    {
                        cos = Math.Cos(k * arg);
                        sin = Math.Sin(k * arg);

                        dst[j] += new Complex((data[i, k].Real * cos - data[i, k].Imaginary * sin), (data[i, k].Real * sin + data[i, k].Imaginary * cos));
                    }
                }

                // copy elements
                if (direction == Direction.Forward)
                {
                    // devide also for forward transform
                    for (var j = 0; j < m; j++)
                    {
                        data[i, j] = new Complex(dst[j].Real / m, dst[j].Imaginary / m);
                    }
                }
                else
                {
                    for (var j = 0; j < m; j++)
                    {
                        data[i, j] = dst[j];
                    }
                }
            }

            // process columns
            for (var j = 0; j < m; j++)
            {
                for (var i = 0; i < n; i++)
                {
                    dst[i] = Complex.Zero;

                    arg = -(int)direction * 2.0 * Math.PI * i / n;

                    // sum source elements
                    for (var k = 0; k < n; k++)
                    {
                        cos = Math.Cos(k * arg);
                        sin = Math.Sin(k * arg);

                        dst[i] += new Complex((data[k, j].Real * cos - data[k, j].Imaginary * sin), (data[k, j].Real * sin + data[k, j].Imaginary * cos));
                    }
                }

                // copy elements
                if (direction == Direction.Forward)
                {
                    // devide also for forward transform
                    for (var i = 0; i < n; i++)
                    {
                        data[i, j] = new Complex(dst[i].Real / n, dst[i].Imaginary / n);
                    }
                }
                else
                {
                    for (var i = 0; i < n; i++)
                    {
                        data[i, j] = dst[i];
                    }
                }
            }
        }

        /// <summary>
        /// Get base of binary logarithm.
        /// </summary>
        /// 
        /// <param name="x">Source integer number.</param>
        /// 
        /// <returns>Power of the number (base of binary logarithm).</returns>
        /// 
        public static int Log2(int x)
        {
            if (x <= 65536)
            {
                if (x <= 256)
                {
                    if (x <= 16)
                    {
                        if (x <= 4)
                        {
                            if (x <= 2)
                            {
                                if (x <= 1)
                                    return 0;
                                return 1;
                            }
                            return 2;
                        }
                        if (x <= 8)
                            return 3;
                        return 4;
                    }
                    if (x <= 64)
                    {
                        if (x <= 32)
                            return 5;
                        return 6;
                    }
                    if (x <= 128)
                        return 7;
                    return 8;
                }
                if (x <= 4096)
                {
                    if (x <= 1024)
                    {
                        if (x <= 512)
                            return 9;
                        return 10;
                    }
                    if (x <= 2048)
                        return 11;
                    return 12;
                }
                if (x <= 16384)
                {
                    if (x <= 8192)
                        return 13;
                    return 14;
                }
                if (x <= 32768)
                    return 15;
                return 16;
            }

            if (x <= 16777216)
            {
                if (x <= 1048576)
                {
                    if (x <= 262144)
                    {
                        if (x <= 131072)
                            return 17;
                        return 18;
                    }
                    if (x <= 524288)
                        return 19;
                    return 20;
                }
                if (x <= 4194304)
                {
                    if (x <= 2097152)
                        return 21;
                    return 22;
                }
                if (x <= 8388608)
                    return 23;
                return 24;
            }
            if (x <= 268435456)
            {
                if (x <= 67108864)
                {
                    if (x <= 33554432)
                        return 25;
                    return 26;
                }
                if (x <= 134217728)
                    return 27;
                return 28;
            }
            if (x <= 1073741824)
            {
                if (x <= 536870912)
                    return 29;
                return 30;
            }
            return 31;
        }

        /// <summary>
        /// Checks if the specified integer is power of 2.
        /// </summary>
        /// 
        /// <param name="x">Integer number to check.</param>
        /// 
        /// <returns>Returns <b>true</b> if the specified number is power of 2.
        /// Otherwise returns <b>false</b>.</returns>
        /// 
        public static bool IsPowerOf2(int x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }

        /// <summary>
        /// One dimensional Fast Fourier Transform.
        /// </summary>
        /// 
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        /// 
        /// <remarks><para><note>The method accepts <paramref name="data"/> array of 2<sup>n</sup> size
        /// only, where <b>n</b> may vary in the [1, 14] range.</note></para></remarks>
        /// 
        /// <exception cref="ArgumentException">Incorrect data length.</exception>
        /// 
        public static void FFT(Complex[] data, Direction direction)
        {
            var n = data.Length;
            var m = Log2(n);

            // reorder data first
            ReorderData(data);

            // compute FFT
            var tn = 1;

            for (var k = 1; k <= m; k++)
            {
                var rotation = GetComplexRotation(k, direction);

                var tm = tn;
                tn <<= 1;

                for (var i = 0; i < tm; i++)
                {
                    var t = rotation[i];

                    for (var even = i; even < n; even += tn)
                    {
                        var odd = even + tm;
                        var ce = data[even];
                        var co = data[odd];

                        var tr = co.Real * t.Real - co.Imaginary * t.Imaginary;
                        var ti = co.Real * t.Imaginary + co.Imaginary * t.Real;

                        data[even] += new Complex(tr, ti);

                        data[odd] = new Complex(ce.Real - tr, ce.Imaginary - ti);
                    }
                }
            }

            if (direction == Direction.Forward)
            {
                for (var i = 0; i < n; i++)
                {
                    data[i] /= (double)n;
                }
            }
        }

        /// <summary>
        /// Two dimensional Fast Fourier Transform.
        /// </summary>
        /// 
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        /// 
        /// <remarks><para><note>The method accepts <paramref name="data"/> array of 2<sup>n</sup> size
        /// only in each dimension, where <b>n</b> may vary in the [1, 14] range. For example, 16x16 array
        /// is valid, but 15x15 is not.</note></para></remarks>
        /// 
        /// <exception cref="ArgumentException">Incorrect data length.</exception>
        /// 
        public static void FFT2(Complex[,] data, Direction direction)
        {
            var k = data.GetLength(0);
            var n = data.GetLength(1);

            // check data size
            if (
                (!IsPowerOf2(k)) ||
                (!IsPowerOf2(n)) ||
                (k < MinLength) || (k > MaxLength) ||
                (n < MinLength) || (n > MaxLength)
                )
            {
                throw new ArgumentException("Incorrect data length.");
            }

            // process rows
            var row = new Complex[n];

            for (var i = 0; i < k; i++)
            {
                // copy row
                for (var j = 0; j < n; j++)
                    row[j] = data[i, j];
                // transform it
                FFT(row, direction);
                // copy back
                for (var j = 0; j < n; j++)
                    data[i, j] = row[j];
            }

            // process columns
            var col = new Complex[k];

            for (var j = 0; j < n; j++)
            {
                // copy column
                for (var i = 0; i < k; i++)
                    col[i] = data[i, j];
                // transform it
                FFT(col, direction);
                // copy back
                for (var i = 0; i < k; i++)
                    data[i, j] = col[i];
            }
        }

        #region Private Region

        public static int Pow2(int power)
        {
            return ((power >= 0) && (power <= 30)) ? (1 << power) : 0;
        }

        private const int MinLength = 2;
        private const int MaxLength = 16384;
        private const int MinBits = 1;
        private const int MaxBits = 14;
        private static readonly int[][] ReversedBits = new int[MaxBits][];
        private static readonly Complex[,][] ComplexRotation = new Complex[MaxBits, 2][];

        // Get array, indicating which data members should be swapped before FFT
        private static int[] GetReversedBits(int numberOfBits)
        {
            if ((numberOfBits < MinBits) || (numberOfBits > MaxBits))
                throw new ArgumentOutOfRangeException();

            // check if the array is already calculated
            if (ReversedBits[numberOfBits - 1] == null)
            {
                var n = Pow2(numberOfBits);
                var rBits = new int[n];

                // calculate the array
                for (var i = 0; i < n; i++)
                {
                    var oldBits = i;
                    var newBits = 0;

                    for (var j = 0; j < numberOfBits; j++)
                    {
                        newBits = (newBits << 1) | (oldBits & 1);
                        oldBits = (oldBits >> 1);
                    }
                    rBits[i] = newBits;
                }
                ReversedBits[numberOfBits - 1] = rBits;
            }
            return ReversedBits[numberOfBits - 1];
        }

        // Get rotation of complex number
        private static Complex[] GetComplexRotation(int numberOfBits, Direction direction)
        {
            var directionIndex = (direction == Direction.Forward) ? 0 : 1;

            // check if the array is already calculated
            if (ComplexRotation[numberOfBits - 1, directionIndex] == null)
            {
                var n = 1 << (numberOfBits - 1);
                var uR = 1.0;
                var uI = 0.0;
                var angle = Math.PI / n * (int)direction;
                var wR = Math.Cos(angle);
                var wI = Math.Sin(angle);
                var rotation = new Complex[n];

                for (var i = 0; i < n; i++)
                {
                    rotation[i] = new Complex(uR, uI);
                    var t = uR * wI + uI * wR;
                    uR = uR * wR - uI * wI;
                    uI = t;
                }

                ComplexRotation[numberOfBits - 1, directionIndex] = rotation;
            }
            return ComplexRotation[numberOfBits - 1, directionIndex];
        }

        // Reorder data for FFT using
        private static void ReorderData(Complex[] data)
        {
            var len = data.Length;

            // check data length
            if ((len < MinLength) || (len > MaxLength) || (!IsPowerOf2(len)))
                throw new ArgumentException("Incorrect data length.");

            var rBits = GetReversedBits(Log2(len));

            for (var i = 0; i < len; i++)
            {
                var s = rBits[i];

                if (s > i)
                {
                    var t = data[i];
                    data[i] = data[s];
                    data[s] = t;
                }
            }
        }

        #endregion
    }
}