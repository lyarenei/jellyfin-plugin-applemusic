using System.Collections.ObjectModel;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models;

/// <summary>
/// Container for relationship data items.
/// </summary>
public class RelationshipData
{
    /// <summary>
    /// Gets or sets the data items.
    /// </summary>
    public Collection<SearchResult> Data { get; set; } = new();
}
