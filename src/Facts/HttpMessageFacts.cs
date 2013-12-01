using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Facts
{
    public class HttpMessageFacts
    {
        [Fact]
        public void HttpRequestMessage_is_easy_to_instantiate()
        {
            var request = new HttpRequestMessage(
                        HttpMethod.Get,
                        new Uri("http://www.ietf.org/rfc/rfc2616.txt"));

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("http://www.ietf.org/rfc/rfc2616.txt", request.RequestUri.ToString());
            Assert.Equal(new Version(1, 1), request.Version);
        }

        [Fact]
        public void HttpResponseMessage_is_easy_to_instantiate()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
        }

        [Fact]
        public void Header_classes_expose_headers_in_a_strongly_typed_way()
        {
            var request = new HttpRequestMessage();
            request.Headers.Add(
                "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

            HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> accept = request.Headers.Accept;

            Assert.Equal(4, accept.Count);
            MediaTypeWithQualityHeaderValue third = accept.Skip(2).First();
            Assert.Equal("application/xml", third.MediaType);
            Assert.Equal(0.9, third.Quality);
            Assert.Null(third.CharSet);
            Assert.Equal(1, third.Parameters.Count);
            Assert.Equal("q", third.Parameters.First().Name);
            Assert.Equal("0.9", third.Parameters.First().Value);
        }

    }
}
