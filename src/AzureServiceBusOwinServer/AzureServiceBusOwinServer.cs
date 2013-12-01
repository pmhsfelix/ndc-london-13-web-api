using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Hosting.Engine;
using Microsoft.Owin.Hosting.ServerFactory;
using Microsoft.Owin.Hosting.Services;
using Microsoft.ServiceBus;
using Owin;

namespace AzureServiceBusOwin
{
    public class AzureServiceBusOwinServiceConfiguration
    {
        public string IssuerName { get; private set; }
        public string IssuerSecret { get; private set; }
        public string Address { get; private set; }

        public AzureServiceBusOwinServiceConfiguration(string issuerName, string issuerSecret, string address)
        {
            IssuerName = issuerName;
            IssuerSecret = issuerSecret;
            Address = address;
        }

        public TransportClientEndpointBehavior GetTransportBehavior()
        {
            return new TransportClientEndpointBehavior
            {
                TokenProvider =
                    TokenProvider.CreateSharedSecretTokenProvider(IssuerName, IssuerSecret)
            };
        }
    }

    // Based on Microsoft.Owin.Testing.TestServer
    public class AzureServiceBusOwinServer : IDisposable
    {
        public static AzureServiceBusOwinServer Create(AzureServiceBusOwinServiceConfiguration config, Action<IAppBuilder> startup)
        {
            var server = new AzureServiceBusOwinServer();
            server.Configure(config, startup);
            return server;
        }

        private void Configure(AzureServiceBusOwinServiceConfiguration config, Action<IAppBuilder> startup)
        {
            if (startup == null)
            {
                throw new ArgumentNullException("startup");
            }

            var options = new StartOptions();
            if (string.IsNullOrWhiteSpace(options.AppStartup))
            {
                // Populate AppStartup for use in host.AppName
                options.AppStartup = startup.Method.ReflectedType.FullName;
            }

            var testServerFactory = new AzureServiceBusOwinServerFactory(config);
            var services = ServicesFactory.Create();
            var engine = services.GetService<IHostingEngine>();
            var context = new StartContext(options)
            {
                ServerFactory = new ServerFactoryAdapter(testServerFactory),
                Startup = startup
            };
            _started = engine.Start(context);
            _next = testServerFactory.Invoke;
        }

        public void Dispose()
        {
            _started.Dispose();
        }

        private IDisposable _started;
        private Func<IDictionary<string, object>, Task> _next;
    }

    internal class AzureServiceBusOwinServerFactory
    {
        private readonly AzureServiceBusOwinServiceConfiguration _config;
        private Func<IDictionary<string, object>, Task> _app;

        public AzureServiceBusOwinServerFactory(AzureServiceBusOwinServiceConfiguration config)
        {
            _config = config;
        }

        public IDisposable Create(Func<IDictionary<string, object>, Task> app, IDictionary<string, object> properties)
        {
            var host = new WebServiceHost(new DispatcherService(app));
            var ep = host.AddServiceEndpoint(typeof(DispatcherService), GetBinding(), _config.Address);
            ep.Behaviors.Add(_config.GetTransportBehavior());
            host.Open();
            return host;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            return _app.Invoke(env);
        }

        private Binding GetBinding()
        {
            var b = new WebHttpRelayBinding(EndToEndWebHttpSecurityMode.None, RelayClientAuthenticationType.None);
            var elems = b.CreateBindingElements();
            var ee = elems.Find<WebMessageEncodingBindingElement>();
            ee.ContentTypeMapper = new RawContentTypeMapper();
            return new CustomBinding(elems);
        }

        internal class RawContentTypeMapper : WebContentTypeMapper
        {
            public override WebContentFormat GetMessageFormatForContentType(string contentType)
            {
                return WebContentFormat.Raw;
            }
        }
    }
}
