using System;
using System.Collections.Generic;
using System.Linq;

namespace StellarStack
{
    class StackedImage : IImage
    {
        private readonly IImage[] _images;

        public StackedImage(IImage[] images)
        {
            _images = images;
        }

        public float this[float x, float y]
        {
            get
            {
                var pixels = new List<float>(_images.Length);
                pixels.AddRange(_images.Select(t => t[x, y]));

                pixels.Sort();

                var median = pixels[pixels.Count / 2];
                float mean, stdev;
                pixels.MeanStdev(out mean, out stdev);

                const float kappa = 2.0f;
                for (var i = 0; i < pixels.Count; i++)
                    if (Math.Abs((pixels[i] - median) / stdev) > kappa)
                        pixels[i] = median;

                return pixels.Sum() / pixels.Count;
            }
        }
    }
}
