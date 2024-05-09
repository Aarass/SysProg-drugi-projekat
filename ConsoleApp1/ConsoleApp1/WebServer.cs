using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace ConsoleApp1
{
    internal class WebServer
    {
        private readonly HttpListener _mListener;
        private readonly ImageCache _cache;
        public WebServer(int port)
        {
            _mListener = new HttpListener();
            _mListener.Prefixes.Add($"http://localhost:{port}/");

            _cache = new ImageCache(5, new TimeSpan(0, 0, 0, 10));
        }

        public void Run()
        {
            _mListener.Start();
            while (_mListener.IsListening)
            {
                HttpListenerContext context = _mListener.GetContext();

                ThreadPool.QueueUserWorkItem(state =>
                {
                    var request = context.Request;
                    var response = context.Response;

                    if (request.HttpMethod != "GET")
                    {
                        Console.WriteLine("Unsupported request method");
                        SendErrorResponse(response, "Unsupported request method");
                        return;
                    }

                    string url = request.RawUrl.Substring(1);
                    string[] urlParts = url.Split('.');
                    if (url.Length < 6 || urlParts.Length != 2)
                    {
                        Console.WriteLine("Bad image name");
                        SendErrorResponse(response, "Bad image name");
                        return;
                    }

                    string imageName = urlParts[0];
                    string imageExtension = urlParts[1];

                    if (imageName == "favicon") return;

                    Console.WriteLine($"name:{imageName}, extension:{imageExtension}");

                    if (imageExtension != "jpg")
                    {
                        Console.WriteLine("Unsupported image type");
                        SendErrorResponse(response, "Unsupported image type");
                        return;
                    }

                    if (_cache.TryGetImage(imageName, out var pngImage))
                    {
                        Console.WriteLine("Cache hit");
                    }
                    else
                    {
                        Console.WriteLine("Cache miss, converting...");
                        Image jpgImage = LoadImage(imageName);
                        if (jpgImage == null)
                        {
                            Console.WriteLine("Couldn't load image");
                            SendErrorResponse(response, "Couldn't load image");
                            return;
                        }

                        pngImage = ImageService.ConvertImageToPng(jpgImage);
                        if (pngImage == null)
                        {
                            Console.WriteLine("Couldn't convert image");
                            SendErrorResponse(response, "Couldn't convert image");
                            return;
                        }

                        _cache.AddImage(imageName, pngImage);
                    }

                    response.ContentType = "image/png";
                    response.ContentLength64 = pngImage.Length;
                    try
                    {
                        response.OutputStream.Write(pngImage.GetBuffer(), 0, (int)pngImage.Length);
                        response.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Couldn't write image to response:\n {e}");
                        SendErrorResponse(response,"Couldn't write image to response");
                        return;
                    }

                    Console.WriteLine("Image sent");
                });
            }
        }

        private void SendErrorResponse(HttpListenerResponse response, string error)
        {
            byte[] html = Encoding.ASCII.GetBytes($"<p>{error}</p>");

            response.ContentType = "text/html";
            response.ContentLength64 = html.Length;
            response.StatusCode = 400;
            response.OutputStream.Write(html, 0, html.Length);
            response.Close();
        }

        private Image LoadImage(string imageName)
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
        }
    }
}
