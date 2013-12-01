using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Owin;
using Fix;
using Microsoft.Owin;
using Nowin;

namespace FixApp
{
    public class ResourcesController : ApiController
    {
        public HttpResponseMessage GetAll()
        {
            return new HttpResponseMessage
            {
                Content = new StringContent("Hello from Fix and Nowin")
            };
        }
    }
    class Program
    {
        // Based on http://blog.markrendle.net/2013/10/01/fix-and-owin-and-simple-web/
        static void Main()
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("ApiDefault", "{controller}/{id}", new {id = RouteParameter.Optional});
            var server = new HttpServer(config);
            var handler = new HttpMessageHandlerAdapter(null, server, new OwinBufferPolicySelector());
            
            // Build the OWIN app
            var app = new Fixer()
                .Use((ctx, next) => handler.Invoke(new OwinContext(ctx)))
                .Build();

            // Set up the Nowin server
            var builder = ServerBuilder.New()
                .SetPort(8080)
                .SetOwinApp(app);

            // Run
            using (builder.Start())
            {
                Console.WriteLine("Listening on port 8080. Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}
