using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = "",
                FileName = @"C:\Users\proko\Documents\sisp\SysProg-prvi-projekat\Solution\WebServer\bin\Debug\ConsoleApp1.exe",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            startInfo.WorkingDirectory =
                @"C:\Users\proko\Documents\sisp\SysProg-prvi-projekat\Solution\WebServer\bin\Debug\";

            Process process = Process.Start(startInfo);


            while (true)
            {
                var request = new HttpClient().GetAsync($"http://localhost:8080/{Console.ReadLine()}");
                request.Wait();
                Console.WriteLine(request.Result.StatusCode);
            }

        }
    }
}
