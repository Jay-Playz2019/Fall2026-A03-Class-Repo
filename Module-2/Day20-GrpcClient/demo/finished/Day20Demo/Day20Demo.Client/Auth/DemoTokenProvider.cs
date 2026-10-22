namespace Day20Demo.Client.Auth;

/// <summary>
/// Demo token provider that returns a hardcoded token.
/// In production, replace with a provider that retrieves a real token from
/// your identity provider.
/// </summary>
public class DemoTokenProvider : ITokenProvider
{
    public Task<string?> GetTokenAsync()
        => Task.FromResult<string?>("demo-bearer-token");
}
