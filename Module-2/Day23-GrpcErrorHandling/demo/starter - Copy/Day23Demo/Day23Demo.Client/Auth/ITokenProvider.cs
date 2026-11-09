namespace Day23Demo.Client.Auth;

public interface ITokenProvider
{
    Task<string?> GetTokenAsync();
}
