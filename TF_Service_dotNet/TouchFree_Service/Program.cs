#if !DEBUG
using Ultraleap.TouchFree.Library.Configuration;
#endif
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Ultraleap.TouchFree.Service
{
    public class Program
    {
        static void Main(string[] args)
        {
#if !DEBUG
            TouchFreeLog.SetUpLogging();
#endif
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:9739");
                });
    }
}