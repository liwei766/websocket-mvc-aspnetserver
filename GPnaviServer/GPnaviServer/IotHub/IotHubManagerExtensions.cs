using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GPnaviServer.IotHub
{
    public static class IotHubManagerExtensions
    {
        public static IServiceCollection AddIotHubManager(this IServiceCollection services)
        {
            services.AddTransient<IotHubConnectionManager>();

            foreach (var type in Assembly.GetEntryAssembly().ExportedTypes)
            {
                if (type.GetTypeInfo().BaseType == typeof(IotHubHandler))
                {
                    services.AddSingleton(type);
                }
            }

            return services;
        }
    }
}
