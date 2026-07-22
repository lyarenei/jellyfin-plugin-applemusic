using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Responses;

/// <summary>
/// A typed list of resources returned by the Apple Music JSON API,
/// including paging metadata.
/// </summary>
public class ResourceList
{
    /// <summary>
    /// Gets the resource items.
    /// </summary>
    [JsonProperty("data")]
    public Collection<SearchResult> Data { get; } = new();

    /// <summary>
    /// Gets or sets the canonical href of this list.
    /// </summary>
    [JsonProperty("href")]
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the URL to fetch the next page of results.
    /// </summary>
    [JsonProperty("next")]
    public string? Next { get; set; }
}
