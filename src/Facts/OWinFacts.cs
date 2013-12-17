using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Testing;
using Newtonsoft.Json.Linq;
using Owin;
using Xunit;

namespace Facts
{
    public class ResourcesController : ApiController
    {
        public HttpResponseMessage Get()
        {
            return Request.CreateResponse(HttpStatusCode.OK, new {msg = "hello"}, new JsonMediaTypeFormatter());
        }
    }

    public class OWinFacts
    {
        [Fact]
        public async Task Katana_Contains_TestServer_for_MemoryHosting()
        {
            using(var server = TestServer.Create(app =>
            {
                var config = new HttpConfiguration();
                config.Routes.MapHttpRoute("WebApiDefault", "{controller}/{id}", new {id = RouteParameter.Optional});
                app.UseWebApi(config); 
            })){
                var client = server.HttpClient;
                var resp = await client.GetAsync("http://does.not.matter/resources");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                var body = await resp.Content.ReadAsAsync<JObject>();
                Assert.Equal("hello",body["msg"].Value<string>());
            }
        }
    }
}
