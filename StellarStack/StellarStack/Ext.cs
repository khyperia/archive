using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StellarStack
{
    static class Ext
    {
        public static void Save(this IIntImage image, string filename)
        {
            var max = Enumerable.Range(0, image.Width * image.Height).Max(i => image[i % image.Width, i / image.Width]);
            var shorts = Enumerable.Range(0, image.Width * image.Height).Select(i => (ushort)(image[i % image.Width, i / image.Width] / max * ushort.MaxValue)).ToArray();
            var frame = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Gray16, null, shorts, image.Width * sizeof(ushort));

            var encoder = new TiffBitmapEncoder
            {
                Compression = TiffCompressOption.None
            };
            encoder.Frames.Add(BitmapFrame.Create(frame));
            using (var stream = File.OpenWrite(filename))
                encoder.Save(stream);
        }

        public static IEnumerable<T> ExtractFirst<T>(this IEnumerable<T> enumerable, Action<T> onFirst)
        {
            var first = true;
            foreach (var item in enumerable)
            {
                if (first)
                {
                    onFirst(item);
                    first = false;
                }
                yield return item;
            }
        }

        public static void MeanStdev(this IReadOnlyCollection<float> floats, out float mean, out float stdev)
        {
            mean = floats.Sum() / floats.Count;
            var meanTemp = mean;
            stdev = (float)Math.Sqrt(floats.Select(f => (f - meanTemp) * (f - meanTemp)).Sum() / floats.Count);
        }

        public static float[] SortIntensities(this IIntImage image)
        {
            var histogram = new float[image.Width * image.Height];
            for (var y = 0; y < image.Height; y++)
                for (var x = 0; x < image.Width; x++)
                    histogram[y * image.Width + x] = image[x, y];
            Array.Sort(histogram);
            return histogram;
        }

        public static float DefaultGet(this IIntImage intImage, float x, float y)
        {
            return intImage[(int)x, (int)y];
            //var intx = (int)Math.Floor(x);
            //var inty = (int)Math.Floor(y);
            //var fltx = x - intx;
            //var flty = y - inty;
            //return intImage[intx, inty] * (1 - fltx) * (1 - flty) +
            //       intImage[intx + 1, inty] * fltx * (1 - flty) +
            //       intImage[intx, inty + 1] * (1 - fltx) * flty +
            //       intImage[intx + 1, inty + 1] * fltx * flty;
        }

        public static float GaussianMean(float y0, float y1, float y2)
        {
            if (y0 <= 0)
                y0 = 1e-10f;
            if (y1 <= 0)
                y1 = 1e-10f;
            if (y2 <= 0)
                y2 = 1e-10f;

            var l0 = (float)Math.Log(y0);
            var l1 = (float)Math.Log(y1);
            var l2 = (float)Math.Log(y2);
            var a = l0 - l1 + (l2 - l0) / 2;
            var b = l1 - l0 + a;
            return -b / (2 * a);
        }
    }
}