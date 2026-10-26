using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Day21Demo.Client.Auth;

/// <summary>
/// Generates a real JWT for testing. Uses the same signing key as the server.
/// In production, tokens would come from your identity provider via
/// AuthenticationStateProvider.
/// </summary>
public class JwtTokenProvider : ITokenProvider
{
    // DEMO ONLY: In production, tokens come from your identity provider.
    // Never embed signing keys in client code — this exists only to simulate JWT flow locally.
    private const string SigningKey = "ThisIsADemoSigningKeyForDay21AtLeast32Chars!";
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
