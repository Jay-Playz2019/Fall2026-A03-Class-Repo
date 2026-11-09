namespace Day22Demo.Client.Auth;

public interface ITokenProvider
{
    Task<string?> GetTokenAsync();
}
