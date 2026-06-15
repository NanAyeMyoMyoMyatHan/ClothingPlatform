using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.User;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatformProject.Features.User
{
    public class UserServices : IUserService 
    {
        private readonly AppDbContext _db;

        public UserServices(AppDbContext db)
        {
            _db = db;
        }
        public async Task<PagedResult<UserModel>> GetUsersCustomerAsync(int page, int pageSize)
        {
            var query = _db.Users.AsNoTracking();

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Where(u=>u.Role =="customer")
                .Select(u => new UserModel
                {
                    Id = u.UserId,
                    First_Name = u.FirstName,
                    Last_Name = u.LastName,
                    Email = u.Email,
                    Address = u.Address,
                    Role = u.Role,
                    PhoneNo = u.PhoneNumber,
                    CreatedAt= u.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<UserModel>
            {
                Items = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<UserModel>> GetUsersStaffAsync(int page, int pageSize)
        {
            var query = _db.Users.AsNoTracking();

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Where(u => u.Role == "staff")
                .Select(u => new UserModel
                {
                    Id = u.UserId,
                    First_Name = u.FirstName,
                    Last_Name = u.LastName,
                    Email = u.Email,
                    Address = u.Address,
                    Role = u.Role,
                    PhoneNo = u.PhoneNumber,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<UserModel>
            {
                Items = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public UserDto? GetUserDto(int userId)
        {
            var user = _db.Users
                .AsNoTracking()
                .FirstOrDefault(x=>x.UserId == userId);
            if (user == null)
            {
                return null;
            }
            return new UserDto
            {
                Id = userId,
                First_Name = user.FirstName,
                Last_Name= user.LastName,
                Address= user.Address,
                Email = user.Email

            };
        }

        public void CreateUser(CreatRquestModel userdto)
        {
            var user = new UserModel
            {
                First_Name = userdto.First_Name,
                Last_Name = userdto.Last_Name,
                Address = userdto.Address,
                Email = userdto.Email,
                Role = userdto.Role
            };
            _db.Add(user);
            _db.SaveChanges();
        }

        public void UpdateUser(int userId,UpdateRequestModel model)
        {
            var item = _db.Users.AsNoTracking()
                .FirstOrDefault(x => x.UserId == userId);
            if(item == null)
            {
                return;
            }
            
            model.First_Name = item.FirstName;
            model.Last_Name = item.LastName;
            model.Address = item.Address;
            model.Email = item.Email;
            model.Role = item.Role;

            _db.SaveChanges();
        }
        
        public void DeleteUser(int userId)
        {
            var item  = _db.Users
                .FirstOrDefault(x => x.UserId == userId);
            if (item == null) { return; }
            _db.Users.Remove(item);
            _db.SaveChanges();
        }
    }
}
