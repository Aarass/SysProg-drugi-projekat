using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Client
{
    internal struct Records
    {
        public List<TimeSpan> noOptimizations;
        public List<TimeSpan> threadsOn;
        public List<TimeSpan> cacheOn;
    }
    internal class Program
    {
        static string[] images =
        {
            "camera.jpg",
            "church.jpg",
            "mountain.jpg",
            "cube.jpg",
            "tiger.jpg",
            "plant.jpg",
        };

        static List<ManualResetEvent> events;
        static void Main(string[] args)
        {
            Records allRecords = new Records()
            {
                noOptimizations = new List<TimeSpan>(),
                threadsOn = new List<TimeSpan>(),
                cacheOn = new List<TimeSpan>(),
            };

            StartServer();

            ChangeMode("cache_off");
            ChangeMode("cache_clear");
            ChangeMode("threads_off");
            events = new List<ManualResetEvent>();
            RequestSameImage("camera.jpg", 10, allRecords.noOptimizations);

            Console.WriteLine("No threads, no cache");
            foreach (var e in events)
            {
                e.WaitOne();
            }
            PrintList(allRecords.noOptimizations);
            PrintStatistics(allRecords.noOptimizations);

            ChangeMode("threads_on");
            events = new List<ManualResetEvent>();
            RequestSameImage("camera.jpg", 10, allRecords.threadsOn);

            Console.WriteLine("Threads, no cache");
            foreach (var e in events)
            {
                e.WaitOne();
            }
            PrintList(allRecords.threadsOn);
            PrintStatistics(allRecords.threadsOn);

            ChangeMode("cache_on");
            ChangeMode("cache_clear");
            events = new List<ManualResetEvent>();
            RequestSameImage("camera.jpg", 10, allRecords.cacheOn);

            Console.WriteLine("Threads, Cache");
            foreach (var e in events)
            {
                e.WaitOne();
            }
            PrintList(allRecords.cacheOn);
            PrintStatistics(allRecords.cacheOn);


            Console.ReadKey();
        }

        static void PrintStatistics(List<TimeSpan> records)
        {
            TimeSpan avg = TimeSpan.FromSeconds(records.Select(s => s.TotalSeconds).Average());
            TimeSpan max = TimeSpan.FromSeconds(records.Select(s => s.TotalSeconds).Max());
            Console.WriteLine($"Average: {avg}, max: {max}");
            Console.WriteLine("----------------------------------------------\n\n");
        }
        static void PrintList(List<TimeSpan> records)
        {
            Console.WriteLine("----------------------------------------------");
            foreach (var record in records)
            {
               Console.WriteLine(record); 
            }
            Console.WriteLine("----------------------------------------------");
        }
        private static readonly object Lock = new object();
        public static void RequestSameImage(string imageName, int n, List<TimeSpan> records)
        {
            for (int i = 0; i < n; i++)
            {
                var e = new ManualResetEvent(false);
                events.Add(e);
                ThreadPool.QueueUserWorkItem(state =>
                {
                    RequestImage(imageName, out var elapsedTime);

                    lock (Lock)
                    {
                        e.Set();
                        records.Add(elapsedTime);
                    }
                });
                if (i == 0)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public static bool RequestImage(string imageName, out TimeSpan elapsedTime)
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            var request = new HttpClient().GetAsync($"http://localhost:8080/{imageName}");
            request.Wait();
            sw.Stop();

            elapsedTime = sw.Elapsed;
            if (request.Result.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Request failed: {request.Result}");
                return false;
            }
            return true;
        }
        public static void ChangeMode(string command)
        {
            var request = new HttpClient().GetAsync($"http://localhost:8081/{command}");
            request.Wait();
            if (request.Result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(request.Result.ToString());
            }
        }

        public static void StartServer()
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

            Process.Start(startInfo);
        }
    }
}
