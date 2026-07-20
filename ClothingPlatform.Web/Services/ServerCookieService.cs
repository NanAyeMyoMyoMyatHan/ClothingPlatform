using Microsoft.AspNetCore.Http;

namespace ClothingPlatform.Web.Services
{
    /// <summary>
    /// Sets and reads auth cookies server-side so HttpOnly, Secure, and SameSite
    /// flags are correctly applied — something that is impossible via document.cookie in JS.
    /// </summary>
    public class ServerCookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly CookieOptions _authOptions = new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(365),
            Path = "/"
        };

        private static readonly CookieOptions _expireOptions = new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UnixEpoch,
            Path = "/"
        };

        public ServerCookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // ── Write ──────────────────────────────────────────────────────────────

        /// <summary>Sets HttpOnly authToken + customerId cookies in one call.</summary>
        public void SetAuthCookies(string token, int userId)
        {
            var response = _httpContextAccessor.HttpContext?.Response;
            if (response == null || response.HasStarted) return;

            response.Cookies.Append("authToken", token, _authOptions);
            response.Cookies.Append("customerId", userId.ToString(), _authOptions);
        }

        /// <summary>Sets only the HttpOnly customerId cookie (for customer login flows without a JWT).</summary>
        public void SetCustomerIdCookie(int userId)
        {
            var response = _httpContextAccessor.HttpContext?.Response;
            if (response == null || response.HasStarted) return;

            response.Cookies.Append("customerId", userId.ToString(), _authOptions);
        }

        /// <summary>Expires both auth cookies, effectively logging the user out.</summary>
        public void ClearAuthCookies()
        {
            var response = _httpContextAccessor.HttpContext?.Response;
            if (response == null || response.HasStarted) return;

            response.Cookies.Append("authToken", "", _expireOptions);
            response.Cookies.Append("customerId", "", _expireOptions);
        }

        // ── Read ───────────────────────────────────────────────────────────────

        /// <summary>Returns the raw JWT token from the request cookie, or null.</summary>
        public string? GetAuthToken()
            => _httpContextAccessor.HttpContext?.Request.Cookies["authToken"];

        /// <summary>Returns the customerId as an int, or null if absent/invalid.</summary>
        public int? GetCustomerId()
        {
            var raw = _httpContextAccessor.HttpContext?.Request.Cookies["customerId"];
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
