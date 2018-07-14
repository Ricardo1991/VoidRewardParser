using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Configuration;

namespace VoidRewardParser.Logic
{
    public class ScreenCapture
    {
        public static async Task<string> ParseTextAsync()
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                using (var memoryRandomAccessStream = new InMemoryRandomAccessStream())
                {
                    await Task.Run(() => SaveScreenshot(memoryStream));
                    await memoryRandomAccessStream.WriteAsync(memoryStream.ToArray().AsBuffer());
                    return await RunOcr(memoryRandomAccessStream);
                }
            }
            finally
            {
                GC.Collect(0);
            }
        }

        public static void SaveScreenshot(Stream stream)
        {
            int width = (int)SystemParameters.FullPrimaryScreenWidth;
            int height = (int)SystemParameters.FullPrimaryScreenHeight;
            Bitmap bitmap = MakeGrayscale3(new Bitmap(width, height, PixelFormat.Format24bppRgb));
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                graphics.Save();
                bitmap.Save(stream, ImageFormat.Png);
            }
        }

        public static Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
                    new float[] {.3f, .3f, .3f, 0, 0},
                    new float[] {.59f, .59f, .59f, 0, 0},
                    new float[] {.11f, .11f, .11f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

        private static async Task<string> RunOcr(IRandomAccessStream stream)
        {
            OcrEngine engine = null;
            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["LanguageCode"]))
            {
                engine = OcrEngine.TryCreateFromLanguage(new Language(ConfigurationManager.AppSettings["LanguageCode"]));
            }
            if (engine == null)
            {
                engine = OcrEngine.TryCreateFromUserProfileLanguages();
            }
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
            var result = await engine.RecognizeAsync(await decoder.GetSoftwareBitmapAsync());
            return result.Text;
        }
    }
}