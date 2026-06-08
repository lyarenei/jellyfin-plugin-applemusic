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
