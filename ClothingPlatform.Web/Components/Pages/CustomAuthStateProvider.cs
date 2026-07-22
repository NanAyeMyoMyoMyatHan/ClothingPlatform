using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ClothingPlatform.Web.Services;
using System.Security.Claims;
using System.Text.Json;

namespace ClothingPlatform.Web.Components.Pages
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ServerCookieService _cookieService;
        private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

        public CustomAuthStateProvider(IJSRuntime jsRuntime, ServerCookieService cookieService)
        {
            _jsRuntime = jsRuntime;
            _cookieService = cookieService;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Read HttpOnly cookie server-side — not accessible to JS
                var token = _cookieService.GetAuthToken();

                if (string.IsNullOrWhiteSpace(token))
                    return Task.FromResult(new AuthenticationState(_anonymous));

                return Task.FromResult(new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"))));
            }
            catch
            {
                return Task.FromResult(new AuthenticationState(_anonymous));
            }
        }

        // Call this method right after a successful API login
        public void NotifyUserAuthentication(string token)
        {
            var authenticatedUser = new ClaimsPrincipal(
                new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(authenticatedUser)));
        }

        // Call this for logout
        public async Task NotifyUserLogout()
        {
            // Clear HttpOnly cookies server-side
            _cookieService.ClearAuthCookies();

            // Clean up any legacy localStorage remnants
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "customerId");
            }
            catch { }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null)
            {
                foreach (var kvp in keyValuePairs)
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString() ?? ""));
            }
            return claims;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}