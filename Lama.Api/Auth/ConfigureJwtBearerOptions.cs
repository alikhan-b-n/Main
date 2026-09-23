using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lama.Api.Auth;

/// <summary>
/// Validation uses the very same key instance that signs the tokens, so issuing and
/// validating can never drift apart (including the random key of an unconfigured run).
/// </summary>
public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtTokenService _tokens;

    public ConfigureJwtBearerOptions(JwtTokenService tokens)
    {
        _tokens = tokens;
    }

    public void Configure(JwtBearerOptions options)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _tokens.Issuer,
            ValidateAudience = true,
            ValidAudience = _tokens.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _tokens.SigningKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    public void Configure(string? name, JwtBearerOptions options) => Configure(options);
}
