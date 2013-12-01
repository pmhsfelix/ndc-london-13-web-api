using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Testing;
using Owin;
using Xunit;

namespace Facts
{
    [Route("")]
    public class ResourcesController : ApiController
    {
        public HttpResponseMessage GetAll()
        {
            return new HttpResponseMessage();
        }
    }

    public class OWinFacts
    {
        [Fact]
        public async Task Fact1()
        {
            using(var server = TestServer.Create(app =>
            {
                var config = new HttpConfiguration();
                config.MapHttpAttributeRoutes();
                app.UseWebApi(config); })){

                var client = server.HttpClient;
                var resp = await client.GetAsync("http://example.net/");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            }
        }
    }
}
