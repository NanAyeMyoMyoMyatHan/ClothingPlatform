using ClothingPlatformProject.Models.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatformProject.Features.Auth
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

        [HttpPost("loginUser")]
        public IActionResult loginUser(LoginRequestDto request)
        {
            var result = _authService.loginuser(request);
            if (result == null)
            {
                return Unauthorized(new
                {
                    Message = "Email or Password is incorrect."

                });
            }
            return Ok(new
            {
                Message = "Login Success",
                Data = result
            });
        }
    }
}
