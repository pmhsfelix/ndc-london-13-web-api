using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using AzureServiceBusOwin;
using Owin;
using ServiceBusRelayHost.Demo.Screen;
using Common;

namespace ConsoleApp
{
    public class ResourcesController : ApiController
    {
        public HttpResponseMessage GetAll()
        {
            return new HttpResponseMessage
            {
                Content = new WaveContent("Hello NDC London")
            };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var sbConfig = new AzureServiceBusOwinServiceConfiguration(
                issuerName: "owner",
                issuerSecret: SecretCredentials.Secret,
                address: SecretCredentials.ServiceBusAddress);

            using (AzureServiceBusOwinServer.Create(sbConfig, app =>
                {
                    var config = new HttpConfiguration();
                    config.Routes.MapHttpRoute("ApiDefault", "webapi/{controller}/{id}", new {id = RouteParameter.Optional});
                    config.MessageHandlers.Add(new TraceMessageHandler());

                    app.Use((ctx, next) =>
                    {
                        Trace.TraceInformation(ctx.Request.Uri.ToString());
                        return next();
                    });
                    app.UseWebApi(config);
                }))
            {

                Console.WriteLine("Server is opened at {0}", sbConfig.Address);
                Console.ReadKey();
            }
        }
    }

    internal class TraceMessageHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Trace.TraceInformation(request.RequestUri.ToString());
            return base.SendAsync(request, cancellationToken);
        }
    }
}
