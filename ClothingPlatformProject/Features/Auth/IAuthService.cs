using ClothingPlatformProject.Models.Auth;

namespace ClothingPlatformProject.Features.Auth
{
    public interface IAuthService
    {
        LoginResponseModel loginuser(LoginRequestDto loginRequest);
    }
}
