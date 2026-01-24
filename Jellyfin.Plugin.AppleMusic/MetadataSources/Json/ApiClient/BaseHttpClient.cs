using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient;

/// <summary>
/// Base HTTP client.
/// </summary>
public class BaseHttpClient : IHttpClient, IDisposable
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

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (headers is not null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
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
