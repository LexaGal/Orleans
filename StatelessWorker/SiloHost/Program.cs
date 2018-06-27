using System;
using System.Net;
using System.Threading.Tasks;
using GrainsLib;
using GrainsLib.Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Logging;

namespace SiloHost
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return RunMainAsync().GetAwaiter().GetResult();
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }
           
        private static async Task<ISiloHost> StartSilo()
        {
             var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .AddMemoryGrainStorage(Consts.DevStore)
                 .Configure<ClusterOptions>(options =>
                 {
                     options.ClusterId = Consts.ClusterId;
                     options.ServiceId = Consts.ServiceId;
                 })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(DirProcessorGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddFile("log.txt"))
                .AddSimpleMessageStreamProvider(Consts.FilesStreamProvider);
            
            var host = builder.Build();

            await host.StartAsync();
            return host;
        }
    }
}
