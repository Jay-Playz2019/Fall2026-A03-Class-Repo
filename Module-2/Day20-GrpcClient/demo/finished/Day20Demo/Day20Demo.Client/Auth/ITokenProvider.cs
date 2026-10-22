namespace Day20Demo.Client.Auth;

/// <summary>
/// Provides bearer tokens for gRPC call authentication.
/// In production, this reads the access token the OIDC handler stored at sign-in,
/// via IHttpContextAccessor + HttpContext.GetTokenAsync("access_token") — see Day 24.
/// (AuthenticationStateProvider gives the claims principal, not the raw token.)
/// </summary>
public interface ITokenProvider
{
    Task<string?> GetTokenAsync();
}
