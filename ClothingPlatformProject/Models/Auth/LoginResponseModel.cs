namespace ClothingPlatformProject.Models.Auth
{
    public class LoginResponseModel
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
    }

    public class AuthResponse
    {
        public bool IsSuccess { get; set; }
        public string? Token { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
