namespace StellarStack
{
    class IntToNormalImage : IImage
    {
        private readonly IIntImage _image;

        public IntToNormalImage(IIntImage image)
        {
            _image = image;
        }

        public float this[float x, float y]
        {
            get { return _image.DefaultGet(x, y); }
        }
    }

    class ImageRasterizer : IIntImage
    {
        private readonly float[,] _buffer;

        public ImageRasterizer(IImage image, float startx, float starty, float lengthx, float lengthy, int width, int height)
        {
            _buffer = new float[width, height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    _buffer[x, y] = image[x * lengthx / width + startx, y * lengthy / height + starty];
        }

        public float this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= _buffer.GetLength(0) || y < 0 || y >= _buffer.GetLength(1))
                    return 0.0f;
                return _buffer[x, y];
            }
        }

        public int Width { get { return _buffer.GetLength(0); } }
        public int Height { get { return _buffer.GetLength(1); } }
    }
}
