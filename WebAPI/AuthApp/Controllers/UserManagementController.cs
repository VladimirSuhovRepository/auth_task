using AuthApp.Services;
using AuthApp.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public partial class UserManagementController : ControllerBase
    {
        private readonly IUserManagementService _userService;

        public UserManagementController(IUserManagementService userService)
        {
            _userService = userService;
        }

        // POST api/users
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct = default)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var created = await _userService.CreateUserAsync(request.Email, request.Password, request.Roles, ct);
            if (created is null)
            {
                return BadRequest("Could not create user.");
            }

            // Assume UserDto has an Id property for CreatedAtAction route values
            return CreatedAtAction(nameof(GetById), new { id = (created as dynamic).Id }, created);
        }

        // GET api/users/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken ct = default)
        {
            var user = await _userService.GetUserByIdAsync(id, ct);
            if (user is null) return NotFound();
            return Ok(user);
        }

        // GET api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(CancellationToken ct = default)
        {
            var users = await _userService.GetAllUsersAsync(ct);
            if (users is null)
            {
                return Ok(Enumerable.Empty<UserDto>());
            }

            return Ok(users);
        }

        // PUT api/users/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserDto user, CancellationToken ct = default)
        {
            if (user is null) return BadRequest("User data is required.");

            // If UserDto includes Id, try to ensure consistency
            try
            {
                var userId = (int)(user as dynamic).Id;
                if (userId != id) return BadRequest("Id in route does not match id in body.");
            }
            catch
            {
                // If UserDto has no Id property, skip this check.
            }

            var updated = await _userService.UpdateUserAsync(user, ct);
            if (!updated) return NotFound();
            return NoContent();
        }

        // DELETE api/users/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var deleted = await _userService.DeleteUserAsync(id, ct);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
