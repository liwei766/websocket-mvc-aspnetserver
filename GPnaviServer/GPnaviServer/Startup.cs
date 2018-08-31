using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using GPnaviServer.Data;
using GPnaviServer.IotHub;
using GPnaviServer.Models;
using GPnaviServer.Services;
using GPnaviServer.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GPnaviServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = true;
            });

            services.AddMvc();

            services.AddRouting();

            services.AddIotHubManager();

            services.AddWebSocketManager();

            // 試験用オンメモリDB
            //services.AddDbContext<GPnaviServerContext>(opt => opt.UseInMemoryDatabase("GPnaviServerData"));

            // SQLServerを使用する
            services.AddDbContext<GPnaviServerContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserVersionService, UserVersionService>();
            services.AddScoped<IUserStatusService, UserStatusService>();
            services.AddScoped<IWorkScheduleService, WorkScheduleService>();
            services.AddScoped<IWorkScheduleVersionService, WorkScheduleVersionService>();
            
            services.AddAutoMapper();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            app.UseSession();

            app.UseAuthentication();


            app.UseMiddleware<IotHubManagerMiddleware>(serviceProvider.GetService<IotHubApiHandler>());

            //新規追加:Microsoft.AspNetCore.WebSockets 
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };
            app.UseWebSockets(webSocketOptions);

            //app.MapWebSocketManager("/ws", serviceProvider.GetService<TestMessageHandler>());

            // WebSocketAPI
            app.MapWebSocketManager("/wsapi", serviceProvider.GetService<WebSocketApiHandler>());

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Users}/{action=Login}");

            });



            app.UseDefaultFiles();
            app.UseStaticFiles();

        }

    }
}
