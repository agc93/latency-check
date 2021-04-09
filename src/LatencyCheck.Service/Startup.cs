using System.Collections.Generic;
using LatencyCheck.Service.Counters;
using LatencyCheck.Service.Registry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LatencyCheck.Service
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services) {
            services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals);
            services.AddHostFiltering(x => {
                x.AllowedHosts = new List<string> { "localhost", "127.0.0.1", "[::1" };
            });
            services.AddSingleton<ProcessConnectionClient>(p =>
                ProcessConnectionClient.Create("LatencyCheck.Service.exe"));
            var section = Configuration.GetSection("Processes");
            var checks = section.Exists() ? section.Get<List<string>>() : new List<string>();
            foreach (var check in checks)
            {
                services.AddSingleton<ProcessConnectionClient>(p => ProcessConnectionClient.Create(check));
            }

            services.AddSingleton<ProcessSet>(p => new ProcessSet(p.GetServices<ProcessConnectionClient>()));

            services.AddSingleton<IUpdateHandler, RegistryUpdateHandler>();
            services.AddSingleton<IUpdateHandler, PerformanceCounterHandler>();
            services.AddSingleton<IUpdateHandler, CacheUpdateHandler>();
            services.AddMemoryCache();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Error");
                app.UseForwardedHeaders();
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            // app.UseHttpsRedirection();
            // app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}