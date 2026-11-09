using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Day23Practice.Client.Auth;

public class JwtTokenProvider : ITokenProvider
{
    private const string SigningKey = "ThisIsADemoSigningKeyForDay23AtLeast32Chars!";
    private const string Issuer = "https://demo-issuer.example.com";
    private const string Audience = "grpc-demo-api";

    public Task<string?> GetTokenAsync()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(SigningKey));
        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("name", "Alice Admin"),
            new Claim("role", "admin"),
            new Claim(JwtRegisteredClaimNames.Sub, "alice"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        return Task.FromResult<string?>(
            new JwtSecurityTokenHandler().WriteToken(token));
    }
}
