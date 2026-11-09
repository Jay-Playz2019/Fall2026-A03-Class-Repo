namespace Day23Practice.Client.Auth;

public interface ITokenProvider
{
    Task<string?> GetTokenAsync();
}
