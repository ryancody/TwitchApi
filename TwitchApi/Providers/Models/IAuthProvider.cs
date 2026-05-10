using TwitchApi.Models;

namespace TwitchApi.Providers.Models;

public interface IAuthProvider
{
    Task<AuthInfo> GetAuthInfoAsync();
    Task SaveAuthInfoAsync(AuthInfo authInfo);
}
