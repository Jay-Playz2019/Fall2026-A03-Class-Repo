namespace Day22Practice.Client.Auth;

public interface ITokenProvider
{
    Task<string?> GetTokenAsync();
}
