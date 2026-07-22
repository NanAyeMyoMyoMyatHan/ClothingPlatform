using ClothingPlatform.Api.Models.Auth;

namespace ClothingPlatform.Api.Features.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(AuthRequest request);
    }
}
