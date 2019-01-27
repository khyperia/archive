using System;

namespace StellarStack
{
    class StretchedImage : IIntImage
    {
        private readonly IIntImage _source;
        private readonly float _subtractAmount;
        private readonly float _divideAmount;
        private readonly float _gamma;

        public StretchedImage(IIntImage source)
        {
            _source = source;
            var sorted = source.SortIntensities();
            _subtractAmount = sorted[sorted.Length / 20];
            _divideAmount = sorted[sorted.Length - 1] - _subtractAmount;
            _gamma = -(float)Math.Log(2, (sorted[sorted.Length * 9 / 10] - _subtractAmount) / _divideAmount);
        }

        public float this[int x, int y]
        {
            get { return (float)Math.Pow((_source[x, y] - _subtractAmount) / _divideAmount, _gamma); }
        }

        public int Width { get { return _source.Width; } }
        public int Height { get { return _source.Height; } }
    }

    class FlatHistogramImage : IIntImage
    {
        private readonly IIntImage _source;
        private readonly float[] _intensities;

        public FlatHistogramImage(IIntImage source)
        {
            _source = source;
            _intensities = source.SortIntensities();
        }

        public float this[int x, int y]
        {
            get { return Array.BinarySearch(_intensities, _source[x, y]); }
        }

        public int Width { get { return _source.Width; } }
        public int Height { get { return _source.Height; } }
    }
}