using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StellarStack
{
    class BufferImage : IIntImage
    {
        private readonly float[,] _source;

        public BufferImage(float[,] source)
        {
            _source = source;
        }

        public float this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= _source.GetLength(0) || y < 0 || y >= _source.GetLength(1))
                    return 0.0f;
                return _source[x, y];
            }
        }

        public int Width { get { return _source.GetLength(0); } }
        public int Height { get { return _source.GetLength(1); } }
    }

    class FileImage : IImage, IIntImage
    {
        private readonly float[,] _source;

        public FileImage(string filename)
        {
            Console.WriteLine("Loading " + filename);
            var tiff = new TiffBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.Default);
            var image = tiff.Frames[0];
            if (image.Format != PixelFormats.Gray16)
                throw new Exception();
            var temp = new ushort[image.PixelWidth * image.PixelHeight];
            image.CopyPixels(temp, image.PixelWidth * sizeof(ushort), 0);
            _source = new float[image.PixelWidth, image.PixelHeight];
            for (var y = 0; y < _source.GetLength(1); y++)
                for (var x = 0; x < _source.GetLength(0); x++)
                    _source[x, y] = temp[y * _source.GetLength(0) + x];
        }

        public float this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= _source.GetLength(0) || y < 0 || y >= _source.GetLength(1))
                    return 0.0f;
                return _source[x, y];
            }
        }

        public float this[float x, float y]
        {
            get { return this.DefaultGet(x, y); }
        }

        public int Width
        { get { return _source.GetLength(0); } }
        public int Height
        { get { return _source.GetLength(1); } }
    }
}