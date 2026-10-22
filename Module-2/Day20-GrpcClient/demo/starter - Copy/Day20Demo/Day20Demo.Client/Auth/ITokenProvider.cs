namespace Day20Demo.Client.Auth;
// One method: hand back the token to send, or null
public interface ITokenProvider
{
    Task<string?> GetTokenAsync();
}