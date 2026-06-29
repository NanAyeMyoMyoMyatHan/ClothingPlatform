using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatform.Api.Features.Permission
{
    /// <summary>
    /// Admin-only endpoints for managing role permissions.
    /// All routes require the "AdminOnly" JWT policy.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        /// <summary>Returns all manageable roles (admin excluded).</summary>
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _permissionService.GetManageableRolesAsync();
            return Ok(roles);
        }

        /// <summary>Returns the full permission × role matrix with current grant states.</summary>
        [HttpGet("matrix")]
        public async Task<IActionResult> GetMatrix()
        {
            var matrix = await _permissionService.GetPermissionMatrixAsync();
            return Ok(matrix);
        }

        /// <summary>
        /// Atomically replaces the permission set for the given role.
        /// Permissions not in the request body are revoked.
        /// </summary>
        [HttpPut("role/{roleId}")]
        public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            var success = await _permissionService.UpdateRolePermissionsAsync(roleId, request.PermissionIds ?? new List<int>());

            if (!success)
            {
                return BadRequest("Role not found, is protected (admin), or permission IDs are invalid.");
            }

            return Ok(new { message = "Permissions updated successfully." });
        }
    }
}
