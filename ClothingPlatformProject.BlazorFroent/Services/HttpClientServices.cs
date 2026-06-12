using Newtonsoft.Json;

namespace ClothingPlatformProject.BlazorFroent.Services
{
    public class HttpClientServices
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpClientServices(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task<T?> ExecuteAsync<T>(string url, object? obj = null, EnumHttpMethod method = EnumHttpMethod.Get)
        {
            HttpResponseMessage? response = null;
            HttpContent? content = null;
            if (obj != null)
            {
                //string jsonString = JsonConvert.SerializeObject(obj);
                content = new StringContent(obj.ToJson(), System.Text.Encoding.UTF8, "application/json");
            }
            var client = _httpClientFactory.CreateClient("admin");// ဒီမှာ ကိုယ်ထည့်ချင်တဲ့ နာမည်ထည့်ပေးရမယ်
            switch (method)
            {
                case EnumHttpMethod.Get:
                    response = await client.GetAsync(url);
                    break;
                case EnumHttpMethod.Post:
                    response = await client.PostAsync(url, content);
                    break;
                case EnumHttpMethod.Put:
                    response = await client.PutAsync(url, content);
                    break;
                case EnumHttpMethod.Patch:
                    response = await client.PatchAsync(url, content);
                    break;
                case EnumHttpMethod.Delete:
                    response = await client.DeleteAsync(url);
                    break;
                case EnumHttpMethod.None:
                default:
                    throw new Exception("PLM Invalid Http method.");
            }
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(jsonString)!;
            }
            // API ဘက်က တက်လာတဲ့ Error Message အစစ်ကို ဆွဲထုတ်ပြခြင်း
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"PLM Http request failed with status code: {response.StatusCode}");
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

