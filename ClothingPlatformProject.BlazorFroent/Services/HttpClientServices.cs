using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http.Headers;

namespace ClothingPlatformProject.BlazorFroent.Services
{
    public class HttpClientServices
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJSRuntime _jsRuntime;

        public HttpClientServices(IHttpClientFactory httpClientFactory, IJSRuntime jsRuntime)
        {
            _httpClientFactory = httpClientFactory;
            _jsRuntime = jsRuntime;
        }

        public async Task<T?> ExecuteAsync<T>(string url, object? obj = null, EnumHttpMethod method = EnumHttpMethod.Get)
        {
            HttpResponseMessage? response = null;
            HttpContent? content = null;

            if (obj != null)
            {
                string jsonString = JsonConvert.SerializeObject(obj);
                content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
            }

            var client = _httpClientFactory.CreateClient("admin");
            await AttachBearerTokenAsync(client);

            switch (method)
            {
                case EnumHttpMethod.Get: response = await client.GetAsync(url); break;
                case EnumHttpMethod.Post: response = await client.PostAsync(url, content); break;
                case EnumHttpMethod.Put: response = await client.PutAsync(url, content); break;
                case EnumHttpMethod.Patch: response = await client.PatchAsync(url, content); break;
                case EnumHttpMethod.Delete: response = await client.DeleteAsync(url); break;
                default: throw new Exception("PLM Invalid Http method.");
            }

            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var streamReader = new StreamReader(responseStream);
                using var jsonReader = new JsonTextReader(streamReader);

                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(jsonReader);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($" Http request failed ({response.StatusCode}). API Error: {errorContent}");
        }

        private async Task AttachBearerTokenAsync(HttpClient client)
        {
            try
            {
                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                client.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
                    ? null
                    : new AuthenticationHeaderValue("Bearer", token);
            }
            catch
            {
                client.DefaultRequestHeaders.Authorization = null;
            }
        }
    }

    public enum EnumHttpMethod
    {
        None,
        Get,
        Post,
        Put,
        Patch,
        Delete
    }
}
