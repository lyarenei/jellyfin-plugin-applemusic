using System.Collections.Generic;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models;

/// <summary>
/// Relationships data from Apple Music API response.
/// </summary>
public class Relationships
{
    /// <summary>
    /// Gets or sets the artists relationship data.
    /// </summary>
    public RelationshipData? Artists { get; set; }
}

/// <summary>
/// Container for relationship data items.
/// </summary>
public class RelationshipData
{
    /// <summary>
    /// Gets or sets the data items.
    /// </summary>
    public List<SearchResult> Data { get; set; } = new();
}
