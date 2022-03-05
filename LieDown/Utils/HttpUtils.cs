using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LieDown
{

    public class HttpException : Exception
    {
        public HttpException(System.Net.HttpStatusCode statusCode, System.Net.Http.Headers.HttpResponseHeaders headers, string message) : base(message)
        {

            StatusCode = statusCode;
            Headers = headers;
        }
        public System.Net.HttpStatusCode StatusCode { get; set; }
        public System.Net.Http.Headers.HttpResponseHeaders Headers { get; set; }

    }
    public class HttpResut<T>
    {
        public T Data { get; set; }
    }
    public class HttpUtils
    {
        public static async Task<T> PostAsync<T>(string url, string postJson)
        {

            using var client = new HttpClient();
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(postJson, Encoding.UTF8, "application/json")
            };
            using var httpResponseMessage = await client.SendAsync(httpRequestMessage);
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var graphQLResponse = JsonConvert.DeserializeObject<HttpResut<T>>(content);
                return graphQLResponse.Data;
            }

            // error handling
            throw new HttpException(httpResponseMessage.StatusCode, httpResponseMessage.Headers, content);

        }
    }
}
