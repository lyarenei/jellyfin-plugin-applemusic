using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient;

/// <summary>
/// Provides the Apple Music web player bearer token.
/// The token is scraped from the Apple Music website's script bundle and cached
/// until shortly before it expires, at which point the next request re-fetches it.
/// </summary>
public partial class WebPlayTokenProvider : IAppleMusicTokenProvider, IDisposable
{
    private const string HomePageUrl = "https://music.apple.com/";
    private const string AssetsBaseUrl = "https://music.apple.com";
    private const string WebPlayKeyId = "WebPlayKid";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    private static readonly TimeSpan _refreshMargin = TimeSpan.FromHours(24);

    private readonly IHttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _token;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebPlayTokenProvider"/> class.
    /// </summary>
    /// <param name="httpClient">Underlying HTTP client.</param>
    /// <param name="logger">Logger.</param>
    public WebPlayTokenProvider(IHttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_token is not null && IsTokenValid())
        {
            return _token;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && IsTokenValid())
            {
                return _token;
            }

            await RefreshAsync(cancellationToken);
            return _token!;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <param name="disposing">Whether to release managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _refreshLock.Dispose();
        }

        _isDisposed = true;
    }

    private bool IsTokenValid()
    {
        return DateTimeOffset.UtcNow < _expiresAt - _refreshMargin;
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching Apple Music web player token");

        var headers = new Dictionary<string, string> { { "User-Agent", UserAgent } };

        var html = await _httpClient.GetStringAsync(HomePageUrl, headers, cancellationToken);
        var bundleMatch = BundleRegex().Match(html);
        if (!bundleMatch.Success)
        {
            _logger.LogError("Could not locate the Apple Music web player script bundle in the page");
            throw new InvalidOperationException("Failed to locate the Apple Music web player script bundle.");
        }

        var bundleUrl = AssetsBaseUrl + bundleMatch.Value;
        var bundle = await _httpClient.GetStringAsync(bundleUrl, headers, cancellationToken);

        var token = ExtractWebPlayToken(bundle);
        if (token is null)
        {
            _logger.LogError("Could not extract the Apple Music web player token from bundle {BundleUrl}", bundleUrl);
            throw new InvalidOperationException("Failed to extract the Apple Music web player token from the script bundle.");
        }

        _token = token;
        _expiresAt = ReadExpiry(token);
        _logger.LogInformation("Apple Music web player token refreshed, expires at {ExpiresAt}", _expiresAt);
    }

    private static string? ExtractWebPlayToken(string bundle)
    {
        foreach (Match match in JwtRegex().Matches(bundle))
        {
            var token = match.Value;
            if (GetHeaderKeyId(token) == WebPlayKeyId)
            {
                return token;
            }
        }

        return null;
    }

    private static string? GetHeaderKeyId(string token)
    {
        var header = DecodeSegment(token, 0);
        if (header is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(header);
        return document.RootElement.TryGetProperty("kid", out var kid) ? kid.GetString() : null;
    }

    private static DateTimeOffset ReadExpiry(string token)
    {
        var payload = DecodeSegment(token, 1);
        if (payload is not null)
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
            }
        }

        // Without a readable expiry, treat the token as immediately stale so the next
        // request re-fetches rather than trusting an unbounded token.
        return DateTimeOffset.MinValue;
    }

    private static string? DecodeSegment(string token, int index)
    {
        var parts = token.Split('.');
        if (parts.Length <= index)
        {
            return null;
        }

        // JWTs use base64url, which swaps '+' and '/' for '-' and '_'
        // Convert them back for Convert.FromBase64String
        var value = parts[index].Replace('-', '+').Replace('_', '/');

        // base64 requires the length to be a multiple of 4, so restore padding if necessary
        var padding = (4 - (value.Length % 4)) % 4;
        if (padding > 0)
        {
            value = value.PadRight(value.Length + padding, '=');
        }

        var bytes = Convert.FromBase64String(value);
        return Encoding.UTF8.GetString(bytes);
    }

    [GeneratedRegex(@"/assets/index~[A-Za-z0-9]+\.js")]
    private static partial Regex BundleRegex();

    [GeneratedRegex(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+")]
    private static partial Regex JwtRegex();
}
