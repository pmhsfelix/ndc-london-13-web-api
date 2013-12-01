using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Assertions
{
    public static class HttpResponseMessageAssertionExtensions
    {
        public static void ShouldBeJsonOf<T>(this HttpResponseMessage res, Action<T> a)
        {
            
        }
    }
}
