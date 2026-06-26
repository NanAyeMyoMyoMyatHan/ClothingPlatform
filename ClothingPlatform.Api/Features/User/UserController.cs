using ClothingPlatform.Api.Filters;
using ClothingPlatform.Api.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatform.Api.Features.User
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOrStaff")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("customers")]
        [Permission("Customers.View", true)]
        public async Task<IActionResult> GetUsersCustomer(int page = 1, int pageSize = 10)
        {
            var result = await _userService.GetUsersCustomerAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("staffs")]
        [Permission("Staff.Manage", true)]
        public async Task<IActionResult> GetUsersStaff(int staffpage = 1, int staffpageSize = 10)
        {
            var result = await _userService.GetUsersStaffAsync(staffpage, staffpageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Permission("Customers.View", true)]
        public IActionResult GetUserById(int id)
        {
            var result = _userService.GetUserDto(id);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost]
        [Permission("Staff.Manage", true)]
        public IActionResult CreateUser(CreatRquestModel userDto)
        {
            _userService.CreateUser(userDto);
            return Ok("Create Success");
        }

        [HttpPut("{id}")]
        [Permission("Staff.Manage", true)]
        public IActionResult UpdateUser(int id, UpdateRequestModel model)
        {
            _userService.UpdateUser(id, model);
            return Ok("Update Success");
        }

        [HttpDelete("{id}")]
        [Permission("Staff.Manage", true)]
        public IActionResult DeleteUser(int id)
        {
            _userService.DeleteUser(id);
            return Ok("Delete Successfully");
        }

        [HttpGet("dashboard")]
        [Permission("Customers.View", true)]
        public async Task<IActionResult> Dashboard()
        {
            var result = await _userService.GetDashboardAsync();
            return Ok(result);
        }
    }
}
