using Microsoft.JSInterop;

namespace ClothingPlatform.Web.Services
{
    /// <summary>
    /// LEGACY UTILITY — read-only JS cookie helper for non-sensitive cookies.
    /// Auth cookies (authToken, customerId) are now managed exclusively by
    /// <see cref="ServerCookieService"/> which sets the HttpOnly flag server-side.
    /// </summary>
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
    }
}
