using ClothingPlatform.Api.Models.Staff;
using ClothingPlatform.Api.Models.User;

namespace ClothingPlatform.Api.Features.User
{
    public interface IUserService
    {
      
        Task<PagedResult<UserModel>> GetUsersCustomerAsync(int page, int pageSize);
        Task<PagedResult<UserModel>> GetUsersStaffAsync(int page, int pageSize);

        UserDto? GetUserDto(int userId);
        
        void CreateUser(CreatRquestModel model);

        void UpdateUser(int userId,UpdateRequestModel model);

        void DeleteUser(int userId);
        Task<StaffDashboardDto> GetDashboardAsync();
    }
}
