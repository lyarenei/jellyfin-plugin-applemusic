using Newtonsoft.Json;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Web.Schema;

/// <summary>
/// Schema.org MusicGroup data embedded in an Apple Music artist page.
/// </summary>
public class MusicGroup
{
    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the artist description.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the artist image URL.
    /// </summary>
    [JsonProperty("image")]
    public string? Image { get; set; }
}
