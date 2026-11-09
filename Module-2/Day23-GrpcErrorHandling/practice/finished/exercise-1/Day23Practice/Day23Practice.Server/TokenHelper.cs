using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Day23Practice.Server;

public static class TokenHelper
{
    private const string SigningKey = "ThisIsADemoSigningKeyForDay23AtLeast32Chars!";
    private const string Issuer = "https://demo-issuer.example.com";
    private const string Audience = "grpc-demo-api";

    public static string GenerateToken(
        string name, string role, int expiresInMinutes = 60)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(SigningKey));
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
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
