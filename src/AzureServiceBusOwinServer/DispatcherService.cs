﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Owin;
using Microsoft.ServiceBus.Web;

namespace AzureServiceBusOwin
{
    [ServiceContract]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal class DispatcherService
    {
        private readonly Func<IDictionary<string, object>, Task> _next;

        public DispatcherService(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        [WebGet(UriTemplate = "*")]
        [OperationContract(AsyncPattern = true)]
        public async Task<Message> GetAsync()
        {
            var webOperContext = WebOperationContext.Current;
            var ms = new MemoryStream();
            var owinContext = MakeOwinContextFrom(webOperContext.IncomingRequest, Stream.Null, ms);
            await _next.Invoke(owinContext.Environment);
            CopyOwinContextToOutgoingResponse(owinContext, webOperContext.OutgoingResponse);
            ms.Seek(0, SeekOrigin.Begin);
            return StreamMessageHelper.CreateMessage(MessageVersion.None, "GETRESPONSE", ms);
        }

        [WebInvoke(UriTemplate = "*", Method = "*")]
        [OperationContract(AsyncPattern = true)]
        public async Task<Message> InvokeAsync(Message msg)
        {
            var webOperContext = WebOperationContext.Current;
            object value;
            Stream s = null;
            if (msg.Properties.TryGetValue("WebBodyFormatMessageProperty", out value))
            {
                var prop = value as WebBodyFormatMessageProperty;
                if (prop != null && (prop.Format == WebContentFormat.Json || prop.Format == WebContentFormat.Raw))
                {
                    s = StreamMessageHelper.GetStream(msg);
                }
            }
            else
            {
                var ms = new MemoryStream();
                using (var xw = XmlDictionaryWriter.CreateTextWriter(ms, Encoding.UTF8, false))
                {
                    msg.WriteBodyContents(xw);
                }
                ms.Seek(0, SeekOrigin.Begin);
                s = ms;
            }

            var outputStream = new MemoryStream();
            var owinContext = MakeOwinContextFrom(webOperContext.IncomingRequest, s, outputStream);
            await _next.Invoke(owinContext.Environment);
            CopyOwinContextToOutgoingResponse(owinContext, webOperContext.OutgoingResponse);
            outputStream.Seek(0, SeekOrigin.Begin);
            return StreamMessageHelper.CreateMessage(MessageVersion.None, "GETRESPONSE", outputStream);

        }

        private void CopyOwinContextToOutgoingResponse(OwinContext owinContext, OutgoingWebResponseContext outgoingResponse)
        {
            outgoingResponse.StatusCode = (HttpStatusCode)owinContext.Response.StatusCode;
            foreach (var h in owinContext.Response.Headers)
            {
                outgoingResponse.Headers.Add(h.Key, owinContext.Response.Headers[h.Key]); // TODO remove double lookup
            }
        }

        private OwinContext MakeOwinContextFrom(IncomingWebRequestContext incomingRequest, Stream inputStream, Stream outputStream)
        {
            var ctx = new OwinContext();
            ctx.Request.Method = incomingRequest.Method;
            var reqUri = incomingRequest.UriTemplateMatch.RequestUri;
            ctx.Request.Scheme = reqUri.Scheme;
            //ctx.Request.Host = new HostString(reqUri.Host);
            ctx.Request.Path = new PathString(reqUri.AbsolutePath);
            ctx.Request.QueryString = new QueryString(reqUri.Query);

            foreach (var name in incomingRequest.Headers.AllKeys)
            {
                ctx.Request.Headers.Append(name, incomingRequest.Headers.Get(name));
            }
            ctx.Request.Body = inputStream;
            ctx.Response.Body = outputStream;
            return ctx;
        }
    }
}
