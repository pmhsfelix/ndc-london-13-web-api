using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class RequestExtensions
    {
        public static HttpResponseMessage Json<T>(this HttpRequestMessage req, T t)
        {
            return new HttpResponseMessage()
            {
                Content = new JsonContent<T>(t)
            };
        }
    }

    public class JsonContent<T> : ObjectContent<T>
    {
        public JsonContent(T value) : base(value, new JsonMediaTypeFormatter())
        {
        }
    }

    


}
