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
        public List<UserModel> GetAllUsersInTbl()
        {
            return _db.Users.AsNoTracking()
                .Select(x => new UserModel
                {
                    Id = x.UserId,
                    First_Name = x.FirstName,
                    Last_Name = x.LastName,
                    Email = x.Email,
                    Address=x.Address,
                  
                }).ToList();
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
