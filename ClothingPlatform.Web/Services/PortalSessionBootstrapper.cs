using System.Text.Json;
using ClothingPlatform.DB.AppDbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace ClothingPlatform.Web.Services
{
    public class PortalSessionBootstrapper : IPortalSessionBootstrapper
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IJSRuntime _jsRuntime;
        private readonly SessionState _session;

        public PortalSessionBootstrapper(IDbContextFactory<AppDbContext> dbFactory, IJSRuntime jsRuntime, SessionState session)
        {
            _dbFactory = dbFactory;
            _jsRuntime = jsRuntime;
            _session = session;
        }

        public async Task<bool> RestorePortalSessionAsync()
        {
            if (_session.IsLoggedIn)
            {
                return _session.IsAdmin || _session.IsStaff;
            }

            string? token;
            try
            {
                token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            }
            catch
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var claims = TryReadJwtPayload(token);
            if (claims.Count == 0 || IsExpired(claims))
            {
                await RemoveStoredTokenAsync();
                return false;
            }

            var userId = TryGetIntClaim(claims, "nameid")
                ?? TryGetIntClaim(claims, "sub")
                ?? TryGetIntClaim(claims, "nameidentifier");
            var email = TryGetStringClaim(claims, "email")
                ?? TryGetStringClaim(claims, "emailaddress");

            await using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .AsQueryable();

            User? user = userId.HasValue
                ? await query.FirstOrDefaultAsync(u => u.UserId == userId.Value)
                : null;

            if (user == null && !string.IsNullOrWhiteSpace(email))
            {
                var normalizedEmail = email.Trim().ToLower();
                user = await query.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            }

            var roleName = user?.Role?.RoleName;
            if (user == null ||
                (!string.Equals(roleName, "admin", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(roleName, "staff", StringComparison.OrdinalIgnoreCase)))
            {
                await RemoveStoredTokenAsync();
                return false;
            }

            var permissions = user.Role?.RolePermissions?
                .Where(rp => rp.Permission != null)
                .Select(rp => rp.Permission.PermissionName)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            _session.Login(user, permissions);
            return true;
        }

        private async Task RemoveStoredTokenAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            }
            catch
            {
            }
        }

        private static Dictionary<string, JsonElement> TryReadJwtPayload(string jwt)
        {
            var segments = jwt.Split('.');
            if (segments.Length < 2)
            {
                return new Dictionary<string, JsonElement>();
            }

            try
            {
                var jsonBytes = ParseBase64WithoutPadding(segments[1]);
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes)
                    ?? new Dictionary<string, JsonElement>();
            }
            catch
            {
                return new Dictionary<string, JsonElement>();
            }
        }

        private static bool IsExpired(Dictionary<string, JsonElement> claims)
        {
            var exp = TryGetLongClaim(claims, "exp");
            return exp.HasValue && DateTimeOffset.FromUnixTimeSeconds(exp.Value) <= DateTimeOffset.UtcNow;
        }

        private static string? TryGetStringClaim(Dictionary<string, JsonElement> claims, string name)
        {
            var claim = claims.FirstOrDefault(c =>
                string.Equals(c.Key, name, StringComparison.OrdinalIgnoreCase) ||
                c.Key.EndsWith("/" + name, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(claim.Key))
            {
                return null;
            }

            return claim.Value.ValueKind == JsonValueKind.String
                ? claim.Value.GetString()
                : claim.Value.ToString();
        }

        private static int? TryGetIntClaim(Dictionary<string, JsonElement> claims, string name)
        {
            var value = TryGetLongClaim(claims, name);
            return value.HasValue ? Convert.ToInt32(value.Value) : null;
        }

        private static long? TryGetLongClaim(Dictionary<string, JsonElement> claims, string name)
        {
            var value = TryGetStringClaim(claims, name);
            if (long.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            base64 = base64.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}
