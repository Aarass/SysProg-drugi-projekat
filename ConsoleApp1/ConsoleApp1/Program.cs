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
            WebServer server = new WebServer(8080);
            server.RunModeChangeListener();
            server.Run();
        }
    }
}
