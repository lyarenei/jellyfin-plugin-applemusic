using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient;

/// <summary>
/// Base HTTP client.
/// </summary>
public class BaseHttpClient : IDisposable
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseHttpClient"/> class.
    /// </summary>
    /// <param name="client">Http client.</param>
    /// <param name="logger">Logger.</param>
    public BaseHttpClient(HttpClient client, ILogger logger)
    {
        _httpClient = client;
        _logger = logger;
    }

    /// <summary>
    /// Send a basic GET request and deserialize the response.
    /// </summary>
    /// <param name="url">Request URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="T">Response type.</typeparam>
    /// <returns>Response. Null on error.</returns>
    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        // TODO auth header
        // TODO error handling
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, _serializerOptions, cancellationToken);
    }

    /// <summary>
    /// Disposes managed and unmanaged (own) resources.
    /// </summary>
    /// <param name="disposing">Dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _httpClient.Dispose();
        }

        _isDisposed = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
