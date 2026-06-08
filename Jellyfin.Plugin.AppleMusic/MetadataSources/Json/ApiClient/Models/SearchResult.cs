namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models;

/// <summary>
/// Apple Music JSON API search result.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchResult"/> class.
    /// </summary>
    public SearchResult()
    {
        Id = string.Empty;
        Type = string.Empty;
        Href = string.Empty;
        Attributes = new Attributes();
    }

    /// <summary>
    /// Gets or sets Apple Music ID.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the result.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the URL of the result.
    /// </summary>
    public string Href { get; set; }

    /// <summary>
    /// Gets or sets the attributes of the result.
    /// </summary>
    public Attributes Attributes { get; set; }

    /// <summary>
    /// Gets or sets the relationships (e.g., artists for albums).
    /// </summary>
    public Relationships? Relationships { get; set; }
}
