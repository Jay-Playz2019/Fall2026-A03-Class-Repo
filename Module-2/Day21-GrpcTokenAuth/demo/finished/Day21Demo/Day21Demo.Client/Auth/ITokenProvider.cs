namespace Day21Demo.Client.Auth;

public interface ITokenProvider
{
    Task<string?> GetTokenAsync();
}
