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
        Task PostXml(string url, string xml);
        Task<T> PostXml<T>(string url, string xml);
        Task Delete(string url);
    }

    public class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly HttpClient _httpClient;

        public HttpClientWrapper(string hostname, string username, string password)
        {
            _httpClient = Create(username, password, hostname);
        }

        public async Task<T> Get<T>(string url, params object[] args)
        {
            string requestUri = string.Format(url, args);

            HttpResponseMessage response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);

            VerifyResponse(response, requestUri);

            string json = await response.Content.ReadAsStringAsync();

            return Json.Deserialize<T>(json);
        }

        public async Task<Stream> GetStream(string url, params object[] args)
        {
            string requestUri = string.Format(url, args);

            HttpResponseMessage response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);

            VerifyResponse(response, requestUri);

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<string> GetString(string url, params object[] args)
        {
            string requestUri = string.Format(url, args);

            HttpResponseMessage response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);

            VerifyResponse(response, requestUri);

            return await response.Content.ReadAsStringAsync();
        }

        public async Task PostXml(string url, string xml)
        {
            var stringContent = new StringContent(xml, Encoding.UTF8, "application/xml");
            var response = await _httpClient.PostAsync(url, stringContent).ConfigureAwait(false);

            VerifyResponse(response, url);
        }

        public async Task<T> PostXml<T>(string url, string xml)
        {
            var stringContent = new StringContent(xml, Encoding.UTF8, "application/xml");
            var response = await _httpClient.PostAsync(url, stringContent).ConfigureAwait(false);

            VerifyResponse(response, url);

            string json = await response.Content.ReadAsStringAsync();

            return Json.Deserialize<T>(json);
        }

        public async Task Delete(string url)
        {
            var httpResponseMessage = await _httpClient.DeleteAsync(url);

            VerifyResponse(httpResponseMessage, url);
        }

        private HttpClient Create(string username, string password, string hostname)
        {
            var httpClientHandler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(username, password),
            };

            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri("http://" + hostname)
            };

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        private static void VerifyResponse(HttpResponseMessage response, string requestUri)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception("Resource not found: " + requestUri);
            }

            response.EnsureSuccessStatusCode();
        }

    }
}
