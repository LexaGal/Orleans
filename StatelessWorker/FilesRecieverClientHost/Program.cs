using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GrainsLib;
using GrainsLib.Grains;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Logging;
using Orleans.Runtime;

namespace FilesRecieverClientHost
{
    public class Program
    {
        static int Main(string[] args)
        {
            return RunMainAsync().GetAwaiter().GetResult();
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (var client = await StartClientWithRetries())
                {
                    await ProcessorDirFiles(client);
                    Console.ReadKey();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }

        private static async Task<IClusterClient> StartClientWithRetries(int initializeAttemptsBeforeFailing = 5)
        {
            var attempt = 0;
            IClusterClient client;
            while (true)
            {
                try
                {
                    client = new ClientBuilder()
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = Consts.ClusterId;
                            options.ServiceId = Consts.ServiceId;
                        })
                        .ConfigureApplicationParts(parts =>
                            parts.AddApplicationPart(typeof(IFilesReciever).Assembly).WithReferences())
                        .ConfigureLogging(logging => logging.AddFile("log.txt"))
                        .AddSimpleMessageStreamProvider(Consts.FilesStreamProvider)
                        .Build();

                    await client.Connect();
                    Console.WriteLine("Client successfully connected to silo host");
                    break;
                }
                catch (SiloUnavailableException)
                {
                    attempt++;
                    Console.WriteLine($"Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    if (attempt > initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(4));
                }
            }

            return client;
        }

        private static async Task ProcessorDirFiles(IClusterClient client)
        {
            var currentDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            if (currentDir == null) return;

            var outDir = Path.Combine(currentDir, "out");
            var settingsDir = Path.Combine(currentDir, "settings");
            Directory.CreateDirectory(outDir);
            Directory.CreateDirectory(settingsDir);

            var filesReciever = client.GetGrain<IFilesReciever>(Consts.FilesRecGuid);
            await filesReciever.SetupFilesReciever(outDir, settingsDir);
            await filesReciever.StartProcessFiles();
        }
    }
}
