using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Routing;
using Xunit;

namespace Facts
{
    [RoutePrefix("resources")]
    public class ResourceController : ApiController
    {
        [Route("")]
        public HttpResponseMessage GetAll()
        {
            return new HttpResponseMessage();
        }

        [Route("{id}")]
        public HttpResponseMessage GetById(string id)
        {
            return new HttpResponseMessage();
        }

        [Route("{id}/related")]
        public HttpResponseMessage GetByIdRelated(string id)
        {
            return new HttpResponseMessage();
        }
    }

    public class Resources2Controller : ApiController
    {
        public HttpResponseMessage GetAll()
        {
            return new HttpResponseMessage();
        }
    }
    
    public class AttributeFacts
    {
        [Fact]
        public async Task Fact1()
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            config.EnsureInitialized();
            var routes = config.Routes;
            var server = new HttpServer(config);
            var client = new HttpClient(server);
            var resp = await client.GetAsync("http://example.net/resources");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            resp = await client.GetAsync("http://example.net/resources/123");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            resp = await client.GetAsync("http://example.net/resources/123/related");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task Fact2()
        {
            var config = new HttpConfiguration();
            var controllerSelector = config.Services.GetHttpControllerSelector();
            var actionSelector = config.Services.GetActionSelector();
            config.Services.Replace(typeof(IHttpControllerSelector), new ControllerSelectorDecorator(controllerSelector));
            config.Services.Replace(typeof(IHttpActionSelector), new ActionSelectorDecorator(actionSelector));
            controllerSelector = config.Services.GetHttpControllerSelector();
            var cmap = controllerSelector.GetControllerMapping();
            Assert.Equal(2, cmap.Count);
            var cdesc = cmap.First(kp => kp.Value.ControllerType == typeof (Resources2Controller)).Value as MyControllerDescriptor;
            cdesc.Attributes.Add(new RouteAttribute("resources2"));
            config.MapHttpAttributeRoutes();
            config.EnsureInitialized();
            var routes = config.Routes;
            var server = new HttpServer(config);
            var client = new HttpClient(server);
            var resp = await client.GetAsync("http://example.net/resources2");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        public class ControllerSelectorDecorator : IHttpControllerSelector
        {
            private readonly IHttpControllerSelector _inner;

            public HttpControllerDescriptor SelectController(HttpRequestMessage request)
            {
                return _inner.SelectController(request);
            }

            private Lazy<IDictionary<string, HttpControllerDescriptor>> _map;
            public IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
            {
                return _map.Value;
            }

            public ControllerSelectorDecorator(IHttpControllerSelector inner)
            {
                _inner = inner;
                _map = new Lazy<IDictionary<string, HttpControllerDescriptor>>(
                    () => _inner.GetControllerMapping().ToDictionary(kp => kp.Key, kp => new MyControllerDescriptor(kp.Value) as HttpControllerDescriptor)); 

            }
        }

        public class MyControllerDescriptor : HttpControllerDescriptor
        {
            public MyControllerDescriptor(HttpControllerDescriptor inner)
                : base(inner.Configuration, inner.ControllerName, inner.ControllerType)
            {
                Attributes = new Collection<Attribute>();
            }

            public ICollection<Attribute> Attributes { get; private set; }

            public override Collection<T> GetCustomAttributes<T>(bool inherit)
            {
                var fromBase = base.GetCustomAttributes<T>(inherit);
                return new Collection<T>(Attributes.OfType<T>().Concat(fromBase).ToList());
            }
        }
    }

    public class ActionSelectorDecorator : IHttpActionSelector
    {
        private readonly IHttpActionSelector _inner;

        public ActionSelectorDecorator(IHttpActionSelector inner)
        {
            _inner = inner;
        }

        public HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
        {
            return Decorate(_inner.SelectAction(controllerContext));
        }

        private readonly ConcurrentDictionary<HttpControllerDescriptor, ILookup<string, HttpActionDescriptor>> _map =
            new ConcurrentDictionary<HttpControllerDescriptor, ILookup<string, HttpActionDescriptor>>();

        public ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
        {
            return _map.GetOrAdd(controllerDescriptor,
                d => _inner.GetActionMapping(d)
                    .SelectMany(g => g, (g,i) => new {g.Key,i})
                    .ToLookup(g => g.Key, g => g.i));


        }

        private HttpActionDescriptor Decorate(HttpActionDescriptor desc)
        {
            var rhad = desc as ReflectedHttpActionDescriptor;
            if (rhad == null) return desc;
            return new MyActionDescriptor(rhad);
        }
    }

    internal class MyActionDescriptor : ReflectedHttpActionDescriptor
    {
        public MyActionDescriptor(ReflectedHttpActionDescriptor desc)
            : base(desc.ControllerDescriptor, desc.MethodInfo)
        {
            Attributes = new Collection<Attribute>();
        }

        public ICollection<Attribute> Attributes { get; private set; }

        public override Collection<T> GetCustomAttributes<T>(bool inherit)
        {
            var fromBase = base.GetCustomAttributes<T>(inherit);
            return new Collection<T>(Attributes.OfType<T>().Concat(fromBase).ToList());
        }
    }
}
