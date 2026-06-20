using ClothingPlatform.Api.Models.Auth;

namespace ClothingPlatform.Api.Features.Auth
{
    public interface ICustomerAuthService
    {
        Task<CustomerAuthResponse?> LoginAsync(CustomerLoginRequest request);
        Task<CustomerAuthResponse> RegisterAsync(CustomerRegisterRequest request);
    }
}
