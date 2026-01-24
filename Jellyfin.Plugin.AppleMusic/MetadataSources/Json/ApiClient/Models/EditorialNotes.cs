namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models;

/// <summary>
/// Editorial notes from Apple Music API response.
/// </summary>
public class EditorialNotes
{
    /// <summary>
    /// Gets or sets the short editorial note.
    /// </summary>
    public string? Short { get; set; }

    /// <summary>
    /// Gets or sets the standard editorial note.
    /// </summary>
    public string? Standard { get; set; }
}
