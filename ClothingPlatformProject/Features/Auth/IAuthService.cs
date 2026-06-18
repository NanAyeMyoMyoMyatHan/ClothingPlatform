using ClothingPlatformProject.Models.Auth;

namespace ClothingPlatformProject.Features.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(AuthRequest request);
    }
}
