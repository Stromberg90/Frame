using Frame;
using System;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Diagnostics;
using System.Collections.Generic;

namespace Benchmark {
    static class Program {
        static readonly string[] arguments = { @"E:\Dropbox\Resources\Inspiration\32814_DevMatt.jpg" };

        [STAThread]
        static void Main(string[] args) {
            ProfileOptimization.SetProfileRoot(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location));
            ProfileOptimization.StartProfile("Startup.Profile");

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            var manager = new SingleAppMangager();
            manager.Run(arguments);
            stopWatch.Stop();

            Console.WriteLine($"Time: {stopWatch.Elapsed.TotalSeconds}");

            var output_file = File.AppendText("baseline.txt");
            output_file.WriteLine($"Time: {stopWatch.Elapsed.TotalSeconds}");
            output_file.Close();
        }
    }
}