using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using AzureServiceBusOwin;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Diagnostics;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.Provider;
using Owin;
using ServiceBusRelayHost.Demo.Screen;
using Common;

namespace ConsoleApp
{
    public class HelloController : ApiController
    {
        [Authorize]
        public HttpResponseMessage GetAll()
        {
            return new HttpResponseMessage
            {
                Content = new WaveContent(string.Format("Hello {0}", User.Identity.IsAuthenticated ? User.Identity.Name : "stranger"))
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

                    app.Use(async (ctx, next) =>
                    {
                        Trace.TraceInformation(ctx.Request.Uri.ToString());
                        await next();
                        Trace.TraceInformation(ctx.Response.StatusCode.ToString());
                    });

                    app.UseErrorPage();

                    
                    app.UseBasicAuthentication(new BasicAuthenticationOptions("ndc", (user, pw) => 
                    {
                        var ticket = user != pw
                            ? null
                            : new AuthenticationTicket
                            (
                                new ClaimsIdentity(new GenericIdentity(user, "Basic")),
                                new AuthenticationProperties()
                            );
                        return Task.FromResult(ticket);
                    }));

                    app.UseWebApi(config);
                }))
            {

                Console.WriteLine("Server is opened at {0}", sbConfig.Address);
                Process.Start(sbConfig.Address);
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
