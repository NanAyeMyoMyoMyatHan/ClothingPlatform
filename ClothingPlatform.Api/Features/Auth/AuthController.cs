using ClothingPlatform.Api.Models.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatform.Api.Features.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> loginUser(AuthRequest request)
        {
            AuthResponse? result;
            try
            {
                result = await _authService.LoginAsync(request);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    ex.Message
                });
            }

            if (result == null)
            {
                return Unauthorized(new
                {
                    Message = "Email or password is incorrect."
                });
            }

            return Ok(result);
        }
    }
}
