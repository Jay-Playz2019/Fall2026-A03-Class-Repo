namespace Day20Demo.Client.Auth;

// Demo only: a fixed string, not a JWT
public class DemoTokenProvider : ITokenProvider
{
    public Task<string?> GetTokenAsync()
        => Task.FromResult<string?>("demo-bearer-token");
}