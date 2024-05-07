using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Threading;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int worker_threads, completion_ports_threads;
            ThreadPool.GetAvailableThreads(out worker_threads, out completion_ports_threads);
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();

            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();
                string image_name = context.Request.RawUrl;
                Console.WriteLine(image_name);
            }

            Image png_image = ConvertJpgToPng(Console.ReadLine());
        }

        public static Image ConvertJpgToPng(string image_name)
        {
            string path = $"..\\..\\assets{image_name}";
            Console.WriteLine(path);
            string new_path = $"{path.Substring(0, path.Length - 3)}png";
            Console.WriteLine(new_path);

            Image jpg_image = Bitmap.FromFile(path);

            jpg_image.Save(new_path, ImageFormat.Png);
            return Bitmap.FromFile(new_path);
        }
    }
}
