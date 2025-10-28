using AuthApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static AuthApp.Controllers.UserManagementController;

namespace AuthApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserManagementService _userService;

        public AuthController(IUserManagementService userService)
        {
            _userService = userService;
        }

        // POST api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] CredentialsRequest request, CancellationToken ct = default)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var valid = await _userService.ValidateCredentialsAsync(request.Email, request.Password, ct);
            if (!valid)
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByEmailAsync(request.Email, ct);
            if (user is null)
            {
                // Defensive: credentials validated but user not found -> treat as unauthorized.
                return Unauthorized();
            }

            // If token generation is needed, move that responsibility to a dedicated service.
            return Ok(new { authenticated = true, user });
        }
    }
}