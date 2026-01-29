using System.Collections.Generic;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Responses;

/// <summary>
/// Response for single-resource API endpoints (albums, artists by ID).
/// </summary>
public class ResourceResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceResponse"/> class.
    /// </summary>
    public ResourceResponse()
    {
        Data = new List<SearchResult>();
    }

    /// <summary>
    /// Gets or sets the resource data.
    /// </summary>
    public List<SearchResult> Data { get; set; }
}
