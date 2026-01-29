using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Requests;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient;

/// <summary>
/// Default Apple Music JSON API client.
/// </summary>
public class DefaultApiClient
{
    private const string CatalogBaseUrl = "https://amp-api-edge.music.apple.com/v1/catalog/us";
    private const string SearchBaseUrl = "https://amp-api-edge.music.apple.com/v1/catalog/us/search";
    private const string Origin = "https://music.apple.com";
    private const string Referer = "https://music.apple.com/";

    // TODO: Dynamic fetch & automatic refresh
    private const string Jwt = "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IldlYlBsYXlLaWQifQ.eyJpc3MiOiJBTVBXZWJQbGF5IiwiaWF0IjoxNzYyNTM4NTI0LCJleHAiOjE3Njk3OTYxMjQsInJvb3RfaHR0cHNfb3JpZ2luIjpbImFwcGxlLmNvbSJdfQ.2fpk1NEdRGBhrWjhjDJfeVWQyfa005cJYQ0Ye37GeD08vuyZvVA1xOc0JiePTEa9FLHa1HZjLd3n5F0CYUqLTw";

    private readonly IHttpClient _httpClient;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">Underlying HTTP client instance.</param>
    /// <param name="logger">Logger instance.</param>
    public DefaultApiClient(IHttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Search Apple Music for albums and artists.
    /// </summary>
    /// <param name="request">Search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search response with albums and artists.</returns>
    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var url = $"{SearchBaseUrl}?{request.ToQueryString()}";
        var headers = CreateRequestHeaders();

        _logger.LogDebug("Searching Apple Music API: {Url}", url);

        var response = await _httpClient.GetAsync<SearchResponse>(url, headers, cancellationToken);
        return response ?? new SearchResponse();
    }

    private static Dictionary<string, string> CreateRequestHeaders()
    {
        return new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {Jwt}" },
            { "Origin", Origin },
            { "Referer", Referer },
        };
    }
}
