using ClothingPlatform.Api.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatform.Api.Features.User
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetUsersCustomer(int page=1, int pageSize=10)
        {
            var result = await _userService.GetUsersCustomerAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("staffs")]
        public async Task<IActionResult> GetUsersStaff(int staffpage = 1, int staffpageSize = 10)
        {
            var result = await _userService.GetUsersStaffAsync(staffpage, staffpageSize);
            return Ok(result);
        }


        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var result = _userService.GetUserDto(id);
            if(result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPost]
        public IActionResult CreateUser(CreatRquestModel userDto)
        {
            _userService.CreateUser(userDto);
            return Ok("Create Success");
        }

        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, UpdateRequestModel model)
        {
            _userService.UpdateUser(id, model);
            return Ok("Update Success");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            _userService.DeleteUser(id);
            return Ok("Delete Successfully");
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var result = await _userService.GetDashboardAsync();
            return Ok(result);
        }
    }
}
