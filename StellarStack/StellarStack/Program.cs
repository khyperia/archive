using System;
using System.IO;
using System.Linq;

namespace StellarStack
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: StellarStack.exe [directory containing *.tiff files]");
                return;
            }
            var files = Directory.EnumerateFiles(args[0], "*.tiff");
            Console.WriteLine("Loading and calibrating images");
            var images = files.Select(s => new FileImage(s));
            IIntImage firstImage = null;
            var calibrated = AlignedImage.Align(null, images.ExtractFirst(f => firstImage = f)).ToArray();
            Console.WriteLine("Stacking");
            var stack = new StackedImage(calibrated);
            const int drizzle = 2;
            var rasterized = new ImageRasterizer(stack, 0, 0,
                firstImage.Width, firstImage.Height,
                firstImage.Width * drizzle, firstImage.Height * drizzle);
            Console.WriteLine("Saving");

            const string basename = "Norm";
            var filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "image" + basename + ".tiff");
            rasterized.Save(filename);
            var filenameStretched = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "image" + basename + "Stretched.tiff");
            new StretchedImage(rasterized).Save(filenameStretched);
            var filenameFlatHist = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "image" + basename + "FlatHist.tiff");
            new FlatHistogramImage(rasterized).Save(filenameFlatHist);
        }
    }
}
