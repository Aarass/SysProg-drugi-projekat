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
            //int worker_threads, completion_ports_threads;
            //ThreadPool.GetAvailableThreads(out worker_threads, out completion_ports_threads);
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();

            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();

                ThreadPool.QueueUserWorkItem(state =>
                {
                    HttpListenerRequest Request = context.Request;
                    HttpListenerResponse Response = context.Response;

                    string uri = context.Request.RawUrl;
                    if (uri == "/favicon.ico") return;

                    string path = $"..\\..\\assets{uri}";
                    string newPath = $"{path.Substring(0, path.Length - 3)}png";

                    Console.WriteLine(path);
                    Console.WriteLine(newPath);

                    bool alreadyConverted = File.Exists(newPath);
                    if (!alreadyConverted)
                    {
                        Console.WriteLine("Converting image...");
                        Image jpgImage;
                        try
                        {
                            jpgImage = Image.FromFile(path);
                        }
                        catch (Exception)
                        {
                            SetErrorResponse(context, "Couldn't load jpg image");
                            return;
                        }

                        try
                        {
                            jpgImage.Save(newPath, ImageFormat.Png);
                        }
                        catch (Exception)
                        {
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
                        SetErrorResponse(context, "Couldn't load png image");
                        return;
                    }

                    try
                    {
                        Response.ContentType = "image/png";
                        Response.ContentLength64 = pngImage.Length;
                        Response.OutputStream.Write(pngImage, 0, pngImage.Length);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Couldn't make response: {e.ToString()}");
                    }
                });
            }
        }

        public static void SetErrorResponse(HttpListenerContext context, string error)
        {
            Console.WriteLine(error);
            HttpListenerResponse Response = context.Response;

            byte[] html = Encoding.ASCII.GetBytes($"<p>{error}</p>");

            Response.ContentType = "text/html";
            Response.ContentLength64 = html.Length;
            Response.StatusCode = 400;
            Response.OutputStream.Write(html, 0, html.Length);
        }
    }
}
