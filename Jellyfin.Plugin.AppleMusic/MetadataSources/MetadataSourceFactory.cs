using System.Net.Http;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Json;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Web;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources;

/// <summary>
/// Factory for creating metadata sources.
/// </summary>
public static class MetadataSourceFactory
{
    // Json metadata source is still experimental and should not be used.
    // When the time comes, this should be converted to a plugin setting.
    private const bool UseJsonSource = false;

    /// <summary>
    /// Creates a metadata source instance.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <returns>A metadata source instance.</returns>
    public static IMetadataSource Create(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        if (UseJsonSource)
        {
            return CreateJsonSource(httpClientFactory, loggerFactory);
        }

        return new WebMetadataSource(loggerFactory);
    }

    private static JsonMetadataSource CreateJsonSource(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        var httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        var baseHttpClient = new BaseHttpClient(httpClient, loggerFactory.CreateLogger<BaseHttpClient>());
        var apiClient = new DefaultApiClient(baseHttpClient, loggerFactory.CreateLogger<DefaultApiClient>());
        return new JsonMetadataSource(apiClient, loggerFactory);
    }
}
