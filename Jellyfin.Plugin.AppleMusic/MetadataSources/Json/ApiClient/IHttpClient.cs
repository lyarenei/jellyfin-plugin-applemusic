using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient;

/// <summary>
/// JSON API HTTP client interface.
/// </summary>
public interface IHttpClient
{
    /// <summary>
    /// Send a GET request and deserialize the response.
    /// </summary>
    /// <param name="url">Request URL.</param>
    /// <param name="headers">Optional headers to include in the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="T">Response type.</typeparam>
    /// <returns>Deserialized response, or null on error.</returns>
    Task<T?> GetAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
}
