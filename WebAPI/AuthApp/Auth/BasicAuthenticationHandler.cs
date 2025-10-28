using AuthApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
// Simple Basic Authentication handler for demonstration.
// NOTE: This is for demo only. Replace with proper secure authentication in production.
public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IUserManagementService _userManagementService;

    public BasicAuthenticationHandler(
        IUserManagementService userManagementService,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _userManagementService = userManagementService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return AuthenticateResult.NoResult();

        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        try
        {
            var token = authHeader.Substring("Basic ".Length).Trim();
            var credentialBytes = Convert.FromBase64String(token);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            if (credentials.Length != 2)
                return AuthenticateResult.Fail("Invalid Basic authentication header");

            var username = credentials[0];
            var password = credentials[1];

            // Validate credentials - demo user store
            if (!await _userManagementService.ValidateCredentialsAsync(username, password))
                return AuthenticateResult.Fail("Invalid username or password");

            var roles = await _userManagementService.GetUserRolesAsync(username);
            var claims = new[] { new Claim(ClaimTypes.Name, username) }
                .Concat(roles.Select(r => new Claim(ClaimTypes.Role, r)))
                .ToArray();
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Invalid Base64 string");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating");
            return AuthenticateResult.Fail("Authentication error");
        }
    }
}
