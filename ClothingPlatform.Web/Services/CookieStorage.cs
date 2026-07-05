using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace ClothingPlatform.Web.Services
{
    public static class CookieStorage
    {
        public static async Task<string?> GetCookieAsync(IJSRuntime jsRuntime, string name)
        {
            try
            {
                var js = "(function() { " +
                         $"  var name = '{name}='; " +
                         "  var ca = decodeURIComponent(document.cookie).split(';'); " +
                         "  for(var i=0; i<ca.length; i++) { " +
                         "    var c = ca[i].trim(); " +
                         "    if (c.indexOf(name) == 0) return c.substring(name.length, c.length); " +
                         "  } " +
                         "  return ''; " +
                         "})()";
                return await jsRuntime.InvokeAsync<string>("eval", js);
            }
            catch
            {
                return null;
            }
        }

        public static async Task SetCookieAsync(IJSRuntime jsRuntime, string name, string value)
        {
            try
            {
                var js = $"document.cookie = '{name}={value}; path=/; max-age=31536000; SameSite=Lax';";
                await jsRuntime.InvokeVoidAsync("eval", js);
            }
            catch
            {
            }
        }

        public static async Task RemoveCookieAsync(IJSRuntime jsRuntime, string name)
        {
            try
            {
                var js = $"document.cookie = '{name}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 UTC; SameSite=Lax';";
                await jsRuntime.InvokeVoidAsync("eval", js);
            }
            catch
            {
            }
        }

        public static Task<string?> GetTokenAsync(IJSRuntime jsRuntime) => GetCookieAsync(jsRuntime, "authToken");

        public static Task SetTokenAsync(IJSRuntime jsRuntime, string token) => SetCookieAsync(jsRuntime, "authToken", token);

        public static Task RemoveTokenAsync(IJSRuntime jsRuntime) => RemoveCookieAsync(jsRuntime, "authToken");
    }
}
