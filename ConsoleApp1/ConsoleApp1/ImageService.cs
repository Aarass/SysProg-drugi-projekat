using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class ImageService
    {
        public static MemoryStream ConvertImageToPng(Image image)
        {
            MemoryStream buffer = new MemoryStream();
            try
            {
                image.Save(buffer, ImageFormat.Png);
            }
            catch
            {
                return null;
            }

            return buffer;
        }
    }
}
