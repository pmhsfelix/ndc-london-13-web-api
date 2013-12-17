using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Common;
using Microsoft.Owin.Hosting;
using Owin;
using Xunit;

namespace Facts
{
    public class WhatIpAmIController : ApiController
    {
        public HttpResponseMessage Get(IPAddress ip)
        {
            return new HttpResponseMessage
            {
                Content = new StringContent(ip != null ? ip.ToString() : "unknown")
            };
        }
    }

    public class ParameterBindingFacts : IDisposable
    {
        private readonly IDisposable _app;

        public ParameterBindingFacts()
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("ApiDefault", "{controller}/{id}", new {id = RouteParameter.Optional});
            config.ParameterBindingRules.Add(
                IpAddressParameterBinding.Rule(req => req.GetOwinContext().Request.RemoteIpAddress));

            _app = WebApp.Start("http://localhost:8080/", app =>
            {
                app.UseWebApi(config);
            });
        }

        [Fact]
        public async Task Can_define_custom_parameter_binder()
        {
            var client = new HttpClient();
            var res = await client.GetAsync("http://localhost:8080/whatipami");
            var cont = await res.Content.ReadAsStringAsync();
            Assert.Equal("::1", cont);
        }

        public void Dispose()
        {
            _app.Dispose();
        }
    }
}
