using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class ImageService
    {
        public static Task<Image> LoadImageAsync(string imageName)
        {
            return Task.Run(() =>
            {
                string imagePath = $"..\\..\\assets\\{imageName}.jpg";

                Image image;
                try
                {
                    image = Image.FromFile(imagePath);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Couldn't load jpg image:\n {e}");
                    return null;
                }

                return image;
            });
        }
        public static Task<byte[]> ConvertImageToPngAsync(Image image)
        {
            return Task.Run(() =>
            {
                using (MemoryStream buffer = new MemoryStream())
                {
                    try
                    {
                        image.Save(buffer, ImageFormat.Png);
                        return buffer.ToArray();
                    }
                    catch
                    {
                        return null;
                    }
                }
            });
        }
    }
}
