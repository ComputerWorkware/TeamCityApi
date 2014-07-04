using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeamCityApi.Util;

namespace TeamCityApi
{
    public interface IHttpClientWrapper
    {
        Task<T> Get<T>(string url, params object[] args);
        Task<Stream> GetStream(string url, params object[] args);
        Task<string> GetString(string url, params object[] args);
    }

    public class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly string _hostname;
        private readonly string _username;
        private readonly string _password;

        public HttpClientWrapper(string hostname, string username, string password)
        {
            _hostname = hostname;
            _username = username;
            _password = password;
        }

        public async Task<T> Get<T>(string url, params object[] args)
        {
            string requestUri = string.Format(url, args);

            using (HttpClient httpClient = Create())
            {
                HttpResponseMessage response = await httpClient.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                return Json.Deserialize<T>(json);
            }
        }

        public async Task<Stream> GetStream(string url, params object[] args)
        {
            string requestUri = string.Format(url, args);

            using (var httpClient = Create())
            {
                HttpResponseMessage response = await httpClient.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync();
            }
        }

        public async Task<string> GetString(string url, params object[] args)
        {
            string requestUri = string.Format(url, args);

            using (HttpClient httpClient = Create())
            {
                HttpResponseMessage response = await httpClient.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
        }

        private HttpClient Create()
        {
            var httpClientHandler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(_username, _password),
            };

            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri("http://" + _hostname)
            };

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }
    }
}
