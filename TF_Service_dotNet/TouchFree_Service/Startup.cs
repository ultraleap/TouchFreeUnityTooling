using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Ultraleap.TouchFree.Library.Configuration;
using Ultraleap.TouchFree.Service.Connection;

namespace Ultraleap.TouchFree.Service
{
    public class Startup
    {
        InteractionManager interactionManager;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddUpdateBehaviour();
            services.AddConfig();

            services.AddTrackingConnectionManager();
            services.AddHandManager();
            services.AddVirtualScreen();

            services.AddPositioning();

            services.AddClientConnectionManager();
            services.AddWebSocketReceiver();

            services.AddInteractions();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseWebSockets();
            app.UseStaticFiles("/settings");

            app.UseTouchFreeRouter();

            interactionManager = app.ApplicationServices.GetService<InteractionManager>();
            var configManager = app.ApplicationServices.GetService<IConfigManager>();

            // This is here so the test infrastructure has some sign that the app is ready
            Console.WriteLine("Service Setup Complete");

            Console.WriteLine("TouchFree physical config screen height is: " + configManager.PhysicalConfig.ScreenHeightMm + " mm");
        }
    }
}