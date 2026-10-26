namespace Day21Practice.Client.Auth;

public interface ITokenProvider
{
    Task<string?> GetTokenAsync();
}
