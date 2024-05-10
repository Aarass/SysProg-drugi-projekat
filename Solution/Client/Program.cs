using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public List<TimeSpan> NoOptimizations;
        public List<TimeSpan> ThreadsOn;
        public List<TimeSpan> ThreadsAndCacheOn;
    }
    internal class Program
    {
        static readonly string[] Images =
        {
            "camera.jpg",
            "church.jpg",
            "mountain.jpg",
            "cube.jpg",
            "tiger.jpg",
            "plant.jpg",
        };

        static void Main(string[] args)
        {
            StartServer();

            Records allRecords = new Records()
            {
                NoOptimizations = new List<TimeSpan>(),
                ThreadsOn = new List<TimeSpan>(),
                ThreadsAndCacheOn = new List<TimeSpan>(),
            };

            var chosenImage = Images[4];

            ChangeMode("cache_off", "cache_clear", "threads_off");
            GetSameImage(chosenImage, 10, allRecords.NoOptimizations);
            PrintList(allRecords.NoOptimizations);
            PrintStatistics(allRecords.NoOptimizations);

            ChangeMode("threads_on");
            GetSameImage(chosenImage, 10, allRecords.ThreadsOn);
            PrintList(allRecords.ThreadsOn);
            PrintStatistics(allRecords.ThreadsOn);

            ChangeMode("cache_on", "cache_clear");
            GetSameImage(chosenImage, 10, allRecords.ThreadsAndCacheOn);
            PrintList(allRecords.ThreadsAndCacheOn);
            PrintStatistics(allRecords.ThreadsAndCacheOn);

            Console.ReadKey();
        }

        static void PrintStatistics(List<TimeSpan> records)
        {
            var avg = TimeSpan.FromSeconds(records.Select(s => s.TotalSeconds).Average());
            var max = TimeSpan.FromSeconds(records.Select(s => s.TotalSeconds).Max());
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
        public static void GetSameImage(string imageName, int n, List<TimeSpan> records)
        {
            Console.WriteLine("Getting images...");
            List<ManualResetEvent> events = new List<ManualResetEvent>();

            for (int i = 0; i < n; i++)
            {
                var resetEvent = new ManualResetEvent(false);
                events.Add(resetEvent);
                ThreadPool.QueueUserWorkItem(state =>
                {
                    RequestImage(imageName, out var elapsedTime);

                    lock (Lock)
                    {
                        resetEvent.Set();
                        records.Add(elapsedTime);
                    }
                });
                if (i == 0)
                {
                    resetEvent.WaitOne();
                }
            }

            foreach (var resetEvent in events)
            {
                resetEvent.WaitOne();
            }
        }

        public static bool RequestImage(string imageName, out TimeSpan elapsedTime)
        {
            var client = new HttpClient();
            var sw = new Stopwatch();

            sw.Start();
            var request = client.GetAsync($"http://localhost:8080/{imageName}");
            request.Wait();
            sw.Stop();

            client.Dispose();

            var response = request.Result;
            response.Dispose();

            var responseContent = response.ToString();
            var responseStatus = response.StatusCode;
            response.Dispose();

            elapsedTime = sw.Elapsed;
            if (responseStatus!= HttpStatusCode.OK)
            {
                Console.WriteLine($"Request failed: {responseContent}");
                return false;
            }
            return true;
        }
        public static void ChangeMode(params string[] commands)
        {
            foreach (var command in commands)
            {
                var request = new HttpClient().GetAsync($"http://localhost:8081/{command}");
                request.Wait();

                if (request.Result.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(request.Result.ToString());
                }

                Console.WriteLine($"Command: {command}");
            }
        }

        public static void StartServer()
        {
            string serverDir = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\WebServer\bin\Debug\");
            string serverPath = Path.Combine(serverDir, "WebServer.exe");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = "",
                FileName = serverPath,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            startInfo.WorkingDirectory = serverDir;

            Process.Start(startInfo);
        }
    }
}
