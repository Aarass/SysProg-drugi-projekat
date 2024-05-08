using System;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5050/");
            listener.Start();

            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();

                ThreadPool.QueueUserWorkItem(state =>
                {
                    HttpListenerRequest Request = context.Request;
                    HttpListenerResponse Response = context.Response;

                    if (Request.HttpMethod != "GET")
                    {
                        Console.WriteLine("Unsupported request method");
                        SetErrorResponse(context, "Unsupported request method");
                        return;
                    }

                    string uri = context.Request.RawUrl;
                    if (uri == "/favicon.ico") return;

                    string path = $"..\\..\\assets{uri}";
                    string newPath = $"{path.Substring(0, path.Length - 3)}png";

                    bool alreadyConverted = File.Exists(newPath);
                    if (!alreadyConverted)
                    {
                        try
                        {
                            ConvertJpgImageToPng(path, newPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Converting image failed:\n {ex}");
                            SetErrorResponse(context, "Couldn't convert image");
                            return;
                        }
                    }

                    byte[] pngImage;
                    try
                    {
                         pngImage = File.ReadAllBytes(newPath);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Couldn't load converted image");
                        SetErrorResponse(context, "Internal error");
                        return;
                    }

                    try
                    {
                        Response.OutputStream.Write(pngImage, 0, pngImage.Length);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Couldn't write image to response:\n {e.ToString()}");
                    }

                    Response.ContentType = "image/png";
                    Response.ContentLength64 = pngImage.Length;

                    Console.WriteLine("Image sent");
                });
            }
        }

        public static void ConvertJpgImageToPng(string path, string newPath)
        {
            Console.WriteLine("Converting image...");

            Image jpgImage;
            try
            {
                jpgImage = Image.FromFile(path);
            }
            catch (Exception e)
            {
                throw new Exception($"Couldn't load jpg image:\n {e.ToString()}");
            }

            try
            {
                jpgImage.Save(newPath, ImageFormat.Png);
            }
            catch (Exception e)
            {
                throw new Exception($"Couldn't convert image:\n {e.ToString()}");
            }

            Console.WriteLine("Converting done");
        }
        public static void SetErrorResponse(HttpListenerContext context, string error)
        {
            HttpListenerResponse Response = context.Response;

            byte[] html = Encoding.ASCII.GetBytes($"<p>{error}</p>");

            Response.ContentType = "text/html";
            Response.ContentLength64 = html.Length;
            Response.StatusCode = 400;
            Response.OutputStream.Write(html, 0, html.Length);
        }
    }
}
