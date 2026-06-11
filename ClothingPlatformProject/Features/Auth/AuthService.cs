using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatformProject.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;

        public AuthService(AppDbContext db)
        {
            _db = db;
        }
        public LoginResponseModel? loginuser(LoginRequestDto loginRequest)
        {
            var user = _db.Users
                .AsNoTracking()
                .FirstOrDefault(x =>
                x.Email == loginRequest.Email &&
                x.PasswordHash == loginRequest.Password); 
            if(user != null)
            {
                return new LoginResponseModel
                {
                    Email = user.Email,
                    UserId = user.UserId,
                    Username = $"{user.FirstName}{user.LastName}"
                };
            }
            return null;
        }
    }
}
