using ClothingPlatformProject.Models.Auth;

namespace ClothingPlatformProject.Features.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> loginuser(LoginRequest loginRequest);
    }
}
