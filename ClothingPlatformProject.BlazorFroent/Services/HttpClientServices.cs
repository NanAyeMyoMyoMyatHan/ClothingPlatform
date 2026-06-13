using Newtonsoft.Json;
using System.IO;

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
                // 💡 .ToJson() တိုးချဲ့မက်သဒ် ရှိရင်လည်း သုံးနိုင်သလို၊ မရှိရင် အောက်ကအတိုင်း standard ရေးနိုင်ပါတယ်
                string jsonString = JsonConvert.SerializeObject(obj);
                content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
            }

            var client = _httpClientFactory.CreateClient("admin");

            switch (method)
            {
                case EnumHttpMethod.Get: response = await client.GetAsync(url); break;
                case EnumHttpMethod.Post: response = await client.PostAsync(url, content); break;
                case EnumHttpMethod.Put: response = await client.PutAsync(url, content); break;
                case EnumHttpMethod.Patch: response = await client.PatchAsync(url, content); break;
                case EnumHttpMethod.Delete: response = await client.DeleteAsync(url); break;
                default: throw new Exception("PLM Invalid Http method.");
            }

            // 🟢 အလုပ်အောင်မြင်ခဲ့လျှင် (၂၀၀ ကျော် Status Code များ)
            if (response.IsSuccessStatusCode)
            {
                // ✨ အသက်က ဒီနေရာပါ! စာသားကြီးအဖြစ် အကုန်မပြောင်းဘဲ Stream အဖြစ် ဖတ်ပါတယ်
                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var streamReader = new StreamReader(responseStream);
                using var jsonReader = new JsonTextReader(streamReader);

                var serializer = new JsonSerializer();
                // ဒေတာ ဘယ်လောက်ကြီးကြီး Memory မစားဘဲ စီးကြောင်းအတိုင်း တိုက်ရိုက်ပြောင်းလဲပေးသွားပါတယ်
                return serializer.Deserialize<T>(jsonReader);
            }

            // 🔴 အလုပ်မအောင်မြင်လျှင် (API ဘက်က တက်လာတဲ့ Error Message အစစ်ကို ဖတ်ပြခြင်း)
            var errorContent = await response.Content.ReadAsStringAsync();

            // 💡 ရှင်းလင်းတဲ့ Error မက်ဆေ့ချ် ရအောင် ပြင်ဆင်လိုက်ပါတယ်
            throw new Exception($"PLM Http request failed ({response.StatusCode}). API Error: {errorContent}");
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