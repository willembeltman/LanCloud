using System;
using LanCloud.Services;
using LanCloud.Domain.Application;
using LanCloud.Domain.VirtualFtp;
using System.Diagnostics;
using System.IO;
using LanCloud.Forms;

namespace LanCloud
{
    class Program
    {
        static void Main(string[] args)
        {
            var currentDirectory = Environment.CurrentDirectory;
            var logService = new LogService(currentDirectory);
            using (var logger = logService.Create())
            {
                var configService = new ConfigService(currentDirectory, logger);
                var config = configService.Load();

                using (var application = new LocalApplication(config, logger))
                {
                    //Thread.Sleep(100);
                    Console.WriteLine("Started");

                    StatusForm form = new StatusForm(application);
                    form.ShowDialog();

                    Console.WriteLine("Shutting down please wait...");

                    //var res = localApplication.RemoteApplications.First().Ping();

                    //DoTest(virtualFtpServer);

                    //DoTest2(virtualFtpServer);

                    //Console.WriteLine("Press any key to stop...");
                    //Console.ReadKey(true);
                }
            }
        }

        private static void DoTest2(VirtualFtpServer virtualFtpServer)
        {
            using (var stream = virtualFtpServer.FileOpenRead("/test.bin"))
            using (var reader = new StreamReader(stream))
            {
                Console.WriteLine(reader.ReadToEnd());
                Console.WriteLine();
            }
        }

        private static void DoTest(VirtualFtpServer virtualFtpServer)
        {
            Console.WriteLine("creating file");

            byte[] buffer = new byte[1024 * 8];
            var aantal = 128* 1024;
            var size = aantal * buffer.Length;

            Stopwatch sw = Stopwatch.StartNew();
            using (var stream = virtualFtpServer.FileOpenWriteCreate("/test.bin"))
            {
                for (long i = 0; i < aantal; i++)
                {
                    stream.Write(buffer, 0, buffer.Length);
                    //Thread.Sleep(200);
                }
            }
            var time = sw.Elapsed.TotalSeconds;
            var speed = size / time / 1024 / 1024;
            Console.WriteLine($"speed: {speed}mb/sec");
        }
    }
}


