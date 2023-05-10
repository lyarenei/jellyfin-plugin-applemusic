namespace Jellyfin.Plugin.ITunes.ExternalIds;

// Disable warning for missing enum item with value 0.
#pragma warning disable CA1008

/// <summary>
/// Apple Music provider keys.
/// </summary>
public enum ITunesProviderKey
{
    /// <summary>
    /// Apple Music album provider ID.
    /// </summary>
    Album = 11448800,

    /// <summary>
    /// Apple Music album artist provider ID.
    /// </summary>
    AlbumArtist = 11448801,

    /// <summary>
    /// Apple Music artist provider ID.
    /// </summary>
    Artist = 11448802,
}
