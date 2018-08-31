using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.IotHub
{
    public class IotHubManagerMiddleware
    {
        private readonly RequestDelegate _next;
        private IotHubHandler _iotHubHandler { get; set; }

        public IotHubManagerMiddleware(RequestDelegate next, IotHubHandler iotHubHandler)
        {
            _next = next;
            _iotHubHandler = iotHubHandler;
        }
        public async Task Invoke(HttpContext context)
        {
            System.Diagnostics.Debug.WriteLine("---- IotHubManagerMiddleware Invoke ----");

            if (_iotHubHandler.IsConnect)
            {
                System.Diagnostics.Debug.WriteLine(" isConnect");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(" is not Connect");
                _iotHubHandler.Connect();
            }

            await _next.Invoke(context);
        }

    }
}
