using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lama.Application.AccessControl;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lama.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    /// <summary>
    /// Signing key. Keep it out of appsettings.json:
    /// dotnet user-secrets set "Auth:Jwt:Key" "&lt;random 32+ chars&gt;" --project Lama.Api
    /// Without it the API generates a key at startup, so tokens stop working after a restart.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "IconicU CRM";
    public string Audience { get; set; } = "IconicU CRM";
    public int ExpiresHours { get; set; } = 12;
}

public class JwtTokenService
{
    public const int MinKeyLength = 32;

    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(IOptions<JwtOptions> options, ILogger<JwtTokenService> logger)
    {
        _options = options.Value;

        if (_options.Key.Length >= MinKeyLength)
        {
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        }
        else
        {
            logger.LogWarning(
                "{Section}:Key is missing or shorter than {Min} characters — using a random key for this run, " +
                "so everyone is signed out on restart. Set it with dotnet user-secrets.",
                JwtOptions.SectionName, MinKeyLength);
            _signingKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
        }
    }

    public SymmetricSecurityKey SigningKey => _signingKey;

    public string Issuer => _options.Issuer;

    public string Audience => _options.Audience;

    public (string Token, DateTime ExpiresAt) Issue(UserDto user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(Math.Clamp(_options.ExpiresHours, 1, 24 * 30));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid? UserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
