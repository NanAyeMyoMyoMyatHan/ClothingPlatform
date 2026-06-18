using System.Net;

namespace ClothingPlatformProject.Models.Auth
{
    public class AuthRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class AuthResponse
    {
        public bool IsSuccessStatusCode;
        public HttpStatusCode StatusCode;

        public string AccessToken { get; set; }
        public string Role { get; set; }
        public List<string> Permissions { get; set; } = new();
        public string Content { get; set; }
    }
}
