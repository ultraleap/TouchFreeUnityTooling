using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Ultraleap.TouchFree.Library.Configuration;

namespace Ultraleap.TouchFree.Service.Connection
{
    public static class TouchFreeRouterExtensions
    {
        public static IApplicationBuilder UseTouchFreeRouter(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TouchFreeRouter>();
        }

        public static IServiceCollection AddClientConnectionManager(this IServiceCollection services)
        {
            services.AddSingleton<ClientConnectionManager>();
            return services;
        }

        public static IServiceCollection AddWebSocketReceiver(this IServiceCollection services)
        {
            services.AddSingleton<WebSocketReceiver>();
            return services;
        }
    
        public static IServiceCollection AddUpdateBehaviour(this IServiceCollection services)
        {
            services.AddSingleton<UpdateBehaviour>();
            return services;
        }

        public static IServiceCollection AddConfigFileWatcher(this IServiceCollection services)
        {
            var watcher = new ConfigFileWatcher();

            services.BuildServiceProvider().GetService<UpdateBehaviour>().OnUpdate += watcher.Update;

            var descriptor = new ServiceDescriptor(typeof(ConfigFileWatcher), watcher);

            services.Add(descriptor);
            return services;
        }
    }
}