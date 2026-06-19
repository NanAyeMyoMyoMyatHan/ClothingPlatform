using ClothingPlatformProject.Models.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatformProject.Features.Auth
{
    [Route("api/customer-auth")]
    [ApiController]
    public class CustomerAuthController : ControllerBase
    {
        private readonly ICustomerAuthService _customerAuthService;

        public CustomerAuthController(ICustomerAuthService customerAuthService)
        {
            _customerAuthService = customerAuthService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(CustomerLoginRequest request)
        {
            var result = await _customerAuthService.LoginAsync(request);
            if (result == null)
            {
                return Unauthorized(new { Message = "Email or password is incorrect." });
            }

            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(CustomerRegisterRequest request)
        {
            try
            {
                var result = await _customerAuthService.RegisterAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { ex.Message });
            }
        }
    }
}
