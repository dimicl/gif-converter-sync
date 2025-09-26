using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageGIFConverter
{
    public class GIFService
    {
        //method 
        public void CreateAnimatedGIF(string filePath, Stream outputStream, int frameCount, int delay)
        {
            using var image = Image.Load<Rgba32>(filePath);
            var gifEncoder = new GifEncoder();

            var frames = new List<ImageFrame<Rgba32>>();
            for (int i = 0; i < frameCount; i++)
            {
                var cloned = image.Clone();
                cloned.Mutate(ctx => ctx.Hue((float)(i * 36)));
                var frame = cloned.Frames.RootFrame;
                frame.Metadata.GetGifMetadata().FrameDelay = delay;
                frames.Add(frame);
            }

            using var gif = new Image<Rgba32>(image.Width, image.Height);
            foreach (var f in frames)
                gif.Frames.AddFrame(f);

            gif.Frames.RemoveFrame(0); // ukloni prazni frame
            gif.Save(outputStream, gifEncoder);

        }
    }
}
