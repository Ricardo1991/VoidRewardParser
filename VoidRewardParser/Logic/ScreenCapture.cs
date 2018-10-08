using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Tesseract;
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace VoidRewardParser.Logic
{
    public static class ScreenCapture
    {
        private static bool? windows10 = null;
        private static TesseractEngine ocrEngine = null;

        public enum FormatImage
        { PNG, TIFF };

        public static async Task<string> ParseTextAsync()
        {
            if (windows10 == null)
                windows10 = Utilities.IsWindows10OrGreater();

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    if (windows10.Value)
                    {
                        await Task.Run(() => SaveScreenshot(memoryStream, FormatImage.PNG));
                        return await RunOcr(memoryStream);
                    }
                    else
                    {
                        await Task.Run(() => SaveScreenshot(memoryStream, FormatImage.TIFF));
                        return await Task.Run(() => RunTesseractOcr(memoryStream));
                    }
                }
            }
            finally
            {
                GC.Collect(0);
            }
        }

        public static void SaveScreenshot(Stream stream, FormatImage format)
        {
            System.Diagnostics.Process p = Warframe.GetProcess();
            if (p == null)
                throw new Exception();

            IntPtr ptr = p.MainWindowHandle;
            User32.Rect rect = new User32.Rect();
            User32.GetWindowRect(ptr, ref rect);

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (height == 0 || width == 0) return;

            using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));
                    graphics.Save();
                    graphics.Dispose();

                    switch (format)
                    {
                        case FormatImage.TIFF:
                            MakeGrayscale3(bitmap).Save(stream, System.Drawing.Imaging.ImageFormat.Tiff);
                            break;

                        case FormatImage.PNG:
                        default:
                            MakeGrayscale3(bitmap).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
#if DEBUG
                            //using (FileStream file = new FileStream("shot_" + DateTime.Now.ToString("HH_mm_ss") + ".png", FileMode.Create, FileAccess.Write))
                            //{
                            //    ((MemoryStream)stream).WriteTo(file);
                            //}
#endif
                            break;
                    }
                }
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

        private static async Task<string> RunOcr(MemoryStream memoryStream)
        {
            if (memoryStream == null || memoryStream.Length == 0) return "";

            using (var memoryRandomAccessStream = new InMemoryRandomAccessStream())
            {
                await memoryRandomAccessStream.WriteAsync(memoryStream.ToArray().AsBuffer());
                OcrEngine engine = null;

                if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["LanguageCode"]))
                {
                    engine = OcrEngine.TryCreateFromLanguage(new Language(ConfigurationManager.AppSettings["LanguageCode"]));
                }
                if (engine == null)
                {
                    engine = OcrEngine.TryCreateFromUserProfileLanguages();
                }
                var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(memoryRandomAccessStream);
                OcrResult result = await engine.RecognizeAsync(await decoder.GetSoftwareBitmapAsync());
                return result.Text;
            }
        }

        private static string RunTesseractOcr(MemoryStream memoryStream)
        {
            if (memoryStream == null || memoryStream.Length == 0) return "";

            if (ocrEngine == null)
            {
                var ENGLISH_LANGUAGE = @"eng";
                ocrEngine = new TesseractEngine(@".\tessdata", ENGLISH_LANGUAGE);
                ocrEngine.SetVariable("load_system_dawg", false);
                ocrEngine.SetVariable("load_freq_dawg", false);
            }

            using (var imageWithText = Pix.LoadTiffFromMemory(memoryStream.ToArray()))
            {
                using (var page = ocrEngine.Process(imageWithText))
                {
                    return page.GetText().Replace('\n', ' ');
                }
            }
        }

        private static class User32
        {
            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

            public struct Rect
            {
                public int Left { get; set; }
                public int Top { get; set; }
                public int Right { get; set; }
                public int Bottom { get; set; }
            }
        }
    }
}