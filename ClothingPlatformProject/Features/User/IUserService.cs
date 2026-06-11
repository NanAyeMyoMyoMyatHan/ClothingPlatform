using ClothingPlatformProject.Models.User;

namespace ClothingPlatformProject.Features.User
{
    public interface IUserService
    {
        List<UserModel> GetAllUsersInTbl();
        
        UserDto? GetUserDto(int userId);
        
        void CreateUser(CreatRquestModel model);

        void UpdateUser(int userId,UpdateRequestModel model);

        void DeleteUser(int userId);
    }
}
