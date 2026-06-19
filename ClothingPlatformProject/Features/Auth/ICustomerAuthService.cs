using ClothingPlatformProject.Models.Auth;

namespace ClothingPlatformProject.Features.Auth
{
    public interface ICustomerAuthService
    {
        Task<CustomerAuthResponse?> LoginAsync(CustomerLoginRequest request);
        Task<CustomerAuthResponse> RegisterAsync(CustomerRegisterRequest request);
    }
}
