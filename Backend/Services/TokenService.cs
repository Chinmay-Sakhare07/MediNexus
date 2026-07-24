using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using HospitalManagement.API.Models.DTOs;

namespace HospitalManagement.API.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(UserAccountRecord user);
}

public class TokenService : ITokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryHours;

    public TokenService(IConfiguration config, IWebHostEnvironment env)
    {
        _secret = JwtConfig.ResolveSecret(config, env);
        _issuer = config["Jwt:Issuer"] ?? JwtConfig.DefaultIssuer;
        _audience = config["Jwt:Audience"] ?? JwtConfig.DefaultAudience;
        _expiryHours = int.TryParse(config["Jwt:ExpiryHours"], out var h) ? h : 8;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(UserAccountRecord user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("displayName", user.DisplayName)
        };
        if (user.StaffID.HasValue)   claims.Add(new Claim("staffId", user.StaffID.Value.ToString()));
        if (user.PatientID.HasValue) claims.Add(new Claim("patientId", user.PatientID.Value.ToString()));

        var expires = DateTime.UtcNow.AddHours(_expiryHours);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

/// <summary>Single place both TokenService and Program.cs resolve JWT config from.</summary>
public static class JwtConfig
{
    public const string DefaultIssuer = "MediNexus";
    public const string DefaultAudience = "MediNexusClients";

    // Development-only fallback so `dotnet run` works with zero setup.
    // Production REQUIRES Jwt__Secret (>= 32 chars) or startup throws.
    private const string DevOnlySecret = "medinexus-dev-only-secret-do-not-use-in-prod!!";

    public static string ResolveSecret(IConfiguration config, IWebHostEnvironment env)
    {
        var secret = config["Jwt:Secret"];
        if (!string.IsNullOrWhiteSpace(secret))
        {
            if (secret.Length < 32)
                throw new InvalidOperationException(
                    "Jwt__Secret must be at least 32 characters for HS256.");
            return secret;
        }

        if (env.IsDevelopment()) return DevOnlySecret;

        throw new InvalidOperationException(
            "Jwt__Secret is not configured. Set the Jwt__Secret environment variable " +
            "(any random string of 32+ chars) on the host before starting the API.");
    }
}
