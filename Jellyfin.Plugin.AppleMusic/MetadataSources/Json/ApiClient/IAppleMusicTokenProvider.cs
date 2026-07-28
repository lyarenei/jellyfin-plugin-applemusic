using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient;

/// <summary>
/// Provides a bearer token for authenticating against the Apple Music JSON API.
/// </summary>
public interface IAppleMusicTokenProvider
{
    /// <summary>
    /// Gets a valid Apple Music bearer token, fetching or refreshing it as needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A valid bearer token.</returns>
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}
