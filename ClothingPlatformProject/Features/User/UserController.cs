using ClothingPlatformProject.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatformProject.Features.User
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _userService.GetAllUsersInTbl();
            return Ok(users);
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
    }
}
