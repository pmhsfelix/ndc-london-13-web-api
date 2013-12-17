using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Owin;
using Fix;
using Microsoft.Owin;
using Nowin;
using Common;

namespace FixApp
{
    public class HelloController : ApiController
    {
        public HttpResponseMessage GetAll()
        {
            return new HttpResponseMessage
            {
                Content = new WaveContent("Hello from NDC London 2013")
            };
        }
    }
    
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

    // Adapts between OwinMiddleware and Func<IDictionary<string,object>,Task> 
    class AdapterMiddleware : OwinMiddleware
    {
        private readonly Func<IDictionary<string, object>, Task> _next;
        public AdapterMiddleware(Func<IDictionary<string,object>,Task> next) :
            // According to the source code, the OwinMiddleware.Next can be null
            // This middleware next element is not a OwinMiddleware
            base(null) 
        {
            _next = next;
        }

        public override Task Invoke(IOwinContext context)
        {
            return _next.Invoke(context.Environment);
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
            
            // Build the OWIN app
            var app = new Fixer()
                // I'm creating a new handler *for each* request
                // because the next is defined in the HttpMessageHandlerAdapter ctor,
                .Use((ctx, next) => 
                    new HttpMessageHandlerAdapter(
                        new AdapterMiddleware(next), 
                        server, 
                        new OwinBufferPolicySelector())
                            .Invoke(new OwinContext(ctx)))

                // Final middleware to return a 404
                .Use(async (ctx, next) =>
                {
                    var kctx = new OwinContext(ctx);
                    kctx.Response.StatusCode = 404;
                    kctx.Response.Headers.Append("Content-Type", "text/plain");
                    await kctx.Response.WriteAsync("No one has the resource identified by the request");
                })
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
