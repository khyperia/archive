using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Orbital
{
    static class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("Usage #1: Orbital");
            Console.WriteLine("Usage #2: Orbital [Minimum onscreen stars number]");
        }

        static void Main(string[] args)
        {
            var display = new Display(new[] { new Star(0.01, new Vector2(0, 0), new Vector2(0, 0)) }, 0.0001);
            switch (args.Length)
            {
                case 0:
                    break;
                case 1:
                    int result;
                    if (int.TryParse(args[0], out result))
                        display.MinStars = result;
                    else
                    {
                        PrintUsage();
                        return;
                    }
                    break;
                default:
                    PrintUsage();
                    return;
            }
            Application.Run(display);
        }
    }

    class Display : Form
    {
        private static readonly Random rand = new Random();
        private double _spawnStarMass;
        private Star[] _stars;
        private float[,] _timeBufferRed;
        private float[,] _timeBufferGreen;
        private float[,] _timeBufferBlue;
        private int[] _backbuffer;
        private Bitmap _backbufferBitmap;

        public int MinStars { get; internal set; }

        public Display(Star[] stars, double spawnStarMass)
        {
            _stars = stars;
            _spawnStarMass = spawnStarMass;
        }

        protected override void OnClick(EventArgs e)
        {
            var position = new Vector2((double)(MousePosition.X - Location.X) / Size.Width, (double)(MousePosition.Y - Location.Y) / Size.Height) * 2 - new Vector2(1, 1);
            GenAddStar(position);
            base.OnClick(e);
        }

        private void GenAddStar()
        {
            var theta = rand.NextDouble() * Math.PI * 2;
            var dist = rand.NextDouble() * 0.6 + 0.2;
            var position = new Vector2(Math.Cos(theta) * dist, Math.Sin(theta) * dist);
            GenAddStar(position);
        }

        private void GenAddStar(Vector2 position)
        {
            var centerOfMass = Star.CenterOfMass(_stars);
            var acceleration = Star.AccelerationAt(_stars, position);
            var distance = centerOfMass - position;
            var speed = Math.Sqrt(distance.Length) * Math.Sqrt(acceleration.Length) / 2;
            var theta = distance.Theta + Math.PI / 2;
            var velocity = new Vector2(Math.Cos(theta), Math.Sin(theta)) * speed;
            var star = new Star(_spawnStarMass, position, velocity);
            AddStar(star);
        }

        private void AddStar(Star star)
        {
            Console.WriteLine("Added star");
            var newStars = new Star[_stars.Length + 1];
            Array.Copy(_stars, newStars, _stars.Length);
            newStars[_stars.Length] = star;
            _stars = newStars;
        }

        private void RemoveStar(int index)
        {
            Console.WriteLine("Removed star");
            var newStars = new Star[_stars.Length - 1];
            Array.Copy(_stars, 0, newStars, 0, index);
            Array.Copy(_stars, index + 1, newStars, index, newStars.Length - index);
            _stars = newStars;
            CheckNewStars();
        }

        private void CheckNewStars()
        {
            while (_stars.Length <= MinStars)
                GenAddStar();
        }

        protected override void OnResize(EventArgs e)
        {
            ClientSize = new Size(ClientSize.Width, ClientSize.Width);
            base.OnResize(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (WindowState != FormWindowState.Normal)
                return;

            var size = ClientSize;

            if (_timeBufferRed == null || _timeBufferRed.GetLength(0) != size.Width || _timeBufferRed.GetLength(1) != size.Height)
                _timeBufferRed = new float[size.Width, size.Height];

            if (_timeBufferGreen == null || _timeBufferGreen.GetLength(0) != size.Width || _timeBufferGreen.GetLength(1) != size.Height)
                _timeBufferGreen = new float[size.Width, size.Height];

            if (_timeBufferBlue == null || _timeBufferBlue.GetLength(0) != size.Width || _timeBufferBlue.GetLength(1) != size.Height)
                _timeBufferBlue = new float[size.Width, size.Height];

            CheckNewStars();

            for (var j = 0; j < _stars.Length; j++)
            {
                if (_stars[j].Position.LengthSquared > 2 * 2)
                    RemoveStar(j);
            }

            Star.Simulate(_stars, 0.01);

            if (_backbufferBitmap == null || _backbufferBitmap.Width != size.Width ||
                _backbufferBitmap.Height != size.Height)
            {
                if (_backbufferBitmap != null)
                    _backbufferBitmap.Dispose();
                _backbufferBitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppRgb);
            }
            if (_backbuffer == null || _backbuffer.Length != size.Width * size.Height)
                _backbuffer = new int[size.Width * size.Height];

            for (var i = 0; i < _stars.Length; i++)
            {
                var energy = 0.0;
                for (int j = 0; j < _stars.Length; j++)
                {
                    if (i == j)
                        continue;
                    energy += _stars[i].Energy(_stars[j]);
                }
                var hue = (float)(energy * 100000);
                var saturation = 0.5f;

                var dist = _stars[i].Position;
                dist *= 0.5;
                dist += new Vector2(0.5, 0.5);
                dist = new Vector2(dist.X * size.Width, dist.Y * size.Height);
                const int starBoxSize = 8;
                var x = (int)(dist.X) - starBoxSize;
                var y = (int)(dist.Y) - starBoxSize;
                for (var ix = x; ix < x + starBoxSize * 2 + 1; ix++)
                {
                    for (var iy = y; iy < y + starBoxSize * 2 + 1; iy++)
                    {
                        if (ix >= 0 && ix < size.Width && iy >= 0 && iy < size.Height)
                        {
                            var length = (float)(new Vector2(ix, iy) - dist).LengthSquared;
                            if (length < 0.1f)
                                length = 0.1f;

                            float red, green, blue;
                            Ext.HsvToRgb(hue, saturation, 0.001f / length, out red, out green, out blue);
                            _timeBufferRed[ix, iy] += red;
                            _timeBufferGreen[ix, iy] += green;
                            _timeBufferBlue[ix, iy] += blue;
                        }
                    }
                }
            }

            Parallel.For(0, size.Width * size.Height, i =>
            {
                var x = i % size.Width;
                var y = i / size.Width;
                var red = (byte)Math.Min((_timeBufferRed[x, y] *= 0.99f) * 255, 255);
                var green = (byte)Math.Min((_timeBufferGreen[x, y] *= 0.99f) * 255, 255);
                var blue = (byte)Math.Min((_timeBufferBlue[x, y] *= 0.99f) * 255, 255);
                _backbuffer[y * size.Width + x] = red << 16 | green << 8 | blue;
            });

            var locked = _backbufferBitmap.LockBits(new Rectangle(0, 0, size.Width, size.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            Marshal.Copy(_backbuffer, 0, locked.Scan0, _backbuffer.Length);
            _backbufferBitmap.UnlockBits(locked);

            e.Graphics.DrawImageUnscaled(_backbufferBitmap, 0, 0);

            Invalidate();
        }
    }

    static class Ext
    {
        /// <summary>
        /// Convert HSV to RGB
        /// h is from 0-360
        /// s,v values are 0-1
        /// r,g,b values are 0-255
        /// Based upon http://ilab.usc.edu/wiki/index.php/HSV_And_H2SV_Color_Space#HSV_Transformation_C_.2F_C.2B.2B_Code_2
        /// </summary>
        public static void HsvToRgb(float h, float S, float V, out float r, out float g, out float b)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            float H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            float R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                float hf = H / 60.0f;
                int i = (int)Math.Floor(hf);
                float f = hf - i;
                float pv = V * (1 - S);
                float qv = V * (1 - S * f);
                float tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp(R * 255.0f);
            g = Clamp(G * 255.0f);
            b = Clamp(B * 255.0f);
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        private static float Clamp(float i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
