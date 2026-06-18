using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClothingPlatformProject.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }
        public async Task<AuthResponse> LoginAsync(AuthRequest request)
        {
            var user =await _db.TblUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                x.Email == request.Email &&
                x.PasswordHash == request.Password);

            if (user == null) return null;
            var role = await _db.TblRoles.FirstOrDefaultAsync(
                r => r.RoleId == user.RoleId);
            var roleName = role?.RoleName ?? string.Empty;
            var permissions = await (from rp in _db.TblRolePermissions
                                     join p in _db.TblPermissions on
                                     rp.PermissionId equals p.PermissionId
                                     where rp.RoleId == user.RoleId
                                     select p.PermissionName).ToListAsync();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,user.UserId.ToString()),
                new Claim(ClaimTypes.Email,user.Email),
                new Claim(ClaimTypes.Role,roleName),
            };
            foreach(var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires:DateTime.Now.AddMinutes(
                    Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])),
                signingCredentials:credentials);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return new AuthResponse
            {
                AccessToken = jwt,
                Role = roleName,
                Permissions = permissions
            };


        }

       
    }
}
