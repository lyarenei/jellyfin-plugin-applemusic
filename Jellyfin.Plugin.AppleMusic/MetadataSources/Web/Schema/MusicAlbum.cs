using System.Collections.Generic;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Web.Schema;

/// <summary>
/// Schema.org MusicAlbum data embedded in an Apple Music album page.
/// </summary>
public class MusicAlbum
{
    /// <summary>
    /// Gets or sets the album name.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the album image URL.
    /// </summary>
    [JsonProperty("image")]
    public string? Image { get; set; }

    /// <summary>
    /// Gets or sets the album release date (yyyy-MM-dd).
    /// </summary>
    [JsonProperty("datePublished")]
    public string? DatePublished { get; set; }

    /// <summary>
    /// Gets or sets the album artists.
    /// Empty for compilations.
    /// </summary>
    [JsonProperty("byArtist")]
    public IReadOnlyList<MusicGroup>? ByArtist { get; set; }
}
