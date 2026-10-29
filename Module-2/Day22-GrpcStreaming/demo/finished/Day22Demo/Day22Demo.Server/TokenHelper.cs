using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Day22Demo.Server;

/// <summary>
/// Generates test JWTs for local development. Not for production use.
/// In production, tokens come from your identity provider.
/// Production: use User Secrets or Azure Key Vault — never commit real keys.
/// </summary>
public class TokenHelper
{
    private readonly string _signingKey;
    private readonly string _issuer;
    private readonly string _audience;

    public TokenHelper(IConfiguration configuration)
    {
        _signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey not configured");
        _issuer = configuration["Jwt:Issuer"] ?? "https://demo-issuer.example.com";
        _audience = configuration["Jwt:Audience"] ?? "grpc-demo-api";
    }

    public string GenerateToken(
        string name, string role, int expiresInMinutes = 60)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_signingKey));
        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("name", name),
            new Claim("role", role),
            new Claim(JwtRegisteredClaimNames.Sub, name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
