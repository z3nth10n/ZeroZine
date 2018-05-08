using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotNetAssembly
{
    internal static class FlurlExtensions
    {
        public static string HttpPost(this string url, object data)
        {
            return url.PostUrlEncodedAsync(data).ReceiveString().GetAwaiter().GetResult();
        }

        public static string HttpGet(this string url)
        {
            return url.GetStringAsync().GetAwaiter().GetResult();
        }

        public static string HttpPost(this FlurlClient client, string url, object data)
        {
            return client.Request(url).PostUrlEncodedAsync(data).ReceiveString().GetAwaiter().GetResult();
        }

        public static string HttpGet(this FlurlClient client, string url)
        {
            return client.WithCookies(client.Cookies).Request(url).GetStringAsync().GetAwaiter().GetResult();
        }

        private static string AnonymousToQS(this object data)
        {
            Type type = data.GetType();
            PropertyInfo[] props = type.GetProperties();
            IEnumerable<string> pairs = props.Select(x => x.Name + "=" + x.GetValue(data, null)).ToArray();

            return string.Join("&", pairs);
        }
    }
}