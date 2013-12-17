using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;

namespace Facts
{
    
    public class MemoryHostingFacts
    {
        [Fact]
        public async Task Memory_host_by_connecting_the_client_to_the_server()
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            var server = new HttpServer(config);
            var client = new HttpClient(server);
            var resp = await client.GetAsync("http://does.not.matter/resources");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }
}
