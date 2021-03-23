using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LatencyCheck.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(x => {
                    x.ServiceName = "LatencyCheck";
                })
                .ConfigureWebHostDefaults(web => {
                    web.UseStartup<Startup>();
                    web.UseUrls("http://localhost:5064");
                })
                .ConfigureAppConfiguration(config => {
                    config.AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LatencyCheck", "checks.json"), true, true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<LatencyCheckWorker>();
                    // services.AddHostedService<RegistryWorker>();
                });
    }
}
