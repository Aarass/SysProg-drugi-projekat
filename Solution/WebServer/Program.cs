using System;
using System.IO;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("The current directory is {0}", Directory.GetCurrentDirectory());
            WebServer server = new WebServer(8080);
            server.RunConfigListener();
            server.Run();
        }
    }
}
