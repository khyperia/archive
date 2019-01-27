using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StellarStack
{
    class AlignedImage : IImage
    {
        private readonly IImage _source;
        private readonly float _shiftx;
        private readonly float _shifty;

        public AlignedImage(IImage source, float shiftx, float shifty)
        {
            _source = source;
            _shiftx = shiftx;
            _shifty = shifty;
        }

        public float this[float x, float y]
        {
            get { return _source[x + _shiftx, y + _shifty]; }
        }

        public static IEnumerable<IImage> Align(IIntImage reference, IEnumerable<IIntImage> images)
        {
            var referenceMatrix = reference == null ? null : ToMatrix(reference);

            return from image in images
                   let shift = PhaseCorrelation(ToMatrix(image), ref referenceMatrix)
                   select new AlignedImage(new IntToNormalImage(image), shift.X, shift.Y);
        }

        private static Vector2 PhaseCorrelation(Complex[,] matrix, ref Complex[,] referenceMatrix)
        {
            FourierTransform.FFT2(matrix, FourierTransform.Direction.Forward);

            if (referenceMatrix == null)
            {
                referenceMatrix = matrix;
                return new Vector2(0, 0);
            }

            MultiplyAndNorm(matrix, referenceMatrix);
            FourierTransform.FFT2(matrix, FourierTransform.Direction.Backward);
            var correlatedImage = FromMatrix(matrix);
            return FindShift(correlatedImage);
        }

        private static Complex[,] ToMatrix(IIntImage image)
        {
            var width = FourierTransform.Pow2(FourierTransform.Log2(image.Width) - 1);
            var height = FourierTransform.Pow2(FourierTransform.Log2(image.Height) - 1);
            var matrix = new Complex[width, height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    matrix[x, y] = image[x, y];
            return matrix;
        }

        private static IIntImage FromMatrix(Complex[,] matrix)
        {
            var image = new float[matrix.GetLength(0), matrix.GetLength(1)];
            for (var y = 0; y < image.GetLength(1); y++)
                for (var x = 0; x < image.GetLength(0); x++)
                    image[x, y] = (float)matrix[x, y].Real;
            return new BufferImage(image);
        }

        private static void MultiplyAndNorm(Complex[,] left, Complex[,] right)
        {
            for (var y = 0; y < left.GetLength(1); y++)
            {
                for (var x = 0; x < left.GetLength(0); x++)
                {
                    left[x, y] = (left[x, y] * new Complex(right[x, y].Real, -right[x, y].Imaginary)) / (left[x, y] * right[x, y]).Magnitude;
                }
            }
        }

        private static Vector2 FindShift(IIntImage image)
        {
            var max = float.MinValue;
            var result = Vector2.NaN;
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var value = image[x, y];
                    if (value > max)
                    {
                        var xSubpixel = Ext.GaussianMean(image[x - 1, y], image[x, y], image[x + 1, y]);
                        var ySubpixel = Ext.GaussianMean(image[x, y - 1], image[x, y], image[x, y + 1]);

                        if (Math.Abs(xSubpixel) < 2 && Math.Abs(ySubpixel) < 2)
                        {
                            max = value;
                            result = new Vector2(x + xSubpixel, y + ySubpixel);
                        }
                    }
                }
            }
            result = new Vector2(
                result.X > image.Width / 2.0f ? result.X - image.Width : result.X,
                result.Y > image.Height / 2.0f ? result.Y - image.Height : result.Y);

            Console.WriteLine("registration {0} {1}", result.X, result.Y);
            return result;
        }
    }
}
