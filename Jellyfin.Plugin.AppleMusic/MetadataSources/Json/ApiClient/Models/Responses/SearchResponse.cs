using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Responses;

/// <summary>
/// Search response from Apple Music JSON API.
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// Gets or sets the wrapped results object.
    /// </summary>
    [JsonProperty("results")]
    public SearchResults Results { get; set; } = new();

    /// <summary>
    /// Gets the album results.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<SearchResult> Albums => GetData(Results.Albums);

    /// <summary>
    /// Gets the artist results.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<SearchResult> Artists => GetData(Results.Artists);

    private static IReadOnlyList<SearchResult> GetData(ResourceList? list)
    {
        return list is null ? Array.Empty<SearchResult>() : list.Data;
    }
}
