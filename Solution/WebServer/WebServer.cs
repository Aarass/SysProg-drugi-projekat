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
        private readonly HttpListener _Listener;
        private readonly HttpListener _ModeChangeListener;
        private readonly ImageCache _cache;
        private bool _shouldUseCache = true;
        private bool _shouldUseThreads = true;
        public WebServer(int port)
        {
            _Listener = new HttpListener();
            _Listener.Prefixes.Add($"http://localhost:{port}/");

            _ModeChangeListener = new HttpListener();
            _ModeChangeListener.Prefixes.Add($"http://localhost:{port + 1}/");

            _cache = new ImageCache(3, new TimeSpan(0, 1, 0, 10));
        }

        public void RunModeChangeListener()
        {
            _ModeChangeListener.Start();
            ThreadPool.QueueUserWorkItem(sate =>
            {
                while (_ModeChangeListener.IsListening)
                {
                    var context = _ModeChangeListener.GetContext();
                    switch (context.Request.RawUrl)
                    {
                        case "/threads_off":
                            Console.WriteLine("Will not be using threads");
                            _shouldUseThreads = false;
                            break;
                        case "/threads_on":
                            Console.WriteLine("Will be using threads");
                            _shouldUseThreads = true;
                            break;
                        case "/cache_off":
                            Console.WriteLine("Will not be using cache");
                            _shouldUseCache = false;
                            break;
                        case "/cache_on":
                            Console.WriteLine("Will be using cache");
                            _shouldUseCache = true;
                            break;
                        case "/cache_clear":
                            _cache.Clear();
                            break;
                        default:
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                            continue;
                    }

                    context.Response.StatusCode = 200;
                    context.Response.Close();
                }
            });
        }
        public void Run()
        {
            _Listener.Start();
            while (_Listener.IsListening)
            {
                HttpListenerContext context = _Listener.GetContext();

                if (_shouldUseThreads)
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        HandleConnection(context);
                    });
                }
                else
                {
                    HandleConnection(context);
                }
            }
        }

        private void HandleConnection(HttpListenerContext context)
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

            MemoryStream pngImage;
            if (_shouldUseCache && _cache.TryGetImage(imageName, out pngImage))
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

                if (_shouldUseCache)
                {
                    _cache.AddImage(imageName, pngImage);
                }
            }

            response.ContentType = "image/png";
            response.ContentLength64 = pngImage.Length;
            try
            {
                response.OutputStream.Write(pngImage.GetBuffer(), 0, (int)pngImage.Length);
                response.OutputStream.Close();

                response.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Couldn't write image to response:\n {e}");
                SendErrorResponse(response, "Couldn't write image to response");
                return;
            }

            Console.WriteLine("Image sent");

        }

        private void SendErrorResponse(HttpListenerResponse response, string error)
        {
            try
            {
                byte[] html = Encoding.ASCII.GetBytes($"<p>{error}</p>");

                response.ContentType = "text/html";
                response.ContentLength64 = html.Length;
                response.StatusCode = 400;
                response.OutputStream.Write(html, 0, html.Length);
                response.Close();
            }
            catch
            {
                // ignored
            }
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
