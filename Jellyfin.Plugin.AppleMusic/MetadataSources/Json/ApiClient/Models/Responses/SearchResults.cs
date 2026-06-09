using Newtonsoft.Json;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Responses;

/// <summary>
/// Wrapper for the "results" object in a search response.
/// </summary>
public class SearchResults
{
    /// <summary>
    /// Gets or sets the album results.
    /// </summary>
    [JsonProperty("albums")]
    public ResourceList? Albums { get; set; }

    /// <summary>
    /// Gets or sets the artist results.
    /// </summary>
    [JsonProperty("artists")]
    public ResourceList? Artists { get; set; }
}
