using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class WebServer
    {
        private readonly HttpListener _listener;
        private readonly HttpListener _configListener;
        private readonly ImageCache _cache;
        private bool _shouldUseCache = true;
        private bool _shouldRunConcurrently = true;
        public WebServer(int port)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");

            _configListener = new HttpListener();
            _configListener.Prefixes.Add($"http://localhost:{port + 1}/");

            _cache = new ImageCache(3, new TimeSpan(0, 1, 0, 10));
        }

        public void RunConfigListener()
        {
            _configListener.Start();
            new Thread(() =>
            {
                while (_configListener.IsListening)
                {
                    var context = _configListener.GetContext();
                    switch (context.Request.RawUrl)
                    {
                        case "/concurrency_off":
                            Console.WriteLine("Will be processing requests synchronously");
                            _shouldRunConcurrently = false;
                            break;
                        case "/concurrency_on":
                            Console.WriteLine("Will be processing requests concurrently");
                            _shouldRunConcurrently = true;
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
                            Console.WriteLine("Cache cleared");
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
            }).Start();
        }
        public void Run()
        {
            _listener.Start();
            while (_listener.IsListening)
            {
                var context = _listener.GetContext();

                if (_shouldRunConcurrently)
                {
                    Task.Run(() =>
                    {
                        _ = HandleConnection(context);
                    });
                }
                else
                {
                    HandleConnection(context).Wait();
                }
            }
        }

        private async Task HandleConnection(HttpListenerContext context)
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

            if (_shouldUseCache)
                this._cache.Print("Cache data before request: ");

            byte[] pngImage;
            if (_shouldUseCache && _cache.TryGetImage(imageName, out pngImage))
            {
                Console.WriteLine("Cache hit");
            }
            else
            {
                Console.WriteLine("Cache miss, converting...");
                Image jpgImage = await ImageService.LoadImageAsync(imageName);
                if (jpgImage == null)
                {
                    Console.WriteLine("Couldn't load image");
                    SendErrorResponse(response, "Couldn't load image");
                    return;
                }

                pngImage = await ImageService.ConvertImageToPngAsync(jpgImage);
                jpgImage.Dispose();
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

            if (_shouldUseCache)
                this._cache.Print("Cache data after response: ");



            response.ContentType = "image/png";
            response.ContentLength64 = pngImage.Length;
            try
            {
                await response.OutputStream.WriteAsync(pngImage, 0, pngImage.Length);
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
    }
}
